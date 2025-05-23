using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using RC_GUI_WATS.Commands;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;

namespace RC_GUI_WATS.ViewModels
{
    public class SettingsTabViewModel : BaseViewModel
    {
        private readonly RcTcpClientService _clientService;
        private readonly LimitsService _limitsService;
        private readonly ConfigurationService _configService;
        
        // Properties for settings
        private string _serverIp;
        public string ServerIp
        {
            get => _serverIp;
            set 
            {
                if (SetProperty(ref _serverIp, value))
                {
                    // Zapisz zmiany w konfiguracji
                    _configService.UpdateConfigValue("ServerIP", value);
                }
            }
        }
        
        private string _serverPort;
        public string ServerPort
        {
            get => _serverPort;
            set 
            {
                if (SetProperty(ref _serverPort, value))
                {
                    // Zapisz zmiany w konfiguracji
                    _configService.UpdateConfigValue("ServerPort", value);
                }
            }
        }
        
        // Properties for limits
        public ObservableCollection<ControlLimit> ControlLimits => _limitsService.ControlLimits;
        
        // Properties for quick limit change
        private int _quickScopeTypeSelectedIndex;
        public int QuickScopeTypeSelectedIndex
        {
            get => _quickScopeTypeSelectedIndex;
            set
            {
                if (SetProperty(ref _quickScopeTypeSelectedIndex, value))
                {
                    UpdateQuickScopeValue();
                }
            }
        }
        
        private string _quickScopeValue;
        public string QuickScopeValue
        {
            get => _quickScopeValue;
            set => SetProperty(ref _quickScopeValue, value);
        }
        
        private int _quickLimitTypeSelectedIndex;
        public int QuickLimitTypeSelectedIndex
        {
            get => _quickLimitTypeSelectedIndex;
            set => SetProperty(ref _quickLimitTypeSelectedIndex, value);
        }
        
        private string _quickLimitValue;
        public string QuickLimitValue
        {
            get => _quickLimitValue;
            set => SetProperty(ref _quickLimitValue, value);
        }
        
        // Log
        private string _rawMessagesText;
        public string RawMessagesText
        {
            get => _rawMessagesText;
            set => SetProperty(ref _rawMessagesText, value);
        }
        
        // Commands
        public RelayCommand AddLimitCommand { get; }
        public RelayCommand RefreshLimitsCommand { get; }
        public RelayCommand SaveLimitsCommand { get; }
        public RelayCommand ApplyQuickLimitCommand { get; }

        public SettingsTabViewModel(
            RcTcpClientService clientService,
            LimitsService limitsService,
            ConfigurationService configService,
            string serverIp,
            string serverPort)
        
        {
            _clientService = clientService;
            _limitsService = limitsService;
            _configService = configService;
            
            // Initialize properties from configuration
            _serverIp = serverIp;
            _serverPort = serverPort;
            
            // Initialize commands
            AddLimitCommand = new RelayCommand(AddLimit);
            RefreshLimitsCommand = new RelayCommand(async () => await LoadControlHistoryAsync());
            SaveLimitsCommand = new RelayCommand(SaveLimits);
            ApplyQuickLimitCommand = new RelayCommand(ApplyQuickLimit);
            
            // Subscribe to message events
            _clientService.MessageReceived += OnMessageReceived;
            
            // Initialize with default values
            QuickScopeTypeSelectedIndex = 0;
            QuickLimitTypeSelectedIndex = 0;
            QuickLimitValue = "";
            RawMessagesText = "";
        }
        
        private void OnMessageReceived(RcMessage message)
        {
            // Log raw messages to the debug panel
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"--- Wiadomość {DateTime.Now:HH:mm:ss.fff} ---");
            sb.AppendLine($"Sesja: {message.Header.Session}, Sekwencja: {message.Header.SequenceNumber}, Bloków: {message.Header.BlockCount}");
            
            int blockIndex = 0;
            foreach (var block in message.Blocks)
            {
                sb.AppendLine($"Blok {blockIndex++} | Długość: {block.Length}");
                
                if (block.Payload.Length > 0)
                {
                    char type = (char)block.Payload[0];
                    sb.AppendLine($"Typ: {type}");
                    
                    // Show raw data in hexadecimal
                    sb.AppendLine($"Dane: {BitConverter.ToString(block.Payload)}");
                    
                    // Try to show as ASCII text
                    try
                    {
                        string text = System.Text.Encoding.ASCII.GetString(block.Payload);
                        sb.AppendLine($"Tekst: {text}");
                    }
                    catch { }
                }
                
                sb.AppendLine();
            }
            
            sb.AppendLine();
            
            // Limit log size
            if (RawMessagesText.Length > 10000)
            {
                RawMessagesText = RawMessagesText.Substring(5000);
            }
            
            RawMessagesText += sb.ToString();
        }
        
        private void UpdateQuickScopeValue()
        {
            switch (QuickScopeTypeSelectedIndex)
            {
                case 0: // All instruments
                    QuickScopeValue = "(ALL)";
                    break;
                case 1: // Instrument group
                    QuickScopeValue = "[11*]";
                    break;
                case 2: // Single instrument
                    QuickScopeValue = "PLPKO0000016";
                    break;
            }
        }
        
        public async Task LoadControlHistoryAsync()
        {
            if (!_clientService.IsConnected)
            {
                MessageBox.Show("Nie jesteś połączony z serwerem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            await _limitsService.LoadControlHistoryAsync();
        }
        
        private void AddLimit()
        {
            var addLimitWindow = new AddLimitWindow();
            if (addLimitWindow.ShowDialog() == true)
            {
                var newLimit = addLimitWindow.ControlLimit;
                if (newLimit != null)
                {
                    // Add to collection
                    _limitsService.ControlLimits.Add(newLimit);
                    
                    // Send to server
                    SendControlLimit(newLimit);
                }
            }
        }
        
        private async void SendControlLimit(ControlLimit limit)
        {
            if (_clientService.IsConnected)
            {
                try
                {
                    await _limitsService.SendControlLimitAsync(limit);
                    LogMessage($"Wysłano limit: {limit.ToControlString()}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas wysyłania limitu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Nie jesteś połączony z serwerem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private void SaveLimits()
        {
            if (_limitsService.ControlLimits.Count == 0)
            {
                MessageBox.Show("Brak limitów do zapisania.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "csv",
                Title = "Zapisz limity do pliku"
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Sort limits according to requested order
                    var sortedLimits = new System.Collections.Generic.List<ControlLimit>(_limitsService.ControlLimits);
                    
                    sortedLimits.Sort((a, b) => 
                    {
                        // Helper function to get priority of scope type
                        int GetScopePriority(string scope)
                        {
                            if (scope == "(ALL)")
                                return 0; // Highest priority
                            else if (scope.StartsWith("[") && scope.EndsWith("]"))
                                return 1; // Middle priority
                            else
                                return 2; // Lowest priority (individual ISINs)
                        }
                        
                        // Compare by scope priority first
                        int aPriority = GetScopePriority(a.Scope);
                        int bPriority = GetScopePriority(b.Scope);
                        
                        if (aPriority != bPriority)
                            return aPriority.CompareTo(bPriority);
                        
                        // If same type, compare by name
                        if (a.Name != b.Name)
                            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                        
                        // If same name, compare by scope value
                        return string.Compare(a.Scope, b.Scope, StringComparison.OrdinalIgnoreCase);
                    });
                    
                    // Write to file
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(saveFileDialog.FileName))
                    {
                        // Write header
                        writer.WriteLine("Scope,Name,Value");
                        
                        // Write sorted limits
                        foreach (var limit in sortedLimits)
                        {
                            writer.WriteLine(limit.ToControlString());
                        }
                    }
                    
                    MessageBox.Show($"Zapisano {sortedLimits.Count} limitów do pliku.", "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisywania limitów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private async void ApplyQuickLimit()
        {
            if (!_clientService.IsConnected)
            {
                MessageBox.Show("Nie jesteś połączony z serwerem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Validation
            if (string.IsNullOrWhiteSpace(QuickScopeValue))
            {
                MessageBox.Show("Podaj wartość zakresu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (QuickLimitTypeSelectedIndex < 0)
            {
                MessageBox.Show("Wybierz typ limitu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(QuickLimitValue))
            {
                MessageBox.Show("Podaj wartość limitu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                // Create control string
                string limitType = "";
                switch (QuickLimitTypeSelectedIndex)
                {
                    case 0: limitType = "halt"; break;
                    case 1: limitType = "maxOrderRate"; break;
                    case 2: limitType = "maxTransaction"; break;
                    case 3: limitType = "maxAbsShares"; break;
                    case 4: limitType = "maxShortShares"; break;
                }
                
                string controlString = $"{QuickScopeValue},{limitType},{QuickLimitValue}";
                
                // Send to server
                await _clientService.SendSetControlAsync(controlString);
                
                LogMessage($"Wysłano limit: {controlString}");
                
                await LoadControlHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wysyłania limitu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LogMessage(string message)
        {
            RawMessagesText += $"{message}\n";
        }
    }
}