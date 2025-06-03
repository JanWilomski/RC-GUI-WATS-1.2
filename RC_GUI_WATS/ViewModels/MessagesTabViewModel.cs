// ViewModels/MessagesTabViewModel.cs - Updated version
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
        private readonly OrderBookService _orderBookService;

        // Properties for binding
        public ObservableCollection<Position> Positions => _positionsService.Positions;
        public Capital CurrentCapital => _capitalService.CurrentCapital;
        public ObservableCollection<CcgMessage> CcgMessages => _ccgMessagesService.CcgMessages;
        public ObservableCollection<OrderBookEntry> OrderBook => _orderBookService.Orders;

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

        private string _instrumentMappingText;
        public string InstrumentMappingText
        {
            get => _instrumentMappingText;
            set => SetProperty(ref _instrumentMappingText, value);
        }

        // Order Book properties
        private string _orderBookCountText;
        public string OrderBookCountText
        {
            get => _orderBookCountText;
            set => SetProperty(ref _orderBookCountText, value);
        }

        private string _orderBookStatisticsText;
        public string OrderBookStatisticsText
        {
            get => _orderBookStatisticsText;
            set => SetProperty(ref _orderBookStatisticsText, value);
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

        // Order Book filter properties
        private string _orderStatusFilter;
        public string OrderStatusFilter
        {
            get => _orderStatusFilter;
            set
            {
                if (SetProperty(ref _orderStatusFilter, value))
                {
                    ApplyOrderBookFilters();
                }
            }
        }

        private string _orderSideFilter;
        public string OrderSideFilter
        {
            get => _orderSideFilter;
            set
            {
                if (SetProperty(ref _orderSideFilter, value))
                {
                    ApplyOrderBookFilters();
                }
            }
        }

        private string _orderInstrumentFilter;
        public string OrderInstrumentFilter
        {
            get => _orderInstrumentFilter;
            set
            {
                if (SetProperty(ref _orderInstrumentFilter, value))
                {
                    ApplyOrderBookFilters();
                }
            }
        }

        // Commands
        public RelayCommand AllSwitchCommand { get; }
        public RelayCommand ClearCcgMessagesCommand { get; }
        public RelayCommand ClearFiltersCommand { get; }
        public RelayCommand ClearOrderBookCommand { get; }
        public RelayCommand ClearOrderFiltersCommand { get; }

        public MessagesTabViewModel(
            RcTcpClientService clientService,
            PositionsService positionsService,
            CapitalService capitalService,
            HeartbeatMonitorService heartbeatMonitor,
            CcgMessagesService ccgMessagesService,
            OrderBookService orderBookService)
        {
            _clientService = clientService;
            _positionsService = positionsService;
            _capitalService = capitalService;
            _heartbeatMonitor = heartbeatMonitor;
            _ccgMessagesService = ccgMessagesService;
            _orderBookService = orderBookService;

            // Create heartbeat indicator view model
            HeartbeatIndicator = new HeartbeatIndicatorViewModel(_heartbeatMonitor);

            // Subscribe to capital updates
            _capitalService.CapitalUpdated += UpdateCapitalDisplay;

            // Subscribe to CCG message updates
            _ccgMessagesService.NewCcgMessageReceived += OnNewCcgMessage;
            _ccgMessagesService.MessagesCleared += OnCcgMessagesCleared;

            // Subscribe to Order Book updates
            _orderBookService.OrderUpdated += OnOrderUpdated;
            _orderBookService.OrderBookCleared += OnOrderBookCleared;

            // Initialize commands
            AllSwitchCommand = new RelayCommand(AllSwitchButtonClick);
            ClearCcgMessagesCommand = new RelayCommand(() => _ccgMessagesService.ClearMessages());
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ClearOrderBookCommand = new RelayCommand(() => _orderBookService.ClearOrderBook());
            ClearOrderFiltersCommand = new RelayCommand(ClearOrderFilters);

            // Initialize display
            UpdateCapitalDisplay();
            UpdateCcgStatistics();
            UpdateOrderBookStatistics();
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

        private void OnOrderUpdated(OrderBookEntry order)
        {
            UpdateOrderBookStatistics();
        }

        private void OnOrderBookCleared()
        {
            UpdateOrderBookStatistics();
        }

        private void UpdateCcgStatistics()
        {
            var count = _ccgMessagesService.GetMessageCount();
            CcgMessageCountText = $"CCG Messages: {count}";

            var (orders, trades, cancels, quotes, others) = _ccgMessagesService.GetMessageStatistics();
            CcgStatisticsText = $"Orders: {orders}, Trades: {trades}, Cancels: {cancels}, Quotes: {quotes}, Others: {others}";

            // Update instrument mapping statistics
            var (withMapping, withoutMapping, total) = _ccgMessagesService.GetInstrumentMappingStatistics();

            if (total > 0)
            {
                double mappingPercentage = (double)withMapping / total * 100;
                InstrumentMappingText = $"Instrument Mapping: {withMapping}/{total} ({mappingPercentage:F1}%)";

                if (withoutMapping > 0)
                {
                    InstrumentMappingText += $" | {withoutMapping} unmapped";
                }
            }
            else
            {
                InstrumentMappingText = "Instrument Mapping: N/A (no messages with InstrumentID)";
            }
        }

        private void UpdateOrderBookStatistics()
        {
            var count = _orderBookService.Orders.Count;
            OrderBookCountText = $"Order Book Entries: {count}";

            var (total, active, filled, cancelled) = _orderBookService.GetOrderStatistics();
            OrderBookStatisticsText = $"Total: {total}, Active: {active}, Filled: {filled}, Cancelled: {cancelled}";
        }

        private void ApplyFilters()
        {
            // In a more complex implementation, we could filter the ObservableCollection
            // For now, we rely on the DataGrid's built-in filtering or implement custom filtering
            // This is a placeholder for filter logic that could be implemented with CollectionView
        }

        private void ApplyOrderBookFilters()
        {
            // Similar filtering logic for Order Book
            // Could be implemented with CollectionViewSource
        }

        private void ClearFilters()
        {
            MessageTypeFilter = "";
            SideFilter = "";
            InstrumentIdFilter = "";
        }

        private void ClearOrderFilters()
        {
            OrderStatusFilter = "";
            OrderSideFilter = "";
            OrderInstrumentFilter = "";
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