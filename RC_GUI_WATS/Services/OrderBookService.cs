// Services/OrderBookService.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class OrderBookService
    {
        private readonly CcgMessagesService _ccgMessagesService;
        private readonly InstrumentsService _instrumentsService;
        private readonly Dictionary<ulong, OrderBookEntry> _ordersByOrderId = new Dictionary<ulong, OrderBookEntry>();
        private readonly ObservableCollection<OrderBookEntry> _orderBook = new ObservableCollection<OrderBookEntry>();

        public ObservableCollection<OrderBookEntry> OrderBook => _orderBook;

        public event Action<OrderBookEntry> OrderUpdated;
        public event Action OrderBookCleared;

        public OrderBookService(CcgMessagesService ccgMessagesService, InstrumentsService instrumentsService)
        {
            _ccgMessagesService = ccgMessagesService;
            _instrumentsService = instrumentsService;
            
            // Subscribe to new CCG messages
            _ccgMessagesService.NewCcgMessageReceived += ProcessCcgMessage;
            _ccgMessagesService.MessagesCleared += OnMessagesCleared;

            // Process existing messages
            RebuildOrderBook();
        }

        private void ProcessCcgMessage(CcgMessage message)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                switch (message.Name)
                {
                    case "OrderAdd":
                        // OrderAdd doesn't contain orderId, we need to wait for response
                        break;
                        
                    case "OrderAddResponse":
                        ProcessOrderAddResponse(message);
                        break;
                        
                    case "OrderModify":
                    case "OrderModifyResponse":
                        ProcessOrderModify(message);
                        break;
                        
                    case "OrderCancel":
                    case "OrderCancelResponse":
                        ProcessOrderCancel(message);
                        break;
                        
                    case "Trade":
                        ProcessTrade(message);
                        break;
                }
            });
        }

        private void ProcessOrderAddResponse(CcgMessage message)
        {
            if (string.IsNullOrEmpty(message.ClientOrderId))
                return;

            // Try to parse orderId from ClientOrderId
            if (ulong.TryParse(message.ClientOrderId, out ulong orderId))
            {
                // Check if we already have this order
                if (!_ordersByOrderId.ContainsKey(orderId))
                {
                    // Find the corresponding OrderAdd message by sequence number
                    var orderAddMsg = FindOrderAddMessage(message.SequenceNumber);
                    
                    var entry = new OrderBookEntry
                    {
                        OrderId = orderId,
                        CreatedTime = message.DateReceived,
                        LastUpdateTime = message.DateReceived,
                        Status = GetStatusName(message)
                    };

                    // Extract data from the OrderAdd message if found
                    if (orderAddMsg != null)
                    {
                        entry.InstrumentId = orderAddMsg.InstrumentId;
                        entry.Side = orderAddMsg.Side;
                        entry.Price = orderAddMsg.Price ?? 0;
                        entry.OriginalQuantity = orderAddMsg.Quantity ?? 0;
                        entry.CurrentQuantity = orderAddMsg.Quantity ?? 0;
                        entry.ClientOrderId = orderAddMsg.ClientOrderId;
                        entry.OrderType = GetOrderTypeName(orderAddMsg);
                        entry.TimeInForce = GetTimeInForceName(orderAddMsg);
                    }

                    // Extract additional data from response
                    entry.PublicOrderId = ExtractPublicOrderId(message);
                    entry.DisplayQuantity = message.Quantity ?? 0;
                    entry.FilledQuantity = ExtractFilledQuantity(message);

                    // Map instrument data
                    MapInstrumentData(entry);

                    // Add event
                    entry.Events.Add(new OrderEvent
                    {
                        Timestamp = message.DateReceived,
                        EventType = "Created",
                        Description = $"Order created with status: {entry.Status}",
                        SequenceNumber = message.SequenceNumber
                    });

                    _ordersByOrderId[orderId] = entry;
                    _orderBook.Insert(0, entry); // Insert at beginning for newest first
                    
                    OrderUpdated?.Invoke(entry);
                }
            }
        }

        private void ProcessOrderModify(CcgMessage message)
        {
            if (string.IsNullOrEmpty(message.ClientOrderId))
                return;

            if (ulong.TryParse(message.ClientOrderId, out ulong orderId))
            {
                if (_ordersByOrderId.TryGetValue(orderId, out OrderBookEntry entry))
                {
                    var oldPrice = entry.Price;
                    var oldQuantity = entry.CurrentQuantity;

                    // Update order data
                    if (message.Price.HasValue && message.Price.Value != 0)
                        entry.Price = message.Price.Value;
                    
                    if (message.Quantity.HasValue && message.Quantity.Value != 0)
                        entry.CurrentQuantity = message.Quantity.Value;
                    
                    entry.LastUpdateTime = message.DateReceived;
                    entry.ModificationCount++;

                    // Add event
                    var description = "Order modified:";
                    if (oldPrice != entry.Price)
                        description += $" Price {oldPrice:F4} → {entry.Price:F4}";
                    if (oldQuantity != entry.CurrentQuantity)
                        description += $" Qty {oldQuantity} → {entry.CurrentQuantity}";

                    entry.Events.Add(new OrderEvent
                    {
                        Timestamp = message.DateReceived,
                        EventType = "Modified",
                        Description = description,
                        OldValue = $"P:{oldPrice:F4} Q:{oldQuantity}",
                        NewValue = $"P:{entry.Price:F4} Q:{entry.CurrentQuantity}",
                        SequenceNumber = message.SequenceNumber
                    });

                    OrderUpdated?.Invoke(entry);
                }
            }
        }

        private void ProcessOrderCancel(CcgMessage message)
        {
            if (string.IsNullOrEmpty(message.ClientOrderId))
                return;

            if (ulong.TryParse(message.ClientOrderId, out ulong orderId))
            {
                if (_ordersByOrderId.TryGetValue(orderId, out OrderBookEntry entry))
                {
                    entry.Status = "Cancelled";
                    entry.CurrentQuantity = 0;
                    entry.LastUpdateTime = message.DateReceived;

                    // Add event
                    entry.Events.Add(new OrderEvent
                    {
                        Timestamp = message.DateReceived,
                        EventType = "Cancelled",
                        Description = "Order cancelled",
                        SequenceNumber = message.SequenceNumber
                    });

                    OrderUpdated?.Invoke(entry);
                }
            }
        }

        private void ProcessTrade(CcgMessage message)
        {
            if (string.IsNullOrEmpty(message.ClientOrderId))
                return;

            if (ulong.TryParse(message.ClientOrderId, out ulong orderId))
            {
                if (_ordersByOrderId.TryGetValue(orderId, out OrderBookEntry entry))
                {
                    var tradeQty = message.Quantity ?? 0;
                    entry.FilledQuantity += tradeQty;
                    entry.CurrentQuantity = entry.CurrentQuantity > tradeQty 
                        ? entry.CurrentQuantity - tradeQty 
                        : 0;
                    
                    entry.TradeCount++;
                    entry.LastUpdateTime = message.DateReceived;
                    
                    // Update status
                    if (entry.FilledQuantity >= entry.OriginalQuantity)
                        entry.Status = "Filled";
                    else if (entry.FilledQuantity > 0)
                        entry.Status = "PartiallyFilled";

                    // Add event
                    entry.Events.Add(new OrderEvent
                    {
                        Timestamp = message.DateReceived,
                        EventType = "Trade",
                        Description = $"Executed {tradeQty} @ {message.Price?.ToString("F4") ?? "?"}",
                        SequenceNumber = message.SequenceNumber
                    });

                    OrderUpdated?.Invoke(entry);
                }
            }
        }

        private void MapInstrumentData(OrderBookEntry entry)
        {
            if (entry.InstrumentId.HasValue && _instrumentsService.Instruments.Count > 0)
            {
                var instrument = _instrumentsService.Instruments.FirstOrDefault(
                    i => i.InstrumentID == entry.InstrumentId.Value);

                if (instrument != null)
                {
                    entry.ISIN = instrument.ISIN;
                    entry.ProductCode = instrument.ProductCode;
                }
            }
        }

        private CcgMessage FindOrderAddMessage(uint responseSeqNum)
        {
            // OrderAdd should have sequence number = responseSeqNum - 1 (typically)
            return _ccgMessagesService.CcgMessages
                .FirstOrDefault(m => m.Name == "OrderAdd" && 
                                   m.SequenceNumber >= responseSeqNum - 5 && 
                                   m.SequenceNumber < responseSeqNum);
        }

        private ulong? ExtractPublicOrderId(CcgMessage message)
        {
            // Parse from raw data if needed
            // For now, return null
            return null;
        }

        private ulong ExtractFilledQuantity(CcgMessage message)
        {
            // Parse from raw data if available
            // OrderAddResponse has filled field at specific offset
            return 0;
        }

        private string GetStatusName(CcgMessage message)
        {
            // Based on documentation, extract status from raw data
            // For now, return default
            return "New";
        }

        private string GetOrderTypeName(CcgMessage message)
        {
            // Extract from raw data based on documentation
            return "Limit";
        }

        private string GetTimeInForceName(CcgMessage message)
        {
            // Extract from raw data
            return "DAY";
        }

        public void ClearOrderBook()
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _orderBook.Clear();
                _ordersByOrderId.Clear();
                OrderBookCleared?.Invoke();
            });
        }

        private void OnMessagesCleared()
        {
            ClearOrderBook();
        }

        public void RebuildOrderBook()
        {
            ClearOrderBook();
            
            // Process all existing CCG messages in order
            foreach (var message in _ccgMessagesService.CcgMessages.Reverse())
            {
                ProcessCcgMessage(message);
            }
        }

        // Statistics
        public (int Active, int Filled, int Cancelled, int Total) GetOrderStatistics()
        {
            int active = 0, filled = 0, cancelled = 0;

            foreach (var order in _orderBook)
            {
                switch (order.Status)
                {
                    case "New":
                    case "PartiallyFilled":
                        active++;
                        break;
                    case "Filled":
                        filled++;
                        break;
                    case "Cancelled":
                    case "Expired":
                    case "Rejected":
                        cancelled++;
                        break;
                }
            }

            return (active, filled, cancelled, _orderBook.Count);
        }
    }
}