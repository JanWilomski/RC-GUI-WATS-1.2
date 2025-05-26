// Models/CcgMessage.cs
using System;

namespace RC_GUI_WATS.Models
{
    public class CcgMessage
    {
        // Core message properties
        public string Header { get; set; } = "";
        public string Name { get; set; } = "";
        public uint MsgSeqNum { get; set; }
        public DateTime DateReceived { get; set; }
        public DateTime TransactTime { get; set; }
        public decimal? Price { get; set; }
        public string Side { get; set; } = "";
        public string Symbol { get; set; } = ""; // ISIN or instrument identifier
        public string ClientOrderID { get; set; } = "";
        public string RawMessage { get; set; } = "";
        public byte[] RawData { get; set; }
        
        // Rewind and tracking properties
        public bool IsHistorical { get; set; }
        public uint RcSequenceNumber { get; set; }
        
        // Helper properties for UI display
        public string DateReceivedString => DateReceived.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public string TransactTimeString => TransactTime.ToString("HH:mm:ss.fff");
        public string PriceString => Price?.ToString("F8") ?? "";
        public string TypeIndicator => IsHistorical ? "[H]" : "[L]"; // [H]istorical or [L]ive
        
        // Constructor
        public CcgMessage()
        {
            DateReceived = DateTime.Now;
            TransactTime = DateTime.Now;
            RawData = new byte[0];
        }
    }
}