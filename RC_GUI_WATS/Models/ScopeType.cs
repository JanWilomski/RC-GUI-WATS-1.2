// Models/ScopeType.cs
namespace RC_GUI_WATS.Models
{
    public enum ScopeType
    {
        AllInstruments = 0,     // (ALL)
        InstrumentType = 1,     // (EQUITY), (BOND), etc.
        InstrumentGroup = 2,    // [11*], [ABC*], etc.
        SingleInstrument = 3    // PLPKO0000016, etc.
    }
}