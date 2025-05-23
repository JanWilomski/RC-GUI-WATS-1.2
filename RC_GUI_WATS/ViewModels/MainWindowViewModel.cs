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
        
        // Command properties for buttons
        public RelayCommand ConnectCommand { get; }
        public RelayCommand DisconnectCommand { get; }
        
        public MainWindowViewModel(
            RcTcpClientService clientService,
            PositionsService positionsService,
            CapitalService capitalService,
            LimitsService limitsService,
            InstrumentsService instrumentsService,
            ConfigurationService configService,
            HeartbeatMonitorService heartbeatMonitorService)
        {
            _clientService = clientService;
            _configService = configService;
            _heartbeatMonitorService = heartbeatMonitorService;
            
            // Załaduj wartości z konfiguracji
            _serverIp = _configService.ServerIP;
            _serverPort = _configService.ServerPort.ToString();
            
            // Initialize tab ViewModels - pass heartbeat monitor to MessagesTab
            MessagesTab = new MessagesTabViewModel(clientService, positionsService, capitalService, heartbeatMonitorService);
            SettingsTab = new SettingsTabViewModel(clientService, limitsService, configService, _serverIp, _serverPort);
            FiltersTab = new FiltersTabViewModel();
            InstrumentsTab = new InstrumentsTabViewModel(instrumentsService, _configService.InstrumentsFilePath);
            
            // Subscribe to client service events
            _clientService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // Initialize commands
            ConnectCommand = new RelayCommand(async () => await ConnectToServerAsync(), () => !IsConnected);
            DisconnectCommand = new RelayCommand(() => _clientService.Disconnect(), () => IsConnected);
            
            // Initialize UI
            ConnectionStatus = "Rozłączony";
            StatusBarText = "Gotowy";
            
            // Auto-connect if configured
            if (_configService.AutoConnect)
            {
                Task.Run(async () => await ConnectToServerAsync());
            }
        }
        
        private void OnConnectionStatusChanged(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? "Połączony" : "Rozłączony";
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            
            if (isConnected)
            {
                StatusBarText = $"Połączony z {_serverIp}:{_serverPort}";
            }
            else
            {
                StatusBarText = "Rozłączony";
            }
        }
        
        public async Task ConnectToServerAsync()
        {
            try
            {
                StatusBarText = $"Łączenie z {_serverIp}:{_serverPort}...";
                await _clientService.ConnectAsync(_serverIp, int.Parse(_serverPort));
                
                StatusBarText = "Pobieranie historycznych danych...";
                
                // Rewind messages - get all historical data
                await _clientService.SendRewindAsync(0);
                
                // Load control history
                await SettingsTab.LoadControlHistoryAsync();
            }
            catch (Exception ex)
            {
                StatusBarText = $"Błąd połączenia: {ex.Message}";
                MessageBox.Show($"Błąd podczas łączenia: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}