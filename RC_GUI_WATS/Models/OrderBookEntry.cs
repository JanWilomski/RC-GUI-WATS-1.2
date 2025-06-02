// Models/OrderBookEntry.cs
using System;
using System.Collections.ObjectModel;
using RC_GUI_WATS.ViewModels;

namespace RC_GUI_WATS.Models
{
    public class OrderBookEntry : BaseViewModel
    {
        private ulong _orderId;
        public ulong OrderId
        {
            get => _orderId;
            set => SetProperty(ref _orderId, value);
        }

        private ulong? _publicOrderId;
        public ulong? PublicOrderId
        {
            get => _publicOrderId;
            set => SetProperty(ref _publicOrderId, value);
        }

        private uint? _instrumentId;
        public uint? InstrumentId
        {
            get => _instrumentId;
            set => SetProperty(ref _instrumentId, value);
        }

        private string _isin;
        public string ISIN
        {
            get => _isin;
            set => SetProperty(ref _isin, value);
        }

        private string _productCode;
        public string ProductCode
        {
            get => _productCode;
            set => SetProperty(ref _productCode, value);
        }

        private string _side;
        public string Side
        {
            get => _side;
            set => SetProperty(ref _side, value);
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private ulong _originalQuantity;
        public ulong OriginalQuantity
        {
            get => _originalQuantity;
            set => SetProperty(ref _originalQuantity, value);
        }

        private ulong _currentQuantity;
        public ulong CurrentQuantity
        {
            get => _currentQuantity;
            set => SetProperty(ref _currentQuantity, value);
        }

        private ulong _filledQuantity;
        public ulong FilledQuantity
        {
            get => _filledQuantity;
            set => SetProperty(ref _filledQuantity, value);
        }

        private ulong _displayQuantity;
        public ulong DisplayQuantity
        {
            get => _displayQuantity;
            set => SetProperty(ref _displayQuantity, value);
        }

        private string _orderType;
        public string OrderType
        {
            get => _orderType;
            set => SetProperty(ref _orderType, value);
        }

        private string _timeInForce;
        public string TimeInForce
        {
            get => _timeInForce;
            set => SetProperty(ref _timeInForce, value);
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private DateTime _createdTime;
        public DateTime CreatedTime
        {
            get => _createdTime;
            set => SetProperty(ref _createdTime, value);
        }

        private DateTime _lastUpdateTime;
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        private string _clientOrderId;
        public string ClientOrderId
        {
            get => _clientOrderId;
            set => SetProperty(ref _clientOrderId, value);
        }

        private int _modificationCount;
        public int ModificationCount
        {
            get => _modificationCount;
            set => SetProperty(ref _modificationCount, value);
        }

        private int _tradeCount;
        public int TradeCount
        {
            get => _tradeCount;
            set => SetProperty(ref _tradeCount, value);
        }

        // History of all related CCG messages
        public ObservableCollection<OrderEvent> Events { get; } = new ObservableCollection<OrderEvent>();

        // Display helpers
        public string PriceDisplay => Price.ToString("F4");
        public string FilledDisplay => $"{FilledQuantity}/{OriginalQuantity}";
        public string FillPercentage => OriginalQuantity > 0 
            ? $"{(FilledQuantity * 100.0 / OriginalQuantity):F1}%" 
            : "0%";
    }

    public class OrderEvent : BaseViewModel
    {
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } // Add, Modify, Cancel, Trade, etc.
        public string Description { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public uint? SequenceNumber { get; set; }
    }
}