using System.Collections.ObjectModel;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;
using System.Windows.Media;

namespace RC_GUI_WATS.ViewModels
{
    public class MessagesTabViewModel : BaseViewModel
    {
        private readonly RcTcpClientService _clientService;
        private readonly PositionsService _positionsService;
        private readonly CapitalService _capitalService;
        
        // Properties for binding
        public ObservableCollection<Position> Positions => _positionsService.Positions;
        public Capital CurrentCapital => _capitalService.CurrentCapital;
        
        // UI properties
        private string _openCapitalText;
        public string OpenCapitalText
        {
            get => _openCapitalText;
            set => SetProperty(ref _openCapitalText, value);
        }
        
        private string _accruedCapitalText;
        public string AccruedCapitalText
        {
            get => _accruedCapitalText;
            set => SetProperty(ref _accruedCapitalText, value);
        }
        
        private string _totalCapitalText;
        public string TotalCapitalText
        {
            get => _totalCapitalText;
            set => SetProperty(ref _totalCapitalText, value);
        }
        
        private string _messagesPercentageText;
        public string MessagesPercentageText
        {
            get => _messagesPercentageText;
            set => SetProperty(ref _messagesPercentageText, value);
        }
        
        private Brush _messagesPercentageBrush;
        public Brush MessagesPercentageBrush
        {
            get => _messagesPercentageBrush;
            set => SetProperty(ref _messagesPercentageBrush, value);
        }
        
        // Similar properties for other UI elements
        
        public MessagesTabViewModel(
            RcTcpClientService clientService,
            PositionsService positionsService,
            CapitalService capitalService)
        {
            _clientService = clientService;
            _positionsService = positionsService;
            _capitalService = capitalService;
            
            // Subscribe to capital updates
            _capitalService.CapitalUpdated += UpdateCapitalDisplay;
            
            // Initialize display
            UpdateCapitalDisplay();
        }
        
        public void UpdateCapitalDisplay()
        {
            OpenCapitalText = CurrentCapital.OpenCapital.ToString("0.00");
            AccruedCapitalText = CurrentCapital.AccruedCapital.ToString("0.00");
            TotalCapitalText = CurrentCapital.TotalCapital.ToString("0.00");
            
            MessagesPercentageText = $"{CurrentCapital.MessagesPercentage}%";
            // Update other UI properties
            
            MessagesPercentageBrush = GetBrushForPercentage(CurrentCapital.MessagesPercentage);
            // Update other brushes
        }
        
        private Brush GetBrushForPercentage(double percentage)
        {
            if (percentage < 50)
                return Brushes.Green;
            else if (percentage < 80)
                return Brushes.Orange;
            else
                return Brushes.Red;
        }
        
        public async void AllSwitchButtonClick()
        {
            if (_clientService.IsConnected)
            {
                await _clientService.SendSetControlAsync("(ALL),halt,Y");
            }
        }
    }
}