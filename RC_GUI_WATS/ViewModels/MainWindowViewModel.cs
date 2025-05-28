// ViewModels/MainWindowViewModel.cs
using System;
using System.Threading.Tasks;
using System.Windows;
using RC_GUI_WATS.Services;
using RC_GUI_WATS.Commands;

namespace RC_GUI_WATS.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly RcTcpClientService _clientService;
        private readonly ConfigurationService _configService;
        private readonly HeartbeatMonitorService _heartbeatMonitorService;
        private readonly FileLoggingService _fileLoggingService;
        
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
        
        private string _statusBarText;
        public string StatusBarText
        {
            get => _statusBarText;
            set => SetProperty(ref _statusBarText, value);
        }
        
        // Loading indicator for historical data
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
            CcgMessagesService ccgMessagesService,
            FileLoggingService fileLoggingService) // Add FileLoggingService
        {
            _clientService = clientService;
            _configService = configService;
            _heartbeatMonitorService = heartbeatMonitorService;
            _fileLoggingService = fileLoggingService;
            
            // Załaduj wartości z konfiguracji
            _serverIp = _configService.ServerIP;
            _serverPort = _configService.ServerPort.ToString();
            
            // Initialize tab ViewModels - pass FileLoggingService to SettingsTab
            MessagesTab = new MessagesTabViewModel(clientService, positionsService, capitalService, heartbeatMonitorService, ccgMessagesService);
            SettingsTab = new SettingsTabViewModel(clientService, limitsService, configService, fileLoggingService, _serverIp, _serverPort);
            FiltersTab = new FiltersTabViewModel();
            InstrumentsTab = new InstrumentsTabViewModel(instrumentsService, _configService.InstrumentsFilePath);
            
            // Subscribe to client service events
            _clientService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Initialize commands
            ConnectCommand = new RelayCommand(async () => await ConnectToServerAsync(), () => !IsConnected);
            DisconnectCommand = new RelayCommand(() => DisconnectFromServer(), () => IsConnected);
            RewindCommand = new RelayCommand(async () => await RewindMessagesAsync(), () => IsConnected);
            
            // Initialize UI
            ConnectionStatus = "Rozłączony";
            StatusBarText = "Gotowy";
            IsLoadingHistoricalData = false;
            
            // Log application startup
            _fileLoggingService.LogSettings("Application started", $"Target server: {_serverIp}:{_serverPort}");
            
            // Auto-connect if configured
            if (_configService.AutoConnect)
            {
                _fileLoggingService.LogConnection("Auto-connect enabled", "Starting automatic connection");
                Task.Run(async () => await ConnectToServerAsync());
            }
        }
        
        private void OnConnectionStatusChanged(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? "Połączony" : "Rozłączony";
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            RewindCommand.RaiseCanExecuteChanged();
            
            if (isConnected)
            {
                StatusBarText = $"Połączony z {_serverIp}:{_serverPort}";
                _fileLoggingService.LogConnection("Connected", $"Server: {_serverIp}:{_serverPort}");
            }
            else
            {
                StatusBarText = "Rozłączony";
                IsLoadingHistoricalData = false;
                _fileLoggingService.LogConnection("Disconnected");
            }
        }
        
        public async Task ConnectToServerAsync()
        {
            try
            {
                StatusBarText = $"Łączenie z {_serverIp}:{_serverPort}...";
                _fileLoggingService.LogConnection("Connecting", $"Server: {_serverIp}:{_serverPort}");
                
                await _clientService.ConnectAsync(_serverIp, int.Parse(_serverPort));
                
                StatusBarText = "Pobieranie historycznych danych...";
                IsLoadingHistoricalData = true;
                _fileLoggingService.LogConnection("Connected", "Starting historical data retrieval");
                
                // Rewind messages - get all historical data
                await _clientService.SendRewindAsync(0);
                
                // Load control history
                await SettingsTab.LoadControlHistoryAsync();
                
                // Wait a bit for data to arrive before removing loading indicator
                await Task.Delay(2000);
                IsLoadingHistoricalData = false;
                
                _fileLoggingService.LogConnection("Historical data loaded", "Ready for operation");
            }
            catch (Exception ex)
            {
                StatusBarText = $"Błąd połączenia: {ex.Message}";
                IsLoadingHistoricalData = false;
                MessageBox.Show($"Błąd podczas łączenia: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _fileLoggingService.LogError($"Connection failed to {_serverIp}:{_serverPort}", ex);
            }
        }
        
        private void DisconnectFromServer()
        {
            try
            {
                _fileLoggingService.LogConnection("Disconnecting", "User initiated disconnect");
                _clientService.Disconnect();
                StatusBarText = "Rozłączony przez użytkownika";
            }
            catch (Exception ex)
            {
                _fileLoggingService.LogError("Error during disconnect", ex);
            }
        }
        
        public async Task RewindMessagesAsync()
        {
            try
            {
                StatusBarText = "Pobieranie wszystkich wiadomości CCG...";
                IsLoadingHistoricalData = true;
                _fileLoggingService.LogConnection("Rewind requested", "Fetching all CCG messages");
                
                await _clientService.SendRewindAsync(0);
                
                // Wait for data to arrive
                await Task.Delay(3000);
                IsLoadingHistoricalData = false;
                StatusBarText = "Pobrano wszystkie wiadomości CCG";
                
                _fileLoggingService.LogConnection("Rewind completed", "All CCG messages retrieved");
            }
            catch (Exception ex)
            {
                StatusBarText = $"Błąd podczas pobierania wiadomości: {ex.Message}";
                IsLoadingHistoricalData = false;
                MessageBox.Show($"Błąd podczas rewinding: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _fileLoggingService.LogError("Rewind failed", ex);
            }
        }
    }
}