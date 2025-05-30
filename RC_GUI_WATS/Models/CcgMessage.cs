// Models/CcgMessage.cs
using System;
using System.Linq;
namespace RC_GUI_WATS.Models
{
    public class CcgMessage
    {
        public string Header { get; set; } // Message Type ID
        public string Name { get; set; } // Message Type Name
        public DateTime DateReceived { get; set; } // When we received it
        public DateTime TransactTime { get; set; } // Timestamp from message
        public decimal? Price { get; set; } // Price (for orders)
        public ulong? Quantity { get; set; } // Quantity (for orders/trades)
        public string Side { get; set; } // Buy/Sell (for orders)
        public uint? InstrumentId { get; set; } // Instrument ID
        public string ClientOrderId { get; set; } // Client Order ID or OrderId reference
        public byte[] RawData { get; set; } // Original binary data
        public uint SequenceNumber { get; set; } // Message sequence number
        // New properties for instrument mapping
        public string ISIN { get; set; } // ISIN mapped from InstrumentId
        public string ProductCode { get; set; } // Product Code mapped from InstrumentId
        
        // Additional helper properties
        public string PriceDisplay => Price?.ToString("F4") ?? "";
        public string QuantityDisplay => Quantity?.ToString() ?? "";
        public string InstrumentIdDisplay => InstrumentId?.ToString() ?? "";
        public string TransactTimeDisplay => TransactTime.ToString("HH:mm:ss.fff");
        public string DateReceivedDisplay => DateReceived.ToString("HH:mm:ss.fff");
        public string RawDataHex => RawData != null ? BitConverter.ToString(RawData).Replace("-", " ") : "";
        public string RawDataShort => RawData != null ? BitConverter.ToString(RawData.Take(20).ToArray()).Replace("-", " ") + (RawData.Length > 20 ? "..." : "") : "";
        
        // Display properties for new columns
        public string ISINDisplay => string.IsNullOrEmpty(ISIN) ? "" : ISIN;
        public string ProductCodeDisplay => string.IsNullOrEmpty(ProductCode) ? "" : ProductCode;
    }

    // Enums from GPW WATS documentation
    public enum CcgMessageType : ushort
    {
        Login = 2,
        LoginResponse = 3,
        OrderAdd = 4,
        OrderAddResponse = 5,
        OrderCancel = 6,
        OrderCancelResponse = 7,
        OrderModify = 8,
        OrderModifyResponse = 9,
        Trade = 10,
        Logout = 11,
        ConnectionClose = 12,
        Heartbeat = 13,
        LogoutResponse = 14,
        Reject = 15,
        TradeCaptureReportSingle = 18,
        TradeCaptureReportDual = 19,
        TradeCaptureReportResponse = 20,
        TradeBust = 23,
        MassQuote = 24,
        MassQuoteResponse = 25,
        RequestForExecution = 28,
        OrderMassCancel = 29,
        OrderMassCancelResponse = 30,
        BidOfferUpdate = 31,
        MarketMakerCommand = 32,
        MarketMakerCommandResponse = 33,
        GapFill = 34
    }

    public enum OrderSide : byte
    {
        Buy = 1,
        Sell = 2
    }

    public enum OrderType : byte
    {
        Limit = 1,
        Market = 2,
        MarketToLimit = 3,
        Iceberg = 4,
        StopLimit = 5,
        StopLoss = 6
    }
}