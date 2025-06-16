// Services/OrderBookService.cs - Enhanced version with proper ClientOrderId handling
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class OrderBookService
    {
        private readonly CcgMessagesService _ccgMessagesService;
        private readonly InstrumentsService _instrumentsService;
        private readonly Dictionary<ulong, OrderBookEntry> _orderBook = new Dictionary<ulong, OrderBookEntry>();
        private readonly Dictionary<string, OrderBookEntry> _ordersByClientOrderId = new Dictionary<string, OrderBookEntry>(); // Track by ClientOrderId too
        private readonly ObservableCollection<OrderBookEntry> _orders = new ObservableCollection<OrderBookEntry>();
        private const int MAX_ORDERS = 500; // Limit to prevent memory issues

        public ObservableCollection<OrderBookEntry> Orders => _orders;

        public event Action<OrderBookEntry> OrderUpdated;
        public event Action OrderBookCleared;

        public OrderBookService(CcgMessagesService ccgMessagesService, InstrumentsService instrumentsService)
        {
            _ccgMessagesService = ccgMessagesService;
            _instrumentsService = instrumentsService;
            
            // Subscribe to new CCG messages
            _ccgMessagesService.NewCcgMessageReceived += ProcessCcgMessage;
            _ccgMessagesService.MessagesCleared += OnCcgMessagesCleared;
            
            // Subscribe to instruments updates for mapping
            _instrumentsService.StatusUpdated += OnInstrumentsUpdated;
        }

        private void OnInstrumentsUpdated(string status)
        {
            if (status.Contains("Loaded"))
            {
                // Re-map all existing orders when instruments are loaded
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    foreach (var order in _orders)
                    {
                        MapInstrumentData(order);
                        order.UpdateBasicProperties();
                    }
                });
            }
        }

        private void OnCcgMessagesCleared()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _orderBook.Clear();
                _ordersByClientOrderId.Clear();
                _orders.Clear();
                OrderBookCleared?.Invoke();
            });
        }

        private void ProcessCcgMessage(CcgMessage message)
        {
            try
            {
                switch (message.Name)
                {
                    case "OrderAdd":
                        ProcessOrderAdd(message);
                        break;
                    case "OrderAddResponse":
                        ProcessOrderAddResponse(message);
                        break;
                    case "OrderModify":
                        ProcessOrderModify(message);
                        break;
                    case "OrderModifyResponse":
                        ProcessOrderModifyResponse(message);
                        break;
                    case "OrderCancel":
                        ProcessOrderCancel(message);
                        break;
                    case "OrderCancelResponse":
                        ProcessOrderCancelResponse(message);
                        break;
                    case "Trade":
                        ProcessTrade(message);
                        break;
                    case "OrderMassCancel":
                        ProcessOrderMassCancel(message);
                        break;
                    case "OrderMassCancelResponse":
                        ProcessOrderMassCancelResponse(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing CCG message for order book: {ex.Message}");
            }
        }

        private void ProcessOrderAdd(CcgMessage message)
        {
            // Extract ClientOrderId from OrderAdd message if available
            if (message.RawData != null && message.RawData.Length >= 167)
            {
                try
                {
                    // ClientOrderId is at offset 147, 20 bytes (according to GPW WATS spec)
                    var clientOrderIdBytes = new byte[20];
                    Array.Copy(message.RawData, 147, clientOrderIdBytes, 0, 20);
                    string clientOrderId = Encoding.ASCII.GetString(clientOrderIdBytes).TrimEnd('\0', ' ');
                    
                    // Store the pending OrderAdd with its ClientOrderId for later matching
                    var pendingOrder = new PendingOrderAdd
                    {
                        ClientOrderId = clientOrderId,
                        Message = message,
                        ReceivedTime = message.DateReceived
                    };
                    
                    // Store it temporarily - we'll match it when OrderAddResponse arrives
                    _pendingOrderAdds.Add(pendingOrder);
                    
                    // Clean up old pending orders (older than 30 seconds)
                    var cutoffTime = DateTime.Now.AddSeconds(-30);
                    _pendingOrderAdds.RemoveAll(p => p.ReceivedTime < cutoffTime);
                    
                    System.Diagnostics.Debug.WriteLine($"OrderAdd received: ClientOrderId='{clientOrderId}', InstrumentId={message.InstrumentId}, Side={message.Side}, Price={message.Price}, Qty={message.Quantity}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing OrderAdd ClientOrderId: {ex.Message}");
                }
            }
        }

        // Helper class for tracking pending OrderAdd messages
        private class PendingOrderAdd
        {
            public string ClientOrderId { get; set; }
            public CcgMessage Message { get; set; }
            public DateTime ReceivedTime { get; set; }
        }
        
        private readonly List<PendingOrderAdd> _pendingOrderAdds = new List<PendingOrderAdd>();

        private void ProcessOrderAddResponse(CcgMessage message)
        {
            if (!ulong.TryParse(message.ClientOrderId, out ulong orderId))
                return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var entry = GetOrCreateOrderEntry(orderId);
                
                // Update from OrderAddResponse
                if (message.RawData != null && message.RawData.Length >= 52) // OrderAddResponse is 52 bytes
                {
                    try
                    {
                        // Parse OrderAddResponse fields (based on GPW WATS specification)
                        ulong publicOrderId = BitConverter.ToUInt64(message.RawData, 24);
                        ulong displayQty = BitConverter.ToUInt64(message.RawData, 32);
                        ulong filled = BitConverter.ToUInt64(message.RawData, 40);
                        byte status = message.RawData[48];
                        ushort reason = BitConverter.ToUInt16(message.RawData, 49);
                        byte execTypeReason = message.RawData[51];
                        
                        entry.PublicOrderId = publicOrderId;
                        entry.DisplayQuantity = displayQty;
                        entry.FilledQuantity = filled;
                        entry.Status = GetOrderStatusName(status);
                        entry.LastExecTypeReason = GetExecTypeReasonName(execTypeReason);
                        entry.OrderIdReference = orderId.ToString(); // Store the OrderId reference
                        
                        // Try to find corresponding OrderAdd message and populate details
                        var orderAddMessage = FindCorrespondingOrderAdd(entry, message);
                        if (orderAddMessage != null)
                        {
                            PopulateOrderDetailsFromOrderAdd(entry, orderAddMessage);
                            
                            // Add to ClientOrderId lookup if we have one
                            if (!string.IsNullOrEmpty(entry.ClientOrderId))
                            {
                                _ordersByClientOrderId[entry.ClientOrderId] = entry;
                            }
                        }
                        
                        // Calculate current quantity
                        if (entry.OriginalQuantity > 0)
                        {
                            entry.CurrentQuantity = entry.OriginalQuantity - filled;
                        }
                        
                        entry.LastModifiedTime = message.DateReceived;
                        entry.UpdateBasicProperties();
                        
                        MapInstrumentData(entry);
                        OrderUpdated?.Invoke(entry);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing OrderAddResponse: {ex.Message}");
                    }
                }
            });
        }

        private void ProcessOrderModify(CcgMessage message)
        {
            var entry = FindOrderEntry(message.ClientOrderId);
            if (entry == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Parse OrderModify message to get modification details
                var modifyDetails = ParseOrderModifyDetails(message);
                
                var modification = new OrderModification
                {
                    ModificationTime = message.DateReceived,
                    ModificationType = "Modify Request",
                    Status = "Pending",
                    ModificationDetails = modifyDetails
                };
                
                // Store old values for comparison
                if (modifyDetails.ContainsKey("price"))
                {
                    modification.OldValue = entry.Price?.ToString("F4") ?? "";
                    modification.NewValue = modifyDetails["price"];
                    modification.FieldModified = "Price";
                }
                else if (modifyDetails.ContainsKey("quantity"))
                {
                    modification.OldValue = entry.OriginalQuantity.ToString();
                    modification.NewValue = modifyDetails["quantity"];
                    modification.FieldModified = "Quantity";
                }
                else if (modifyDetails.ContainsKey("displayQty"))
                {
                    modification.OldValue = entry.DisplayQuantity?.ToString() ?? "";
                    modification.NewValue = modifyDetails["displayQty"];
                    modification.FieldModified = "DisplayQty";
                }
                else
                {
                    modification.FieldModified = "Multiple";
                    modification.OldValue = "Various";
                    modification.NewValue = "Various";
                }
                
                // Use new method to add modification and notify UI
                entry.AddModification(modification);
                entry.LastModifiedTime = message.DateReceived;
                entry.UpdateBasicProperties();
                OrderUpdated?.Invoke(entry);
                
                System.Diagnostics.Debug.WriteLine($"OrderModify: OrderId={entry.OrderId}, Field={modification.FieldModified}, Old={modification.OldValue}, New={modification.NewValue}");
            });
        }

        private void ProcessOrderModifyResponse(CcgMessage message)
        {
            var entry = FindOrderEntry(message.ClientOrderId);
            if (entry == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Parse OrderModifyResponse
                if (message.RawData != null && message.RawData.Length >= 36)
                {
                    try
                    {
                        ulong filled = BitConverter.ToUInt64(message.RawData, 24);
                        byte status = message.RawData[32];
                        byte priorityFlag = message.RawData[33];
                        ushort reason = BitConverter.ToUInt16(message.RawData, 34);
                        
                        // Update the latest modification with response using new method
                        entry.UpdateLastModification(latestMod =>
                        {
                            if (latestMod.Status == "Pending")
                            {
                                if (status == 1) // New status = successful modification
                                {
                                    latestMod.Status = "Accepted";
                                    latestMod.PriorityRetained = priorityFlag == 2; // 2 = Retained
                                    
                                    // Apply the modification to the order if successful
                                    ApplyModificationToOrder(entry, latestMod);
                                }
                                else
                                {
                                    latestMod.Status = "Rejected";
                                    latestMod.RejectReason = GetRejectReasonName(reason);
                                }
                            }
                        });
                        
                        // Update filled quantity
                        entry.FilledQuantity = filled;
                        entry.CurrentQuantity = entry.OriginalQuantity - filled;
                        entry.Status = GetOrderStatusName(status);
                        entry.LastModifiedTime = message.DateReceived;
                        entry.UpdateBasicProperties();
                        
                        OrderUpdated?.Invoke(entry);
                        
                        System.Diagnostics.Debug.WriteLine($"OrderModifyResponse: OrderId={entry.OrderId}, Status={GetOrderStatusName(status)}, Priority={(priorityFlag == 2 ? "Retained" : "Lost")}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing OrderModifyResponse: {ex.Message}");
                    }
                }
            });
        }

        private void ProcessOrderCancel(CcgMessage message)
        {
            var entry = FindOrderEntry(message.ClientOrderId);
            if (entry == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var cancelAttempt = new OrderCancelAttempt
                {
                    CancelTime = message.DateReceived,
                    Status = "Pending",
                    CancelReason = "User Request"
                };
                
                // Use new method to add cancel attempt and notify UI
                entry.AddCancelAttempt(cancelAttempt);
                entry.LastModifiedTime = message.DateReceived;
                entry.UpdateBasicProperties();
                OrderUpdated?.Invoke(entry);
            });
        }

        private void ProcessOrderCancelResponse(CcgMessage message)
        {
            var entry = FindOrderEntry(message.ClientOrderId);
            if (entry == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Parse cancel response
                if (message.RawData != null && message.RawData.Length >= 28)
                {
                    try
                    {
                        byte status = message.RawData[24];
                        ushort reason = BitConverter.ToUInt16(message.RawData, 25);
                        byte execTypeReason = message.RawData[27];
                        
                        // Update the latest cancel attempt using new method
                        entry.UpdateLastCancelAttempt(latestCancel =>
                        {
                            if (latestCancel.Status == "Pending")
                            {
                                if (status == 2) // Cancelled status
                                {
                                    latestCancel.Status = "Accepted";
                                }
                                else
                                {
                                    latestCancel.Status = "Rejected";
                                    latestCancel.RejectReason = GetRejectReasonName(reason);
                                }
                            }
                        });
                        
                        entry.Status = GetOrderStatusName(status);
                        entry.LastExecTypeReason = GetExecTypeReasonName(execTypeReason);
                        entry.LastModifiedTime = message.DateReceived;
                        entry.UpdateBasicProperties();
                        OrderUpdated?.Invoke(entry);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing OrderCancelResponse: {ex.Message}");
                    }
                }
            });
        }

        private void ProcessTrade(CcgMessage message)
        {
            var entry = FindOrderEntry(message.ClientOrderId);
            if (entry == null) return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                // Parse Trade message
                if (message.RawData != null && message.RawData.Length >= 52)
                {
                    try
                    {
                        uint tradeId = BitConverter.ToUInt32(message.RawData, 24);
                        long priceRaw = BitConverter.ToInt64(message.RawData, 28);
                        ulong quantity = BitConverter.ToUInt64(message.RawData, 36);
                        ulong leavesQty = BitConverter.ToUInt64(message.RawData, 44);
                        
                        decimal price = (decimal)priceRaw / 100000000m; // Assuming 8 decimal places
                        
                        var trade = new OrderTrade
                        {
                            TradeId = tradeId,
                            Price = price,
                            Quantity = quantity,
                            LeavesQuantity = leavesQty,
                            ExecutionTime = message.DateReceived
                        };
                        
                        // Use new method to add trade and notify UI
                        entry.AddTrade(trade);
                        entry.FilledQuantity += quantity;
                        entry.CurrentQuantity = leavesQty;
                        
                        // Update status based on remaining quantity
                        if (leavesQty == 0)
                        {
                            entry.Status = "Filled";
                        }
                        else
                        {
                            entry.Status = "PartiallyFilled";
                        }
                        
                        entry.LastModifiedTime = message.DateReceived;
                        entry.UpdateBasicProperties();
                        OrderUpdated?.Invoke(entry);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing Trade: {ex.Message}");
                    }
                }
            });
        }

        private void ProcessOrderMassCancel(CcgMessage message)
        {
            System.Diagnostics.Debug.WriteLine("OrderMassCancel received");
        }

        private void ProcessOrderMassCancelResponse(CcgMessage message)
        {
            System.Diagnostics.Debug.WriteLine("OrderMassCancelResponse received");
        }

        // Helper methods

        private OrderBookEntry GetOrCreateOrderEntry(ulong orderId)
        {
            if (!_orderBook.TryGetValue(orderId, out var entry))
            {
                entry = new OrderBookEntry
                {
                    OrderId = orderId,
                    CreatedTime = DateTime.Now,
                    LastModifiedTime = DateTime.Now,
                    Status = "New"
                };
                
                _orderBook[orderId] = entry;
                
                // Maintain size limit
                while (_orders.Count >= MAX_ORDERS)
                {
                    var oldestOrder = _orders.OrderBy(o => o.CreatedTime).FirstOrDefault();
                    if (oldestOrder != null)
                    {
                        _orders.Remove(oldestOrder);
                        _orderBook.Remove(oldestOrder.OrderId);
                        
                        // Remove from ClientOrderId lookup too
                        if (!string.IsNullOrEmpty(oldestOrder.ClientOrderId))
                        {
                            _ordersByClientOrderId.Remove(oldestOrder.ClientOrderId);
                        }
                    }
                }
                
                _orders.Insert(0, entry); // Add to beginning for newest-first display
            }
            
            return entry;
        }

        private OrderBookEntry FindOrderEntry(string orderIdRef)
        {
            // Try to parse as OrderId first
            if (ulong.TryParse(orderIdRef, out ulong orderId))
            {
                if (_orderBook.TryGetValue(orderId, out var entry))
                {
                    return entry;
                }
            }
            
            // Try to find by ClientOrderId
            if (_ordersByClientOrderId.TryGetValue(orderIdRef, out var entryByClientId))
            {
                return entryByClientId;
            }
            
            System.Diagnostics.Debug.WriteLine($"Order not found: {orderIdRef}");
            return null;
        }

        private CcgMessage FindCorrespondingOrderAdd(OrderBookEntry entry, CcgMessage orderAddResponse)
        {
            // Try to find the most recent OrderAdd message that matches this response
            // Look for pending OrderAdd messages received shortly before this response
            var recentPending = _pendingOrderAdds
                .Where(p => p.ReceivedTime <= orderAddResponse.DateReceived)
                .OrderByDescending(p => p.ReceivedTime)
                .FirstOrDefault();
            
            if (recentPending != null)
            {
                _pendingOrderAdds.Remove(recentPending); // Remove it since we matched it
                return recentPending.Message;
            }
            
            // Fallback - find any recent OrderAdd message
            var recentMessages = _ccgMessagesService.CcgMessages.Take(50);
            return recentMessages.FirstOrDefault(m => m.Name == "OrderAdd");
        }

        private void PopulateOrderDetailsFromOrderAdd(OrderBookEntry entry, CcgMessage orderAddMessage)
        {
            if (orderAddMessage.RawData != null && orderAddMessage.RawData.Length >= 167)
            {
                try
                {
                    // Parse key fields from OrderAdd (based on GPW WATS specification)
                    uint instrumentId = BitConverter.ToUInt32(orderAddMessage.RawData, 17);
                    byte orderType = orderAddMessage.RawData[21];
                    byte timeInForce = orderAddMessage.RawData[22];
                    byte side = orderAddMessage.RawData[23];
                    long priceRaw = BitConverter.ToInt64(orderAddMessage.RawData, 24);
                    ulong quantity = BitConverter.ToUInt64(orderAddMessage.RawData, 40);
                    
                    // Extract ClientOrderId from offset 147 (20 bytes)
                    var clientOrderIdBytes = new byte[20];
                    Array.Copy(orderAddMessage.RawData, 147, clientOrderIdBytes, 0, 20);
                    string clientOrderId = Encoding.ASCII.GetString(clientOrderIdBytes).TrimEnd('\0', ' ');
                    
                    entry.InstrumentId = instrumentId;
                    entry.OrderType = GetOrderTypeName(orderType);
                    entry.TimeInForce = GetTimeInForceName(timeInForce);
                    entry.Side = GetSideName(side);
                    entry.Price = (decimal)priceRaw / 100000000m;
                    entry.OriginalQuantity = quantity;
                    entry.CurrentQuantity = quantity; // Will be updated as fills occur
                    entry.ClientOrderId = clientOrderId;
                    
                    System.Diagnostics.Debug.WriteLine($"Populated order details: OrderId={entry.OrderId}, ClientOrderId='{clientOrderId}', InstrumentId={instrumentId}, Side={entry.Side}, Price={entry.Price}, Qty={quantity}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error populating order details: {ex.Message}");
                }
            }
        }

        private Dictionary<string, string> ParseOrderModifyDetails(CcgMessage message)
        {
            var details = new Dictionary<string, string>();
            
            if (message.RawData != null && message.RawData.Length >= 80)
            {
                try
                {
                    // Parse OrderModify fields
                    long priceRaw = BitConverter.ToInt64(message.RawData, 24);
                    long triggerPriceRaw = BitConverter.ToInt64(message.RawData, 32);
                    ulong quantity = BitConverter.ToUInt64(message.RawData, 40);
                    ulong displayQty = BitConverter.ToUInt64(message.RawData, 48);
                    
                    if (priceRaw != 0)
                        details["price"] = ((decimal)priceRaw / 100000000m).ToString("F4");
                    if (triggerPriceRaw != 0)
                        details["triggerPrice"] = ((decimal)triggerPriceRaw / 100000000m).ToString("F4");
                    if (quantity != 0)
                        details["quantity"] = quantity.ToString();
                    if (displayQty != 0)
                        details["displayQty"] = displayQty.ToString();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing OrderModify details: {ex.Message}");
                }
            }
            
            return details;
        }

        private void ApplyModificationToOrder(OrderBookEntry entry, OrderModification modification)
        {
            // Apply successful modification to the order
            if (modification.FieldModified == "Price" && decimal.TryParse(modification.NewValue, out decimal newPrice))
            {
                entry.Price = newPrice;
            }
            else if (modification.FieldModified == "Quantity" && ulong.TryParse(modification.NewValue, out ulong newQuantity))
            {
                entry.OriginalQuantity = newQuantity;
                entry.CurrentQuantity = newQuantity - entry.FilledQuantity;
            }
            else if (modification.FieldModified == "DisplayQty" && ulong.TryParse(modification.NewValue, out ulong newDisplayQty))
            {
                entry.DisplayQuantity = newDisplayQty;
            }
        }

        private void MapInstrumentData(OrderBookEntry order)
        {
            if (order.InstrumentId.HasValue && _instrumentsService.Instruments.Count > 0)
            {
                var instrument = _instrumentsService.Instruments.FirstOrDefault(
                    i => i.InstrumentID == order.InstrumentId.Value);
                
                if (instrument != null)
                {
                    order.ISIN = instrument.ISIN;
                    order.ProductCode = instrument.ProductCode;
                }
            }
        }

        // Status and enum conversion methods
        private string GetOrderStatusName(byte status)
        {
            return status switch
            {
                1 => "New",
                2 => "Cancelled",
                3 => "Rejected",
                4 => "Filled",
                5 => "PartiallyFilled",
                6 => "Expired",
                _ => "Unknown"
            };
        }

        private string GetExecTypeReasonName(byte reason)
        {
            return reason switch
            {
                1 => "NA",
                2 => "CancelOnDisconnect",
                3 => "Expired",
                4 => "Triggered",
                5 => "CancelOnSuspension",
                6 => "OrderRestatement",
                7 => "IcebergOrderRefill",
                8 => "CancelByStp",
                9 => "CancelByCorporateAction",
                10 => "CancelByMassCancel",
                11 => "CancelIocFokOrder",
                12 => "CancelByMarketOperations",
                13 => "Replaced",
                14 => "FirstTradeOnAggressiveOrder",
                15 => "Rejected",
                16 => "CancelonBuyOnlyStateEntry",
                17 => "CancelonKnockedOutStateEntry",
                18 => "CancelByRiskManagement",
                19 => "CancelOnDcDisconnect",
                _ => "Unknown"
            };
        }

        private string GetOrderTypeName(byte orderType)
        {
            return orderType switch
            {
                1 => "Limit",
                2 => "Market",
                3 => "MarketToLimit",
                4 => "Iceberg",
                5 => "StopLimit",
                6 => "StopLoss",
                _ => "Unknown"
            };
        }

        private string GetTimeInForceName(byte timeInForce)
        {
            return timeInForce switch
            {
                1 => "Day",
                2 => "GTC",
                3 => "IOC",
                4 => "FOK",
                5 => "VFA",
                6 => "GTD",
                7 => "VFC",
                8 => "GTT",
                _ => "Unknown"
            };
        }

        private string GetSideName(byte side)
        {
            return side switch
            {
                1 => "Buy",
                2 => "Sell",
                _ => "Unknown"
            };
        }

        private string GetRejectReasonName(ushort reason)
        {
            // This would be a large switch based on GPW WATS reject codes
            // For now, just return the code
            return $"Code_{reason}";
        }

        // Public methods
        public void ClearOrderBook()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _orderBook.Clear();
                _ordersByClientOrderId.Clear();
                _pendingOrderAdds.Clear();
                _orders.Clear();
                OrderBookCleared?.Invoke();
            });
        }

        public OrderBookEntry GetOrder(ulong orderId)
        {
            return _orderBook.TryGetValue(orderId, out var order) ? order : null;
        }

        public OrderBookEntry GetOrderByClientOrderId(string clientOrderId)
        {
            return _ordersByClientOrderId.TryGetValue(clientOrderId, out var order) ? order : null;
        }

        public IEnumerable<OrderBookEntry> GetOrdersByInstrument(uint instrumentId)
        {
            return _orders.Where(o => o.InstrumentId == instrumentId);
        }

        public IEnumerable<OrderBookEntry> GetOrdersByStatus(string status)
        {
            return _orders.Where(o => o.Status == status);
        }

        // Statistics methods
        public (int Total, int Active, int Filled, int Cancelled) GetOrderStatistics()
        {
            int total = _orders.Count;
            int active = _orders.Count(o => o.Status == "New" || o.Status == "PartiallyFilled");
            int filled = _orders.Count(o => o.Status == "Filled");
            int cancelled = _orders.Count(o => o.Status == "Cancelled");
            
            return (total, active, filled, cancelled);
        }
    }
}