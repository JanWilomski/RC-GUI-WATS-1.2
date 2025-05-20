// Models/ControlLimit.cs
namespace RC_GUI_WATS.Models
{
    public class ControlLimit
    {
        public string Scope { get; set; } // (ALL), ISIN lub [wzorzec]
        public string Name { get; set; }  // Nazwa kontroli (np. halt, maxOrderRate, maxAbsShares, maxShortShares)
        public string Value { get; set; } // Wartość kontroli
        public string DisplayName { get; set; } // Opcjonalne przyjazne wyświetlanie
        
        // Konstruktor domyślny
        public ControlLimit()
        {
        }
        
        // Konstruktor z parametrami
        public ControlLimit(string scope, string name, string value)
        {
            Scope = scope;
            Name = name;
            Value = value;
            
            // Ustaw DisplayName na podstawie scope
            if (scope == "(ALL)")
                DisplayName = "Wszystkie instrumenty";
            else if (scope.StartsWith("[") && scope.EndsWith("]"))
                DisplayName = $"Grupa {scope}";
            else
                DisplayName = scope; // ISIN
        }
        
        // Formatowanie do kontroli serwera
        public string ToControlString()
        {
            return $"{Scope},{Name},{Value}";
        }
        
        // Parsowanie z ciągu kontroli serwera
        public static ControlLimit FromControlString(string controlString)
        {
            string[] parts = controlString.Split(',');
            if (parts.Length >= 3)
            {
                return new ControlLimit(parts[0], parts[1], parts[2]);
            }
            return null;
        }
    }
}