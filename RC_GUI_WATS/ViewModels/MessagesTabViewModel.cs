// ViewModels/MessagesTabViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;
using RC_GUI_WATS.Commands;
using System.Windows.Media;

namespace RC_GUI_WATS.ViewModels
{
    public class MessagesTabViewModel : BaseViewModel
    {
        private readonly RcTcpClientService _clientService;
        private readonly PositionsService _positionsService;
        private readonly CapitalService _capitalService;
        private readonly HeartbeatMonitorService _heartbeatMonitor;
        private readonly CcgMessagesService _ccgMessagesService;
        private readonly Dispatcher _dispatcher;
        
        // Properties for binding
        public ObservableCollection<Position> Positions => _positionsService.Positions;
        public Capital CurrentCapital => _capitalService.CurrentCapital;
        public ObservableCollection<CcgMessage> CcgMessages => _ccgMessagesService.CcgMessages;
        
        // Heartbeat indicator
        public HeartbeatIndicatorViewModel HeartbeatIndicator { get; }
        
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
        
        private Brush _capitalPercentageBrush;
        public Brush CapitalPercentageBrush
        {
            get => _capitalPercentageBrush;
            set => SetProperty(ref _capitalPercentageBrush, value);
        }
        
        // Commands
        public RelayCommand AllSwitchCommand { get; }
        public RelayCommand ClearCcgMessagesCommand { get; }
        public RelayCommand RewindCommand { get; }
        
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
            _dispatcher = Application.Current.Dispatcher;
            
            // Create heartbeat indicator view model
            HeartbeatIndicator = new HeartbeatIndicatorViewModel(_heartbeatMonitor);
            
            // Subscribe to capital updates
            _capitalService.CapitalUpdated += UpdateCapitalDisplay;
            
            // Subscribe to CCG messages service events
            _ccgMessagesService.HistoricalMessagesLoaded += OnHistoricalMessagesLoaded;
            _ccgMessagesService.RewindCompleted += OnRewindCompleted;
            
            // Initialize commands
            AllSwitchCommand = new RelayCommand(AllSwitchButtonClick);
            ClearCcgMessagesCommand = new RelayCommand(ClearCcgMessages);
            RewindCommand = new RelayCommand(async () => await PerformRewindAsync(), () => _clientService.IsConnected);
            
            // Initialize display
            UpdateCapitalDisplay();
        }
        
        public void UpdateCapitalDisplay()
        {
            // Ensure UI updates happen on UI thread
            _dispatcher.Invoke(() =>
            {
                OpenCapitalText = CurrentCapital.OpenCapital.ToString("0.00");
                AccruedCapitalText = CurrentCapital.AccruedCapital.ToString("0.00");
                TotalCapitalText = CurrentCapital.TotalCapital.ToString("0.00");
                
                MessagesPercentageText = $"{CurrentCapital.MessagesPercentage}%";
                MessagesPercentageBrush = GetBrushForPercentage(CurrentCapital.MessagesPercentage);
                
                CapitalPercentageBrush = GetBrushForPercentage(CurrentCapital.CapitalPercentage);
            });
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
                try
                {
                    await _clientService.SendSetControlAsync("(ALL),halt,Y");
                }
                catch (Exception ex)
                {
                    _dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Błąd podczas wysyłania komendy: {ex.Message}", "Błąd", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
        }
        
        private void ClearCcgMessages()
        {
            _ccgMessagesService.Clear();
        }
        
        private async Task PerformRewindAsync()
        {
            if (!_clientService.IsConnected)
            {
                MessageBox.Show("Nie jesteś połączony z serwerem!", "Błąd", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                // Clear existing messages
                _ccgMessagesService.Clear();
                
                // Start rewind process
                _ccgMessagesService.StartRewind();
                
                // Send rewind request to get all historical messages
                await _clientService.SendRewindAsync(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas rewind: {ex.Message}", "Błąd", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void OnHistoricalMessagesLoaded(int messageCount)
        {
            _dispatcher.Invoke(() =>
            {
                // Force UI refresh
                OnPropertyChanged(nameof(CcgMessages));
            });
        }
        
        private void OnRewindCompleted()
        {
            _dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(CcgMessages));
            });
        }
    }
}