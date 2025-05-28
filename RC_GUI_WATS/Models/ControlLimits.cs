// Models/ControlLimit.cs
using System;
using System.ComponentModel;

namespace RC_GUI_WATS.Models
{
    public class ControlLimit : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _scope;
        public string Scope 
        { 
            get => _scope;
            set
            {
                _scope = value;
                OnPropertyChanged(nameof(Scope));
                UpdateDisplayName();
            }
        }

        private string _name;
        public string Name 
        { 
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private string _value;
        public string Value 
        { 
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        private string _displayName;
        public string DisplayName 
        { 
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        // Dodajemy timestamp dla chronologicznego sortowania
        public DateTime ReceivedTime { get; set; }
        
        // Konstruktor domyślny
        public ControlLimit()
        {
            ReceivedTime = DateTime.Now;
        }
        
        // Konstruktor z parametrami
        public ControlLimit(string scope, string name, string value)
        {
            Scope = scope;
            Name = name;
            Value = value;
            ReceivedTime = DateTime.Now;
            UpdateDisplayName();
        }
        
        private void UpdateDisplayName()
        {
            // Ustaw DisplayName na podstawie scope
            if (Scope == "(ALL)")
                DisplayName = "Wszystkie instrumenty";
            else if (!string.IsNullOrEmpty(Scope) && Scope.StartsWith("[") && Scope.EndsWith("]"))
                DisplayName = $"Grupa {Scope}";
            else
                DisplayName = Scope; // ISIN
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}