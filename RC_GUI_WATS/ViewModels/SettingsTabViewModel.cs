using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private readonly FileLoggingService _fileLoggingService;
        
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
                    _fileLoggingService.LogSettings("Server IP changed", $"New IP: {value}");
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
                    _fileLoggingService.LogSettings("Server Port changed", $"New Port: {value}");
                }
            }
        }
        
        // Properties for limits
        public ObservableCollection<ControlLimit> ControlLimits => _limitsService.ControlLimits;
        
        private string _limitsSummary;
        public string LimitsSummary
        {
            get => _limitsSummary;
            set => SetProperty(ref _limitsSummary, value);
        }
        
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
        
        // Log file path display
        private string _logFilePath;
        public string LogFilePath
        {
            get => _logFilePath;
            set => SetProperty(ref _logFilePath, value);
        }
        
        // Commands
        public RelayCommand AddLimitCommand { get; }
        public RelayCommand RefreshLimitsCommand { get; }
        public RelayCommand SaveLimitsCommand { get; }
        public RelayCommand ApplyQuickLimitCommand { get; }
        public RelayCommand OpenLogFileCommand { get; }

        public SettingsTabViewModel(
            RcTcpClientService clientService,
            LimitsService limitsService,
            ConfigurationService configService,
            FileLoggingService fileLoggingService,
            string serverIp,
            string serverPort)
        
        {
            _clientService = clientService;
            _limitsService = limitsService;
            _configService = configService;
            _fileLoggingService = fileLoggingService;
            
            // Initialize properties from configuration
            _serverIp = serverIp;
            _serverPort = serverPort;
            _logFilePath = System.IO.Path.GetFileName(_fileLoggingService.GetLogFilePath());
            
            // Initialize commands
            AddLimitCommand = new RelayCommand(AddLimit);
            RefreshLimitsCommand = new RelayCommand(async () => await LoadControlHistoryAsync());
            SaveLimitsCommand = new RelayCommand(SaveLimits);
            ApplyQuickLimitCommand = new RelayCommand(ApplyQuickLimit);
            OpenLogFileCommand = new RelayCommand(OpenLogFile);
            
            // Subscribe to message events
            _clientService.MessageReceived += OnMessageReceived;
            
            // Subscribe to limits collection changes
            _limitsService.ControlLimits.CollectionChanged += (s, e) => UpdateLimitsSummary();
            
            // Initialize with default values
            QuickScopeTypeSelectedIndex = 0;
            QuickLimitTypeSelectedIndex = 0;
            QuickLimitValue = "";
            RawMessagesText = "";
            
            // Initialize limits summary
            UpdateLimitsSummary();
            
            // Log initialization
            _fileLoggingService.LogSettings("Settings tab initialized", $"Server: {_serverIp}:{_serverPort}");
        }
        
        private void OnMessageReceived(RcMessage message)
        {
            // Log raw message to file
            _fileLoggingService.LogRawMessage(message);
            
            // Log raw messages to the debug panel (existing UI functionality)
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
                case 1: // Instrument type
                    QuickScopeValue = "()";
                    break;
                case 2: // Instrument group
                    QuickScopeValue = "[]";
                    break;
                case 3: // Single instrument
                    QuickScopeValue = "";
                    break;
            }
        }
        
        public async Task LoadControlHistoryAsync()
        {
            if (!_clientService.IsConnected)
            {
                var message = "Nie jesteś połączony z serwerem! Nie można pobrać historii kontroli.";
                MessageBox.Show(message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                _fileLoggingService.LogSettings("Load control history failed", "Not connected to server");
                return;
            }
            
            _fileLoggingService.LogSettings("Loading control history", "Sending get controls history request");
            await _limitsService.LoadControlHistoryAsync();
            
            // Update summary after loading
            UpdateLimitsSummary();
        }
        
        private void AddLimit()
        {
            _fileLoggingService.LogSettings("Add limit dialog opened");
            
            var addLimitWindow = new AddLimitWindow();
            if (addLimitWindow.ShowDialog() == true)
            {
                var newLimit = addLimitWindow.ControlLimit;
                if (newLimit != null)
                {
                    // Add to collection
                    _limitsService.ControlLimits.Add(newLimit);
                    _fileLoggingService.LogControlLimit("Added new limit", newLimit.ToControlString());
                    
                    // Send to server
                    SendControlLimit(newLimit);
                }
            }
            else
            {
                _fileLoggingService.LogSettings("Add limit dialog cancelled");
            }
        }
        
        private async void SendControlLimit(ControlLimit limit)
        {
            if (_clientService.IsConnected)
            {
                try
                {
                    await _limitsService.SendControlLimitAsync(limit);
                    var message = $"Wysłano limit: {limit.ToControlString()}";
                    LogMessage(message);
                    _fileLoggingService.LogControlLimit("Sent control limit", limit.ToControlString());
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Błąd podczas wysyłania limitu: {ex.Message}";
                    MessageBox.Show(errorMessage, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    _fileLoggingService.LogError($"Failed to send control limit: {limit.ToControlString()}", ex);
                }
            }
            else
            {
                var message = "Nie jesteś połączony z serwerem!";
                MessageBox.Show(message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                _fileLoggingService.LogSettings("Send control limit failed", "Not connected to server");
            }
        }
        
        private void SaveLimits()
        {
            if (_limitsService.ControlLimits.Count == 0)
            {
                var message = "Brak limitów do zapisania.";
                MessageBox.Show(message, "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                _fileLoggingService.LogSettings("Save limits", "No limits to save");
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
                    // Sort limits according to the 4-tier hierarchy: 1.ALL 2.Types 3.Groups 4.ISIN
                    var sortedLimits = new System.Collections.Generic.List<ControlLimit>(_limitsService.ControlLimits);
                    
                    sortedLimits.Sort((a, b) => 
                    {
                        // Get scope types using the enum
                        var aScopeType = a.GetScopeType();
                        var bScopeType = b.GetScopeType();
                        
                        // Compare by scope type priority first (enum values define priority)
                        if (aScopeType != bScopeType)
                            return aScopeType.CompareTo(bScopeType);
                        
                        // Within same scope type, sort by limit name first
                        int nameComparison = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                        if (nameComparison != 0)
                            return nameComparison;
                        
                        // If same limit name, sort by scope value
                        return string.Compare(a.Scope, b.Scope, StringComparison.OrdinalIgnoreCase);
                    });
                    
                    // Write to file with section headers for better readability
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(saveFileDialog.FileName))
                    {
                        // Write CSV header
                        writer.WriteLine("Scope,Name,Value");
                        
                        // Track current section to add comments
                        ScopeType? currentSectionType = null;
                        int sectionCount = 0;
                        
                        foreach (var limit in sortedLimits)
                        {
                            var limitScopeType = limit.GetScopeType();
                            
                            // Add section comment when scope type changes
                            if (currentSectionType != limitScopeType)
                            {
                                if (currentSectionType.HasValue)
                                    writer.WriteLine(); // Empty line between sections
                                
                                // Add section header as comment
                                string sectionName = GetSectionName(limitScopeType);
                                writer.WriteLine($"# {sectionName}");
                                currentSectionType = limitScopeType;
                                sectionCount++;
                            }
                            
                            // Write the limit
                            writer.WriteLine(limit.ToControlString());
                        }
                        
                        // Add summary at the end
                        writer.WriteLine();
                        writer.WriteLine($"# Summary: {sortedLimits.Count} total limits in {sectionCount} sections");
                        writer.WriteLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    }
                    
                    // Count limits by type for detailed logging
                    var allCount = sortedLimits.Count(l => l.GetScopeType() == ScopeType.AllInstruments);
                    var typeCount = sortedLimits.Count(l => l.GetScopeType() == ScopeType.InstrumentType);
                    var groupCount = sortedLimits.Count(l => l.GetScopeType() == ScopeType.InstrumentGroup);
                    var isinCount = sortedLimits.Count(l => l.GetScopeType() == ScopeType.SingleInstrument);
                    
                    var successMessage = $"Zapisano {sortedLimits.Count} limitów do pliku.\n\n" +
                                       $"Podział:\n" +
                                       $"• Wszystkie instrumenty (ALL): {allCount}\n" +
                                       $"• Typy instrumentów: {typeCount}\n" +
                                       $"• Grupy instrumentów: {groupCount}\n" +
                                       $"• Pojedyncze instrumenty (ISIN): {isinCount}";
                    
                    MessageBox.Show(successMessage, "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    _fileLoggingService.LogSettings("Limits saved to file", 
                        $"File: {saveFileDialog.FileName}, Total: {sortedLimits.Count} " +
                        $"(ALL: {allCount}, Types: {typeCount}, Groups: {groupCount}, ISIN: {isinCount})");
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Błąd podczas zapisywania limitów: {ex.Message}";
                    MessageBox.Show(errorMessage, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    _fileLoggingService.LogError("Failed to save limits to file", ex);
                }
            }
            else
            {
                _fileLoggingService.LogSettings("Save limits dialog cancelled");
            }
        }
        
        private string GetSectionName(ScopeType scopeType)
        {
            return scopeType switch
            {
                ScopeType.AllInstruments => "1. ALL INSTRUMENTS - Limits applied to all instruments",
                ScopeType.InstrumentType => "2. INSTRUMENT TYPES - Limits applied to specific instrument types",
                ScopeType.InstrumentGroup => "3. INSTRUMENT GROUPS - Limits applied to instrument groups",
                ScopeType.SingleInstrument => "4. SINGLE INSTRUMENTS - Limits applied to individual ISINs",
                _ => "UNKNOWN SCOPE TYPE"
            };
        }
        
        private async void ApplyQuickLimit()
        {
            if (!_clientService.IsConnected)
            {
                var message = "Nie jesteś połączony z serwerem!";
                MessageBox.Show(message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                _fileLoggingService.LogSettings("Apply quick limit failed", "Not connected to server");
                return;
            }
            
            // Validation
            if (string.IsNullOrWhiteSpace(QuickScopeValue))
            {
                var message = "Podaj wartość zakresu";
                MessageBox.Show(message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _fileLoggingService.LogSettings("Apply quick limit failed", "Empty scope value");
                return;
            }
            
            if (QuickLimitTypeSelectedIndex < 0)
            {
                var message = "Wybierz typ limitu";
                MessageBox.Show(message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _fileLoggingService.LogSettings("Apply quick limit failed", "No limit type selected");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(QuickLimitValue))
            {
                var message = "Podaj wartość limitu";
                MessageBox.Show(message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _fileLoggingService.LogSettings("Apply quick limit failed", "Empty limit value");
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
                
                var logMessage = $"Wysłano limit: {controlString}";
                LogMessage(logMessage);
                _fileLoggingService.LogControlLimit("Applied quick limit", controlString);
                
                await LoadControlHistoryAsync();
                UpdateLimitsSummary();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Błąd podczas wysyłania limitu: {ex.Message}";
                MessageBox.Show(errorMessage, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _fileLoggingService.LogError("Failed to apply quick limit", ex);
            }
        }
        
        private void OpenLogFile()
        {
            try
            {
                var fullPath = _fileLoggingService.GetLogFilePath();
                if (System.IO.File.Exists(fullPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                    _fileLoggingService.LogSettings("Log file opened", fullPath);
                }
                else
                {
                    MessageBox.Show("Plik dziennika nie istnieje.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _fileLoggingService.LogSettings("Open log file failed", "File does not exist");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas otwierania pliku dziennika: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                _fileLoggingService.LogError("Failed to open log file", ex);
            }
        }
        
        private void LogMessage(string message)
        {
            RawMessagesText += $"{DateTime.Now:HH:mm:ss.fff}: {message}\n";
            _fileLoggingService.LogMessage($"UI_LOG: {message}");
        }
        
        private void UpdateLimitsSummary()
        {
            LimitsSummary = _limitsService.GetLimitsSummary();
        }
    }
}