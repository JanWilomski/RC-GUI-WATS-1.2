// Models/OrderBookEntry.cs - Enhanced version with better modification support and UI notifications
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
                OnPropertyChanged(nameof(StatusDisplay));
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
        
        // Advanced modification info
        public string ModificationsDisplay => GetModificationsDisplay();
        public string LastModificationDisplay => GetLastModificationDisplay();
        public bool HasModifications => Modifications.Count > 0;
        public bool HasSuccessfulModifications => Modifications.Any(m => m.Status == "Accepted");
        public bool HasRejectedModifications => Modifications.Any(m => m.Status == "Rejected");
        public bool HasPendingModifications => Modifications.Any(m => m.Status == "Pending");
        
        // Cancel attempts info
        public int CancelAttemptCount => CancelAttempts.Count;
        public bool HasCancelAttempts => CancelAttempts.Count > 0;
        public bool HasPendingCancels => CancelAttempts.Any(c => c.Status == "Pending");
        
        // Trade info
        public decimal? AveragePrice => GetAveragePrice();
        public string TradesDisplay => GetTradesDisplay();

        // Methods to notify about collection changes
        public void AddModification(OrderModification modification)
        {
            Modifications.Add(modification);
            NotifyModificationsChanged();
        }

        public void UpdateLastModification(Action<OrderModification> updateAction)
        {
            var lastMod = Modifications.LastOrDefault();
            if (lastMod != null)
            {
                updateAction(lastMod);
                NotifyModificationsChanged();
            }
        }

        public void AddTrade(OrderTrade trade)
        {
            Trades.Add(trade);
            NotifyTradesChanged();
        }

        public void AddCancelAttempt(OrderCancelAttempt cancelAttempt)
        {
            CancelAttempts.Add(cancelAttempt);
            NotifyCancelAttemptsChanged();
        }

        public void UpdateLastCancelAttempt(Action<OrderCancelAttempt> updateAction)
        {
            var lastCancel = CancelAttempts.LastOrDefault();
            if (lastCancel != null)
            {
                updateAction(lastCancel);
                NotifyCancelAttemptsChanged();
            }
        }

        private void NotifyModificationsChanged()
        {
            OnPropertyChanged(nameof(ModificationCount));
            OnPropertyChanged(nameof(ModificationsDisplay));
            OnPropertyChanged(nameof(LastModificationDisplay));
            OnPropertyChanged(nameof(HasModifications));
            OnPropertyChanged(nameof(HasSuccessfulModifications));
            OnPropertyChanged(nameof(HasRejectedModifications));
            OnPropertyChanged(nameof(HasPendingModifications));
        }

        private void NotifyTradesChanged()
        {
            OnPropertyChanged(nameof(TradeCount));
            OnPropertyChanged(nameof(TradesDisplay));
            OnPropertyChanged(nameof(AveragePrice));
        }

        private void NotifyCancelAttemptsChanged()
        {
            OnPropertyChanged(nameof(CancelAttemptCount));
            OnPropertyChanged(nameof(HasCancelAttempts));
            OnPropertyChanged(nameof(HasPendingCancels));
        }

        // Method to update basic properties and notify UI
        public void UpdateBasicProperties()
        {
            OnPropertyChanged(nameof(PriceDisplay));
            OnPropertyChanged(nameof(TriggerPriceDisplay));
            OnPropertyChanged(nameof(QuantityInfo));
            OnPropertyChanged(nameof(LastModifiedTimeDisplay));
            OnPropertyChanged(nameof(InstrumentDisplay));
        }
        
        private string GetStatusDisplay()
        {
            if (!string.IsNullOrEmpty(LastExecTypeReason) && LastExecTypeReason != "NA")
            {
                return $"{Status} ({LastExecTypeReason})";
            }
            return Status;
        }
        
        private string GetModificationsDisplay()
        {
            if (Modifications.Count == 0)
                return "None";
            
            var accepted = Modifications.Count(m => m.Status == "Accepted");
            var rejected = Modifications.Count(m => m.Status == "Rejected");
            var pending = Modifications.Count(m => m.Status == "Pending");
            
            var parts = new List<string>();
            if (accepted > 0) parts.Add($"{accepted}✓");
            if (rejected > 0) parts.Add($"{rejected}✗");
            if (pending > 0) parts.Add($"{pending}⏳");
            
            return string.Join(" ", parts);
        }
        
        private string GetLastModificationDisplay()
        {
            var lastMod = Modifications.LastOrDefault();
            if (lastMod == null)
                return "";
            
            var statusIcon = lastMod.Status switch
            {
                "Accepted" => "✓",
                "Rejected" => "✗",
                "Pending" => "⏳",
                _ => "?"
            };
            
            return $"{lastMod.FieldModified} {statusIcon}";
        }
        
        private decimal? GetAveragePrice()
        {
            if (Trades.Count == 0 || FilledQuantity == 0)
                return null;
            
            decimal totalValue = 0;
            ulong totalQuantity = 0;
            
            foreach (var trade in Trades)
            {
                totalValue += trade.Price * (decimal)trade.Quantity;
                totalQuantity += trade.Quantity;
            }
            
            return totalQuantity > 0 ? totalValue / (decimal)totalQuantity : null;
        }
        
        private string GetTradesDisplay()
        {
            if (Trades.Count == 0)
                return "None";
            
            if (Trades.Count == 1)
            {
                var trade = Trades[0];
                return $"1 @ {trade.Price:F4}";
            }
            
            var avgPrice = GetAveragePrice();
            return $"{Trades.Count} @ avg {avgPrice:F4}";
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
        public string TradeDisplay => $"{Quantity} @ {Price:F4}";
    }

    public class OrderModification
    {
        public DateTime ModificationTime { get; set; }
        public string ModificationType { get; set; } = ""; // Price, Quantity, etc.
        public string FieldModified { get; set; } = ""; // Which field was modified
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string Status { get; set; } = ""; // Accepted, Rejected, Pending
        public string RejectReason { get; set; } = "";
        public bool PriorityRetained { get; set; } = false; // From OrderModifyResponse
        public Dictionary<string, string> ModificationDetails { get; set; } = new Dictionary<string, string>();
        
        public string ModificationTimeDisplay => ModificationTime.ToString("HH:mm:ss.fff");
        public string ChangeDescription => $"{FieldModified}: {OldValue} → {NewValue}";
        public string StatusIcon => Status switch
        {
            "Accepted" => "✓",
            "Rejected" => "✗", 
            "Pending" => "⏳",
            _ => "?"
        };
        public string PriorityDisplay => PriorityRetained ? "Retained" : "Lost";
        public string ModificationSummary => $"{ChangeDescription} {StatusIcon}" + 
                                           (Status == "Rejected" && !string.IsNullOrEmpty(RejectReason) ? $" ({RejectReason})" : "");
    }

    public class OrderCancelAttempt
    {
        public DateTime CancelTime { get; set; }
        public string Status { get; set; } = ""; // Accepted, Rejected, Pending
        public string RejectReason { get; set; } = "";
        public string CancelReason { get; set; } = ""; // User, System, etc.
        
        public string CancelTimeDisplay => CancelTime.ToString("HH:mm:ss.fff");
        public string StatusIcon => Status switch
        {
            "Accepted" => "✓",
            "Rejected" => "✗",
            "Pending" => "⏳",
            _ => "?"
        };
        public string CancelSummary => $"{CancelReason} {StatusIcon}" + 
                                     (Status == "Rejected" && !string.IsNullOrEmpty(RejectReason) ? $" ({RejectReason})" : "");
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