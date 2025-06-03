// Services/OrderBookService.cs - Enhanced version with better modifications handling
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class OrderBookService
    {
        private readonly CcgMessagesService _ccgMessagesService;
        private readonly InstrumentsService _instrumentsService;
        private readonly Dictionary<ulong, OrderBookEntry> _orderBook = new Dictionary<ulong, OrderBookEntry>();
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
                    }
                });
            }
        }

        private void OnCcgMessagesCleared()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _orderBook.Clear();
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
            // We'll get the orderId from OrderAddResponse, but we can store some initial data
            // from the original OrderAdd request for reference
            System.Diagnostics.Debug.WriteLine($"OrderAdd received: ClientOrderId={message.ClientOrderId}, InstrumentId={message.InstrumentId}, Side={message.Side}, Price={message.Price}, Qty={message.Quantity}");
        }

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
                        
                        // For new orders, try to get more info from the original OrderAdd
                        if (entry.OriginalQuantity == 0)
                        {
                            // Try to find corresponding OrderAdd message to get full order details
                            var orderAddMessage = FindCorrespondingOrderAdd(message);
                            if (orderAddMessage != null)
                            {
                                PopulateOrderDetailsFromOrderAdd(entry, orderAddMessage);
                            }
                        }
                        
                        // Calculate current quantity
                        if (entry.OriginalQuantity > 0)
                        {
                            entry.CurrentQuantity = entry.OriginalQuantity - filled;
                        }
                        
                        entry.LastModifiedTime = message.DateReceived;
                        
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
            if (!ulong.TryParse(message.ClientOrderId, out ulong orderId))
                return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_orderBook.TryGetValue(orderId, out var entry))
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
                    
                    entry.Modifications.Add(modification);
                    entry.LastModifiedTime = message.DateReceived;
                    OrderUpdated?.Invoke(entry);
                    
                    System.Diagnostics.Debug.WriteLine($"OrderModify: OrderId={orderId}, Field={modification.FieldModified}, Old={modification.OldValue}, New={modification.NewValue}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OrderModify: Order {orderId} not found in order book");
                }
            });
        }

        private void ProcessOrderModifyResponse(CcgMessage message)
        {
            if (!ulong.TryParse(message.ClientOrderId, out ulong orderId))
                return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_orderBook.TryGetValue(orderId, out var entry))
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
                            
                            // Update the latest modification with response
                            var latestMod = entry.Modifications.LastOrDefault();
                            if (latestMod != null && latestMod.Status == "Pending")
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
                            
                            // Update filled quantity
                            entry.FilledQuantity = filled;
                            entry.CurrentQuantity = entry.OriginalQuantity - filled;
                            entry.Status = GetOrderStatusName(status);
                            entry.LastModifiedTime = message.DateReceived;
                            
                            OrderUpdated?.Invoke(entry);
                            
                            System.Diagnostics.Debug.WriteLine($"OrderModifyResponse: OrderId={orderId}, Status={GetOrderStatusName(status)}, Priority={(priorityFlag == 2 ? "Retained" : "Lost")}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing OrderModifyResponse: {ex.Message}");
                        }
                    }
                }
            });
        }

        private void ProcessOrderCancel(CcgMessage message)
        {
            if (!ulong.TryParse(message.ClientOrderId, out ulong orderId))
                return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_orderBook.TryGetValue(orderId, out var entry))
                {
                    var cancelAttempt = new OrderCancelAttempt
                    {
                        CancelTime = message.DateReceived,
                        Status = "Pending",
                        CancelReason = "User Request"
                    };
                    
                    entry.CancelAttempts.Add(cancelAttempt);
                    entry.LastModifiedTime = message.DateReceived;
                    OrderUpdated?.Invoke(entry);
                }
            });
        }

        private void ProcessOrderCancelResponse(CcgMessage message)
        {
            if (!ulong.TryParse(message.ClientOrderId, out ulong orderId))
                return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_orderBook.TryGetValue(orderId, out var entry))
                {
                    // Parse cancel response
                    if (message.RawData != null && message.RawData.Length >= 28)
                    {
                        try
                        {
                            byte status = message.RawData[24];
                            ushort reason = BitConverter.ToUInt16(message.RawData, 25);
                            byte execTypeReason = message.RawData[27];
                            
                            // Update the latest cancel attempt
                            var latestCancel = entry.CancelAttempts.LastOrDefault();
                            if (latestCancel != null && latestCancel.Status == "Pending")
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
                            
                            entry.Status = GetOrderStatusName(status);
                            entry.LastExecTypeReason = GetExecTypeReasonName(execTypeReason);
                            entry.LastModifiedTime = message.DateReceived;
                            OrderUpdated?.Invoke(entry);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing OrderCancelResponse: {ex.Message}");
                        }
                    }
                }
            });
        }

        private void ProcessTrade(CcgMessage message)
        {
            if (!ulong.TryParse(message.ClientOrderId, out ulong orderId))
                return;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_orderBook.TryGetValue(orderId, out var entry))
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
                            
                            entry.Trades.Add(trade);
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
                            OrderUpdated?.Invoke(entry);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing Trade: {ex.Message}");
                        }
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
                    }
                }
                
                _orders.Insert(0, entry); // Add to beginning for newest-first display
            }
            
            return entry;
        }

        private CcgMessage FindCorrespondingOrderAdd(CcgMessage orderAddResponse)
        {
            // Find the most recent OrderAdd message that could correspond to this response
            // This is a simplified approach - in a real system you'd want more sophisticated matching
            var recentMessages = _ccgMessagesService.CcgMessages.Take(50);
            return recentMessages.FirstOrDefault(m => m.Name == "OrderAdd");
        }

        private void PopulateOrderDetailsFromOrderAdd(OrderBookEntry entry, CcgMessage orderAddMessage)
        {
            if (orderAddMessage.RawData != null && orderAddMessage.RawData.Length >= 167)
            {
                try
                {
                    // Parse key fields from OrderAdd
                    uint instrumentId = BitConverter.ToUInt32(orderAddMessage.RawData, 17);
                    byte orderType = orderAddMessage.RawData[21];
                    byte timeInForce = orderAddMessage.RawData[22];
                    byte side = orderAddMessage.RawData[23];
                    long priceRaw = BitConverter.ToInt64(orderAddMessage.RawData, 24);
                    ulong quantity = BitConverter.ToUInt64(orderAddMessage.RawData, 40);
                    
                    entry.InstrumentId = instrumentId;
                    entry.OrderType = GetOrderTypeName(orderType);
                    entry.TimeInForce = GetTimeInForceName(timeInForce);
                    entry.Side = GetSideName(side);
                    entry.Price = (decimal)priceRaw / 100000000m;
                    entry.OriginalQuantity = quantity;
                    entry.CurrentQuantity = quantity; // Will be updated as fills occur
                    
                    System.Diagnostics.Debug.WriteLine($"Populated order details: OrderId={entry.OrderId}, InstrumentId={instrumentId}, Side={entry.Side}, Price={entry.Price}, Qty={quantity}");
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
                _orders.Clear();
                OrderBookCleared?.Invoke();
            });
        }

        public OrderBookEntry GetOrder(ulong orderId)
        {
            return _orderBook.TryGetValue(orderId, out var order) ? order : null;
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