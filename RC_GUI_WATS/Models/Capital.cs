// Models/Capital.cs
namespace RC_GUI_WATS.Models
{
    public class Capital
    {
        public double OpenCapital { get; set; }
        public double AccruedCapital { get; set; }
        public double TotalCapital { get; set; }
        
        // Dodatkowe informacje o limitach
        public double MessagesPercentage { get; set; }
        public double MessagesLimit { get; set; }
        public double CapitalPercentage { get; set; }
        public double CapitalLimit { get; set; }
    }
}