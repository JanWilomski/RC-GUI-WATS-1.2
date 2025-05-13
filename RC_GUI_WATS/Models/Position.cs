// Models/Position.cs
namespace RC_GUI_WATS.Models
{
    public class Position
    {
        public string ISIN { get; set; }
        public string Ticker { get; set; } = "";
        public string Name { get; set; } = "";
        public int Net { get; set; }
        public int OpenLong { get; set; }
        public int OpenShort { get; set; }
    }
}