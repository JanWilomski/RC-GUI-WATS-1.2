// Models/LimitTypeInfo.cs
namespace RC_GUI_WATS.Models
{
    /// <summary>
    /// Klasa pomocnicza do przechowywania informacji o typach limitów
    /// </summary>
    public class LimitTypeInfo
    {
        public string Name { get; set; }
        public string ValueFormat { get; set; }
        public string Description { get; set; }
        
        public LimitTypeInfo(string name, string valueFormat, string description)
        {
            Name = name;
            ValueFormat = valueFormat;
            Description = description;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}