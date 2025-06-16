// ViewModels/MessagesTabViewModel.cs - Updated version with advanced CCG message filtering
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using RC_GUI_WATS.Commands;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;

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

        // Collection view for filtering
        private ICollectionView _ccgMessagesView;
        public ICollectionView CcgMessagesView => _ccgMessagesView;

        // Properties for binding
        public ObservableCollection<Position> Positions => _positionsService.Positions;
        public Capital CurrentCapital => _capitalService.CurrentCapital;
        public ObservableCollection<CcgMessage> CcgMessages => _ccgMessagesService.CcgMessages;
        public ObservableCollection<OrderBookEntry> OrderBook => _orderBookService.Orders;

        // Heartbeat indicator
        public HeartbeatIndicatorViewModel HeartbeatIndicator { get; }

        #region Message Type Filters (Checkboxes)

        private bool _showOrderAdd = true;
        public bool ShowOrderAdd
        {
            get => _showOrderAdd;
            set { if (SetProperty(ref _showOrderAdd, value)) ApplyMessageFilters(); }
        }

        private bool _showOrderAddResponse = true;
        public bool ShowOrderAddResponse
        {
            get => _showOrderAddResponse;
            set { if (SetProperty(ref _showOrderAddResponse, value)) ApplyMessageFilters(); }
        }

        private bool _showOrderModify = true;
        public bool ShowOrderModify
        {
            get => _showOrderModify;
            set { if (SetProperty(ref _showOrderModify, value)) ApplyMessageFilters(); }
        }

        private bool _showOrderModifyResponse = true;
        public bool ShowOrderModifyResponse
        {
            get => _showOrderModifyResponse;
            set { if (SetProperty(ref _showOrderModifyResponse, value)) ApplyMessageFilters(); }
        }

        private bool _showOrderCancel = true;
        public bool ShowOrderCancel
        {
            get => _showOrderCancel;
            set { if (SetProperty(ref _showOrderCancel, value)) ApplyMessageFilters(); }
        }

        private bool _showOrderCancelResponse = true;
        public bool ShowOrderCancelResponse
        {
            get => _showOrderCancelResponse;
            set { if (SetProperty(ref _showOrderCancelResponse, value)) ApplyMessageFilters(); }
        }

        private bool _showTrade = true;
        public bool ShowTrade
        {
            get => _showTrade;
            set { if (SetProperty(ref _showTrade, value)) ApplyMessageFilters(); }
        }

        private bool _showMassQuote = true;
        public bool ShowMassQuote
        {
            get => _showMassQuote;
            set { if (SetProperty(ref _showMassQuote, value)) ApplyMessageFilters(); }
        }

        private bool _showMassQuoteResponse = true;
        public bool ShowMassQuoteResponse
        {
            get => _showMassQuoteResponse;
            set { if (SetProperty(ref _showMassQuoteResponse, value)) ApplyMessageFilters(); }
        }

        private bool _showOrderMassCancel = true;
        public bool ShowOrderMassCancel
        {
            get => _showOrderMassCancel;
            set { if (SetProperty(ref _showOrderMassCancel, value)) ApplyMessageFilters(); }
        }

        private bool _showOrderMassCancelResponse = true;
        public bool ShowOrderMassCancelResponse
        {
            get => _showOrderMassCancelResponse;
            set { if (SetProperty(ref _showOrderMassCancelResponse, value)) ApplyMessageFilters(); }
        }

        private bool _showTradeCaptureReports = true;
        public bool ShowTradeCaptureReports
        {
            get => _showTradeCaptureReports;
            set { if (SetProperty(ref _showTradeCaptureReports, value)) ApplyMessageFilters(); }
        }

        private bool _showReject = true;
        public bool ShowReject
        {
            get => _showReject;
            set { if (SetProperty(ref _showReject, value)) ApplyMessageFilters(); }
        }

        private bool _showLoginLogout = false; // Default false as these are usually not interesting
        public bool ShowLoginLogout
        {
            get => _showLoginLogout;
            set { if (SetProperty(ref _showLoginLogout, value)) ApplyMessageFilters(); }
        }

        private bool _showHeartbeat = false; // Default false as these are very frequent
        public bool ShowHeartbeat
        {
            get => _showHeartbeat;
            set { if (SetProperty(ref _showHeartbeat, value)) ApplyMessageFilters(); }
        }

        private bool _showOthers = true;
        public bool ShowOthers
        {
            get => _showOthers;
            set { if (SetProperty(ref _showOthers, value)) ApplyMessageFilters(); }
        }

        #endregion

        #region UI Properties for Capital and Statistics

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

        private string _currentMessagesText;
        public string CurrentMessagesText
        {
            get => _currentMessagesText;
            set => SetProperty(ref _currentMessagesText, value);
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

        // Filter properties for Order Book
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

        #endregion

        // Commands
        public RelayCommand AllSwitchCommand { get; }
        public RelayCommand ClearCcgMessagesCommand { get; }
        public RelayCommand ClearOrderBookCommand { get; }
        public RelayCommand ClearOrderFiltersCommand { get; }
        public RelayCommand SelectAllMessageTypesCommand { get; }
        public RelayCommand DeselectAllMessageTypesCommand { get; }
        public RelayCommand ResetMessageFiltersCommand { get; }

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

            // Initialize CollectionView for CCG Messages
            _ccgMessagesView = CollectionViewSource.GetDefaultView(_ccgMessagesService.CcgMessages);
            _ccgMessagesView.Filter = CcgMessageFilter;

            // Subscribe to capital updates
            _capitalService.CapitalUpdated += UpdateCapitalDisplay;

            // Subscribe to CCG message updates
            _ccgMessagesService.NewCcgMessageReceived += OnNewCcgMessage;
            _ccgMessagesService.MessagesCleared += OnCcgMessagesCleared;

            // Subscribe to Order Book updates
            _orderBookService.OrderUpdated += OnOrderUpdated;
            _orderBookService.OrderBookCleared += OnOrderBookCleared;

            // Subscribe to connection status to reset counters
            _clientService.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Initialize commands
            AllSwitchCommand = new RelayCommand(AllSwitchButtonClick);
            ClearCcgMessagesCommand = new RelayCommand(() => _ccgMessagesService.ClearMessages());
            ClearOrderBookCommand = new RelayCommand(() => _orderBookService.ClearOrderBook());
            ClearOrderFiltersCommand = new RelayCommand(ClearOrderFilters);
            SelectAllMessageTypesCommand = new RelayCommand(SelectAllMessageTypes);
            DeselectAllMessageTypesCommand = new RelayCommand(DeselectAllMessageTypes);
            ResetMessageFiltersCommand = new RelayCommand(ResetMessageFilters);

            // Initialize display
            UpdateCapitalDisplay();
            UpdateCcgStatistics();
            UpdateOrderBookStatistics();
        }

        #region Message Filtering

        private bool CcgMessageFilter(object item)
        {
            if (item is not CcgMessage message) return false;

            // Apply checkbox filters based on message type
            bool passesTypeFilter = message.Name switch
            {
                "OrderAdd" => ShowOrderAdd,
                "OrderAddResponse" => ShowOrderAddResponse,
                "OrderModify" => ShowOrderModify,
                "OrderModifyResponse" => ShowOrderModifyResponse,
                "OrderCancel" => ShowOrderCancel,
                "OrderCancelResponse" => ShowOrderCancelResponse,
                "Trade" => ShowTrade,
                "MassQuote" => ShowMassQuote,
                "MassQuoteResponse" => ShowMassQuoteResponse,
                "OrderMassCancel" => ShowOrderMassCancel,
                "OrderMassCancelResponse" => ShowOrderMassCancelResponse,
                "TradeCaptureReportSingle" or "TradeCaptureReportDual" or "TradeCaptureReportResponse" => ShowTradeCaptureReports,
                "Reject" => ShowReject,
                "Login" or "LoginResponse" or "Logout" or "LogoutResponse" => ShowLoginLogout,
                "Heartbeat" => ShowHeartbeat,
                _ => ShowOthers
            };

            return passesTypeFilter;
        }

        private void ApplyMessageFilters()
        {
            _ccgMessagesView?.Refresh();
            UpdateFilteredMessageCount();
        }

        private void UpdateFilteredMessageCount()
        {
            if (_ccgMessagesView != null)
            {
                var filteredCount = _ccgMessagesView.Cast<object>().Count();
                var totalCount = _ccgMessagesService.GetMessageCount();
                CcgMessageCountText = $"CCG Messages: {filteredCount}/{totalCount}";
            }
        }

        private void SelectAllMessageTypes()
        {
            ShowOrderAdd = true;
            ShowOrderAddResponse = true;
            ShowOrderModify = true;
            ShowOrderModifyResponse = true;
            ShowOrderCancel = true;
            ShowOrderCancelResponse = true;
            ShowTrade = true;
            ShowMassQuote = true;
            ShowMassQuoteResponse = true;
            ShowOrderMassCancel = true;
            ShowOrderMassCancelResponse = true;
            ShowTradeCaptureReports = true;
            ShowReject = true;
            ShowLoginLogout = true;
            ShowHeartbeat = true;
            ShowOthers = true;
        }

        private void DeselectAllMessageTypes()
        {
            ShowOrderAdd = false;
            ShowOrderAddResponse = false;
            ShowOrderModify = false;
            ShowOrderModifyResponse = false;
            ShowOrderCancel = false;
            ShowOrderCancelResponse = false;
            ShowTrade = false;
            ShowMassQuote = false;
            ShowMassQuoteResponse = false;
            ShowOrderMassCancel = false;
            ShowOrderMassCancelResponse = false;
            ShowTradeCaptureReports = false;
            ShowReject = false;
            ShowLoginLogout = false;
            ShowHeartbeat = false;
            ShowOthers = false;
        }

        private void ResetMessageFilters()
        {
            // Reset to trading-focused defaults
            ShowOrderAdd = true;
            ShowOrderAddResponse = true;
            ShowOrderModify = true;
            ShowOrderModifyResponse = true;
            ShowOrderCancel = true;
            ShowOrderCancelResponse = true;
            ShowTrade = true;
            ShowMassQuote = true;
            ShowMassQuoteResponse = true;
            ShowOrderMassCancel = true;
            ShowOrderMassCancelResponse = true;
            ShowTradeCaptureReports = true;
            ShowReject = true;
            ShowLoginLogout = false; // Usually not interesting
            ShowHeartbeat = false; // Very frequent, usually not needed
            ShowOthers = true;
        }

        #endregion

        #region Event Handlers and Updates

        private void OnConnectionStatusChanged(bool isConnected)
        {
            if (!isConnected)
            {
                // Reset counters when disconnected
                _capitalService.ResetCounters();
            }
        }

        public void UpdateCapitalDisplay()
        {
            OpenCapitalText = CurrentCapital.OpenCapital.ToString("0.00");
            AccruedCapitalText = CurrentCapital.AccruedCapital.ToString("0.00");
            TotalCapitalText = CurrentCapital.TotalCapital.ToString("0.00");

            // Updated messages display with current count and limit
            CurrentMessagesText = CurrentCapital.CurrentMessages.ToString("0");
            if (CurrentCapital.MessagesLimit > 0)
            {
                MessagesPercentageText = $"{CurrentCapital.MessagesPercentage:F1}%";
            }
            else
            {
                MessagesPercentageText = "N/A";
            }
            
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
            UpdateFilteredMessageCount(); // Use filtered count

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

        #endregion

        #region Filter Management

        private void ApplyOrderBookFilters()
        {
            // Similar filtering logic for Order Book
            // Could be implemented with CollectionViewSource
        }

        private void ClearOrderFilters()
        {
            OrderStatusFilter = "";
            OrderSideFilter = "";
            OrderInstrumentFilter = "";
        }

        #endregion

        public async void AllSwitchButtonClick()
        {
            if (_clientService.IsConnected)
            {
                await _clientService.SendSetControlAsync("(ALL),halt,Y");
            }
        }
    }
}