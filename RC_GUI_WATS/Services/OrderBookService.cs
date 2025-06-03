// Services/OrderBookService.cs
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
            // OrderAdd doesn't have orderId yet, we'll create the entry when we get OrderAddResponse
            // Just log for debugging
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
                        
                        entry.PublicOrderId = publicOrderId;
                        entry.DisplayQuantity = displayQty;
                        entry.FilledQuantity = filled;
                        entry.Status = GetOrderStatusName(status);
                        
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
                    var modification = new OrderModification
                    {
                        ModificationTime = message.DateReceived,
                        ModificationType = "Modify Request",
                        Status = "Pending"
                    };
                    
                    entry.Modifications.Add(modification);
                    entry.LastModifiedTime = message.DateReceived;
                    OrderUpdated?.Invoke(entry);
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
                    // Update the latest modification with response
                    var latestMod = entry.Modifications.LastOrDefault();
                    if (latestMod != null && latestMod.Status == "Pending")
                    {
                        latestMod.Status = "Accepted"; // Could be parsed from response
                    }
                    
                    entry.LastModifiedTime = message.DateReceived;
                    OrderUpdated?.Invoke(entry);
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
                        CancelReason = "User"
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
                    // Update the latest cancel attempt
                    var latestCancel = entry.CancelAttempts.LastOrDefault();
                    if (latestCancel != null && latestCancel.Status == "Pending")
                    {
                        latestCancel.Status = "Accepted"; // Could be parsed from response
                    }
                    
                    entry.Status = "Cancelled";
                    entry.LastModifiedTime = message.DateReceived;
                    OrderUpdated?.Invoke(entry);
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
            // Handle mass cancel requests
            System.Diagnostics.Debug.WriteLine("OrderMassCancel received");
        }

        private void ProcessOrderMassCancelResponse(CcgMessage message)
        {
            // Handle mass cancel responses
            System.Diagnostics.Debug.WriteLine("OrderMassCancelResponse received");
        }

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