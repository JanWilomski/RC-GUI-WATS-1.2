// Models/Capital.cs

using System.ComponentModel;

namespace RC_GUI_WATS.Models
{
    public class Capital : INotifyPropertyChanged
    {
        public double OpenCapital { get; set; }
        public double AccruedCapital { get; set; }
        public double TotalCapital { get; set; }
        
        // Dodatkowe informacje o limitach
        public double MessagesPercentage { get; set; }
        public double MessagesLimit { get; set; }
        private double _capitalPercentage;
        public double CapitalLimit { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;
        

        public double CapitalPercentage
        {
            get => _capitalPercentage;
            set
            {
                if (Math.Abs(_capitalPercentage - value) > 0.000001)
                {
                    _capitalPercentage = value;
                    OnPropertyChanged(nameof(CapitalPercentage));
                }
            }
        }
        
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}