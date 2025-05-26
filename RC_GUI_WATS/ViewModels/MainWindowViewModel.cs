// ViewModels/MainWindowViewModel.cs
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using RC_GUI_WATS.Services;
using RC_GUI_WATS.Commands;

namespace RC_GUI_WATS.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly RcTcpClientService _clientService;
        private readonly ConfigurationService _configService;
        private readonly HeartbeatMonitorService _heartbeatMonitorService;
        private readonly CcgMessagesService _ccgMessagesService;
        private readonly Dispatcher _dispatcher;
        
        // ServerIP i ServerPort z ConfigurationService
        private string _serverIp;
        public string ServerIp
        {
            get => _serverIp;
            set => SetProperty(ref _serverIp, value);
        }
        
        private string _serverPort;
        public string ServerPort
        {
            get => _serverPort;
            set => SetProperty(ref _serverPort, value);
        }
        
        // ViewModels for tabs
        public MessagesTabViewModel MessagesTab { get; }
        public SettingsTabViewModel SettingsTab { get; }
        public FiltersTabViewModel FiltersTab { get; }
        public InstrumentsTabViewModel InstrumentsTab { get; }
        
        // Connection properties
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }
        
        private string _connectionStatus;
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }
        
        private System.Windows.Media.Brush _connectionStatusBrush;
        public System.Windows.Media.Brush ConnectionStatusBrush
        {
            get => _connectionStatusBrush;
            set => SetProperty(ref _connectionStatusBrush, value);
        }
        
        private string _statusBarText;
        public string StatusBarText
        {
            get => _statusBarText;
            set => SetProperty(ref _statusBarText, value);
        }
        
        // Rewind status
        private bool _isLoadingHistoricalData;
        public bool IsLoadingHistoricalData
        {
            get => _isLoadingHistoricalData;
            set => SetProperty(ref _isLoadingHistoricalData, value);
        }
        
        // Command properties for buttons
        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        public RelayCommand RewindCommand { get; }
        
        public MainWindowViewModel(
            RcTcpClientService clientService,
            PositionsService positionsService,
            CapitalService capitalService,
            LimitsService limitsService,
            InstrumentsService instrumentsService,
            ConfigurationService configService,
            HeartbeatMonitorService heartbeatMonitorService,
            CcgMessagesService ccgMessagesService)
        {
            _clientService = clientService;
            _configService = configService;
            _heartbeatMonitorService = heartbeatMonitorService;
            _ccgMessagesService = ccgMessagesService;
            _dispatcher = Application.Current.Dispatcher;
            
            // Załaduj wartości z konfiguracji
            _serverIp = _configService.ServerIP;
            _serverPort = _configService.ServerPort.ToString();
            
            // Initialize tab ViewModels - pass all services including ccgMessagesService
            MessagesTab = new MessagesTabViewModel(clientService, positionsService, capitalService, heartbeatMonitorService, ccgMessagesService);
            SettingsTab = new SettingsTabViewModel(clientService, limitsService, configService, _serverIp, _serverPort);
            FiltersTab = new FiltersTabViewModel();
            InstrumentsTab = new InstrumentsTabViewModel(instrumentsService, _configService.InstrumentsFilePath);
            
            // Subscribe to client service events
            _clientService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Initialize commands
            ConnectCommand = new RelayCommand(async () => await ConnectToServerAsync(), () => !IsConnected);
            DisconnectCommand = new RelayCommand(() => _clientService.Disconnect(), () => IsConnected);
            RewindCommand = new RelayCommand(async () => await PerformRewindAsync(), () => IsConnected && !IsLoadingHistoricalData);
            
            // Initialize UI
            ConnectionStatus = "Rozłączony";
            ConnectionStatusBrush = System.Windows.Media.Brushes.Red;
            StatusBarText = "Gotowy";
            
            // Auto-connect if configured
            if (_configService.AutoConnect)
            {
                // Use dispatcher for auto-connect to avoid threading issues
                _dispatcher.BeginInvoke(new Action(async () => await ConnectToServerAsync()));
            }
        }
        
        private void OnConnectionStatusChanged(bool isConnected)
        {
            // Ensure UI updates happen on UI thread
            _dispatcher.Invoke(() =>
            {
                IsConnected = isConnected;
                ConnectionStatus = isConnected ? "Połączony" : "Rozłączony";
                ConnectionStatusBrush = isConnected ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
                
                ConnectCommand.RaiseCanExecuteChanged();
                DisconnectCommand.RaiseCanExecuteChanged();
                RewindCommand.RaiseCanExecuteChanged();
                
                if (isConnected)
                {
                    StatusBarText = $"Połączony z {_serverIp}:{_serverPort}";
                }
                else
                {
                    StatusBarText = "Rozłączony";
                    IsLoadingHistoricalData = false;
                }
            });
        }
        
        public async Task ConnectToServerAsync()
        {
            try
            {
                // Update UI on UI thread
                _dispatcher.Invoke(() =>
                {
                    StatusBarText = $"Łączenie z {_serverIp}:{_serverPort}...";
                });
                
                await _clientService.ConnectAsync(_serverIp, int.Parse(_serverPort));
                
                // Automatically perform rewind on successful connection
                await PerformRewindAsync();
                
                // Load control history
                _dispatcher.Invoke(() =>
                {
                    StatusBarText = "Ładowanie historii kontroli...";
                });
                
                await SettingsTab.LoadControlHistoryAsync();
                
                _dispatcher.Invoke(() =>
                {
                    StatusBarText = $"Gotowy - połączony z {_serverIp}:{_serverPort}";
                });
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    StatusBarText = $"Błąd połączenia: {ex.Message}";
                    IsLoadingHistoricalData = false;
                });
                
                MessageBox.Show($"Błąd podczas łączenia: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public async Task PerformRewindAsync()
        {
            if (!IsConnected)
            {
                MessageBox.Show("Nie jesteś połączony z serwerem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                _dispatcher.Invoke(() =>
                {
                    IsLoadingHistoricalData = true;
                    StatusBarText = "Pobieranie historycznych danych CCG...";
                });
                
                // Clear existing CCG messages before loading historical data
                _ccgMessagesService.Clear();
                
                // Start rewind process
                _ccgMessagesService.StartRewind();
                
                // Send rewind request to get all historical messages
                // According to documentation: "To recover all messages, use 0"
                await _clientService.SendRewindAsync(0);
                
                // Wait a moment for the rewind to complete
                // The server will send a 'r' (rewind complete) message when done
                await Task.Delay(2000);
                
                _dispatcher.Invoke(() =>
                {
                    StatusBarText = $"Załadowano {_ccgMessagesService.CcgMessages.Count} historycznych wiadomości CCG";
                });
                
                // The MessagesTabViewModel will handle its own UI updates through event subscriptions
            }
            catch (Exception ex)
            {
                _dispatcher.Invoke(() =>
                {
                    StatusBarText = $"Błąd podczas pobierania danych historycznych: {ex.Message}";
                });
                
                MessageBox.Show($"Błąd podczas rewind: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _dispatcher.Invoke(() =>
                {
                    IsLoadingHistoricalData = false;
                    RewindCommand.RaiseCanExecuteChanged();
                });
            }
        }
    }
}