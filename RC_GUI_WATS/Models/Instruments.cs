using System;

namespace RC_GUI_WATS.Models
{
    public class Instrument
    {
        public int InstrumentID { get; set; }
        public string ISIN { get; set; }
        public string ProductCode { get; set; }
        public double ReferencePrice { get; set; }
        public double Multiplier { get; set; }
        public string MIC { get; set; }
        public int InstrumentTypeID { get; set; }
        public string InstrumentType { get; set; }
        public int InstrumentSubtypeID { get; set; }
        public string InstrumentSubtype { get; set; }
        public int MarketStructureID { get; set; }
        public string MarketStructureName { get; set; }
        public string Currency { get; set; }
        public int CollarGroupId { get; set; }
        public int OrderStaticCollarModeId { get; set; }
        public string OrderStaticCollarMode { get; set; }
        public int OrderStaticCollarExpressionTypeId { get; set; }
        public string OrderStaticCollarExpressionType { get; set; }
        public double OrderStaticCollarLowerBound { get; set; }
        public double OrderStaticCollarValue { get; set; }
        public double OrderStaticCollarLowerBid { get; set; }
        public double OrderStaticCollarUpperBid { get; set; }
        public double OrderStaticCollarUpperAsk { get; set; }
        public double OrderStaticCollarLowerAsk { get; set; }
        public DateTime FirstTradingDate { get; set; }
        public DateTime LastTradingDate { get; set; }
        public int ProductID { get; set; }
        public int LotSize { get; set; }
        public int PriceExpressionType { get; set; }
        public int CalendarID { get; set; }
        public int TickTableID { get; set; }
        public int TradingScheduleID { get; set; }
        public int NominalValueType { get; set; }
        public double StrikePrice { get; set; }
        public int SettlementCalendarID { get; set; }
        public int BondCouponType { get; set; }
        public bool Liquidity { get; set; }
        public int MarketModelTypeID { get; set; }
        public string MarketModelType { get; set; }
        public string IssuerRegCountry { get; set; }
        public int UnderlyingInstrumentID { get; set; }
        public string CFICode { get; set; }
        public int IssueSize { get; set; }
        public string NominalCurrency { get; set; }
        public int USIndicator { get; set; }
        public int ExpiryDate { get; set; }
        public int VersionNumber { get; set; }
        public int SettlementType { get; set; }
        public int OptionType { get; set; }
        public int ExerciseType { get; set; }
        public string ProductName { get; set; }
        public int Status { get; set; }
        public int InitialPhaseID { get; set; }
        public double ThresholdMax { get; set; }
        public double ThresholdMin { get; set; }
        public bool IsLeverage { get; set; }
        public double NominalValue { get; set; }
    }
}