// Models/Capital.cs
using System.ComponentModel;

namespace RC_GUI_WATS.Models
{
    public class Capital : INotifyPropertyChanged
    {
        public double OpenCapital { get; set; }
        public double AccruedCapital { get; set; }
        public double TotalCapital { get; set; }
        
        // Informacje o limitach wiadomości
        private double _messagesPercentage;
        public double MessagesPercentage
        {
            get => _messagesPercentage;
            set
            {
                if (Math.Abs(_messagesPercentage - value) > 0.000001)
                {
                    _messagesPercentage = value;
                    OnPropertyChanged(nameof(MessagesPercentage));
                }
            }
        }
        
        private double _messagesLimit;
        public double MessagesLimit
        {
            get => _messagesLimit;
            set
            {
                if (Math.Abs(_messagesLimit - value) > 0.000001)
                {
                    _messagesLimit = value;
                    OnPropertyChanged(nameof(MessagesLimit));
                }
            }
        }
        
        private double _currentMessages;
        public double CurrentMessages
        {
            get => _currentMessages;
            set
            {
                if (Math.Abs(_currentMessages - value) > 0.000001)
                {
                    _currentMessages = value;
                    OnPropertyChanged(nameof(CurrentMessages));
                }
            }
        }
        
        // Informacje o limitach kapitału
        private double _capitalPercentage;
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
        
        private double _capitalLimit;
        public double CapitalLimit
        {
            get => _capitalLimit;
            set
            {
                if (Math.Abs(_capitalLimit - value) > 0.000001)
                {
                    _capitalLimit = value;
                    OnPropertyChanged(nameof(CapitalLimit));
                }
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}