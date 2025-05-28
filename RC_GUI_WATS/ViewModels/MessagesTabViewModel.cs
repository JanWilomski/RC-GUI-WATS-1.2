// ViewModels/MessagesTabViewModel.cs
using System.Collections.ObjectModel;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;
using RC_GUI_WATS.Commands;
using System.Windows.Media;
using System;

namespace RC_GUI_WATS.ViewModels
{
    public class MessagesTabViewModel : BaseViewModel
    {
        private readonly RcTcpClientService _clientService;
        private readonly PositionsService _positionsService;
        private readonly CapitalService _capitalService;
        private readonly HeartbeatMonitorService _heartbeatMonitor;
        private readonly CcgMessagesService _ccgMessagesService;
        
        // Properties for binding
        public ObservableCollection<Position> Positions => _positionsService.Positions;
        public Capital CurrentCapital => _capitalService.CurrentCapital;
        public ObservableCollection<CcgMessage> CcgMessages => _ccgMessagesService.CcgMessages;
        
        // Heartbeat indicator
        public HeartbeatIndicatorViewModel HeartbeatIndicator { get; }
        
        // UI properties for capital
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
        
        private Brush _capitalPercentageBrush;
        public Brush CapitalPercentageBrush
        {
            get => _capitalPercentageBrush;
            set => SetProperty(ref _capitalPercentageBrush, value);
        }

        // CCG Messages properties
        private string _ccgMessageCountText;
        public string CcgMessageCountText
        {
            get => _ccgMessageCountText;
            set => SetProperty(ref _ccgMessageCountText, value);
        }

        private string _ccgStatisticsText;
        public string CcgStatisticsText
        {
            get => _ccgStatisticsText;
            set => SetProperty(ref _ccgStatisticsText, value);
        }

        // Filter properties for CCG messages
        private string _messageTypeFilter;
        public string MessageTypeFilter
        {
            get => _messageTypeFilter;
            set
            {
                if (SetProperty(ref _messageTypeFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _sideFilter;
        public string SideFilter
        {
            get => _sideFilter;
            set
            {
                if (SetProperty(ref _sideFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _instrumentIdFilter;
        public string InstrumentIdFilter
        {
            get => _instrumentIdFilter;
            set
            {
                if (SetProperty(ref _instrumentIdFilter, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Commands
        public RelayCommand AllSwitchCommand { get; }
        public RelayCommand ClearCcgMessagesCommand { get; }
        public RelayCommand ClearFiltersCommand { get; }
        
        public MessagesTabViewModel(
            RcTcpClientService clientService,
            PositionsService positionsService,
            CapitalService capitalService,
            HeartbeatMonitorService heartbeatMonitor,
            CcgMessagesService ccgMessagesService)
        {
            _clientService = clientService;
            _positionsService = positionsService;
            _capitalService = capitalService;
            _heartbeatMonitor = heartbeatMonitor;
            _ccgMessagesService = ccgMessagesService;
            
            // Create heartbeat indicator view model
            HeartbeatIndicator = new HeartbeatIndicatorViewModel(_heartbeatMonitor);
            
            // Subscribe to capital updates
            _capitalService.CapitalUpdated += UpdateCapitalDisplay;
            
            // Subscribe to CCG message updates
            _ccgMessagesService.NewCcgMessageReceived += OnNewCcgMessage;
            _ccgMessagesService.MessagesCleared += OnCcgMessagesCleared;
            
            // Initialize commands
            AllSwitchCommand = new RelayCommand(AllSwitchButtonClick);
            ClearCcgMessagesCommand = new RelayCommand(() => _ccgMessagesService.ClearMessages());
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            
            // Initialize display
            UpdateCapitalDisplay();
            UpdateCcgStatistics();
        }
        
        public void UpdateCapitalDisplay()
        {
            OpenCapitalText = CurrentCapital.OpenCapital.ToString("0.00");
            AccruedCapitalText = CurrentCapital.AccruedCapital.ToString("0.00");
            TotalCapitalText = CurrentCapital.TotalCapital.ToString("0.00");
            
            MessagesPercentageText = $"{CurrentCapital.MessagesPercentage}%";
            MessagesPercentageBrush = GetBrushForPercentage(CurrentCapital.MessagesPercentage);
            
            CapitalPercentageBrush = GetBrushForPercentage(CurrentCapital.CapitalPercentage);
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

        private void OnNewCcgMessage(CcgMessage message)
        {
            UpdateCcgStatistics();
        }

        private void OnCcgMessagesCleared()
        {
            UpdateCcgStatistics();
        }

        private void UpdateCcgStatistics()
        {
            var count = _ccgMessagesService.GetMessageCount();
            CcgMessageCountText = $"CCG Messages: {count}";

            var (orders, trades, cancels, quotes, others) = _ccgMessagesService.GetMessageStatistics();
            CcgStatisticsText = $"Orders: {orders}, Trades: {trades}, Cancels: {cancels}, Quotes: {quotes}, Others: {others}";
        }

        private void ApplyFilters()
        {
            // In a more complex implementation, we could filter the ObservableCollection
            // For now, we rely on the DataGrid's built-in filtering or implement custom filtering
            // This is a placeholder for filter logic that could be implemented with CollectionView
        }

        private void ClearFilters()
        {
            MessageTypeFilter = "";
            SideFilter = "";
            InstrumentIdFilter = "";
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