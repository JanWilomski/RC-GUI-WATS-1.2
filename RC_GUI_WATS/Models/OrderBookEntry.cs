// Models/OrderBookEntry.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RC_GUI_WATS.Models
{
    public class OrderBookEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Identifiers
        public ulong OrderId { get; set; }
        public ulong? PublicOrderId { get; set; }
        public string ClientOrderId { get; set; } = "";
        
        // Instrument info
        public uint? InstrumentId { get; set; }
        public string ISIN { get; set; } = "";
        public string ProductCode { get; set; } = "";
        
        // Order details
        public string Side { get; set; } = ""; // Buy/Sell
        public string OrderType { get; set; } = ""; // Limit, Market, etc.
        public string TimeInForce { get; set; } = ""; // Day, GTC, etc.
        
        // Quantities
        public ulong OriginalQuantity { get; set; }
        public ulong CurrentQuantity { get; set; } // Original - Filled
        public ulong FilledQuantity { get; set; }
        public ulong? DisplayQuantity { get; set; } // For iceberg orders
        
        // Prices
        public decimal? Price { get; set; }
        public decimal? TriggerPrice { get; set; } // For stop orders
        
        // Status
        private string _status = "";
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }
        
        private string _lastExecTypeReason = "";
        public string LastExecTypeReason
        {
            get => _lastExecTypeReason;
            set
            {
                _lastExecTypeReason = value;
                OnPropertyChanged(nameof(LastExecTypeReason));
            }
        }
        
        // Timestamps
        public DateTime CreatedTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
        
        // Collections for tracking changes
        public List<OrderTrade> Trades { get; set; } = new List<OrderTrade>();
        public List<OrderModification> Modifications { get; set; } = new List<OrderModification>();
        public List<OrderCancelAttempt> CancelAttempts { get; set; } = new List<OrderCancelAttempt>();
        
        // Display properties
        public string StatusDisplay => GetStatusDisplay();
        public string PriceDisplay => Price?.ToString("F4") ?? "";
        public string TriggerPriceDisplay => TriggerPrice?.ToString("F4") ?? "";
        public string QuantityInfo => $"{FilledQuantity}/{OriginalQuantity}";
        public string CreatedTimeDisplay => CreatedTime.ToString("HH:mm:ss.fff");
        public string LastModifiedTimeDisplay => LastModifiedTime.ToString("HH:mm:ss.fff");
        public int TradeCount => Trades.Count;
        public int ModificationCount => Modifications.Count;
        public string InstrumentDisplay => !string.IsNullOrEmpty(ProductCode) ? ProductCode : InstrumentId?.ToString() ?? "";
        
        private string GetStatusDisplay()
        {
            if (!string.IsNullOrEmpty(LastExecTypeReason) && LastExecTypeReason != "NA")
            {
                return $"{Status} ({LastExecTypeReason})";
            }
            return Status;
        }
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class OrderTrade
    {
        public uint TradeId { get; set; }
        public decimal Price { get; set; }
        public ulong Quantity { get; set; }
        public ulong LeavesQuantity { get; set; }
        public DateTime ExecutionTime { get; set; }
        
        public string PriceDisplay => Price.ToString("F4");
        public string ExecutionTimeDisplay => ExecutionTime.ToString("HH:mm:ss.fff");
    }

    public class OrderModification
    {
        public DateTime ModificationTime { get; set; }
        public string ModificationType { get; set; } = ""; // Price, Quantity, etc.
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string Status { get; set; } = ""; // Accepted, Rejected
        public string RejectReason { get; set; } = "";
        
        public string ModificationTimeDisplay => ModificationTime.ToString("HH:mm:ss.fff");
        public string ChangeDescription => $"{ModificationType}: {OldValue} → {NewValue}";
    }

    public class OrderCancelAttempt
    {
        public DateTime CancelTime { get; set; }
        public string Status { get; set; } = ""; // Accepted, Rejected
        public string RejectReason { get; set; } = "";
        public string CancelReason { get; set; } = ""; // User, System, etc.
        
        public string CancelTimeDisplay => CancelTime.ToString("HH:mm:ss.fff");
    }

    // Enums for better type safety
    public enum OrderStatus
    {
        Unknown,
        New,
        PartiallyFilled,
        Filled,
        Cancelled,
        Rejected,
        Expired
    }

    public enum OrderSideEnum
    {
        Unknown,
        Buy,
        Sell
    }

    public enum OrderTypeEnum
    {
        Unknown,
        Limit,
        Market,
        MarketToLimit,
        Iceberg,
        StopLimit,
        StopLoss
    }
}