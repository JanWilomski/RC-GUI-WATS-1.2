using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
        private readonly ThemeService _themeService;
        private readonly SettingsService _settingsService;
        
        // Definicje typów limitów dla różnych zakresów (jak w AddLimitWindow)
        private readonly List<LimitTypeInfo> _allInstrumentsLimits = new List<LimitTypeInfo>
        {
            new LimitTypeInfo("halt", "Y/N", "Zatrzymuje handel na wszystkich instrumentach"),
            new LimitTypeInfo("maxMessageCount", "liczba", "Maksymalna liczba wiadomości"),
            new LimitTypeInfo("maxOrderRate", "liczba/s", "Maksymalny rate zleceń, np. 100/s"),
            new LimitTypeInfo("maxTransaction", "procent", "Procent nominalnej wartości transakcji"),
            new LimitTypeInfo("maxAbsShares", "liczba", "Maksymalna bezwzględna pozycja"),
            new LimitTypeInfo("maxShortShares", "liczba", "Maksymalna pozycja krótka"),
            new LimitTypeInfo("maxCapital", "kwota", "Maksymalny kapitał"),
            new LimitTypeInfo("collars", "wartość", "Ograniczenia cenowe"),
            new LimitTypeInfo("maxShortCapital", "kwota", "Maksymalny kapitał pozycji krótkiej"),
            new LimitTypeInfo("capitalImpact", "liczba", "")
        };
        
        private readonly List<LimitTypeInfo> _otherScopesLimits = new List<LimitTypeInfo>
        {
            new LimitTypeInfo("halt", "Y/N", "Zatrzymuje handel na wybranych instrumentach"),
            new LimitTypeInfo("maxTransaction", "procent", "Procent nominalnej wartości transakcji"),
            new LimitTypeInfo("maxShortCapital", "kwota", "Maksymalny kapitał pozycji krótkiej"),
            new LimitTypeInfo("capitalImpact", "procent", "Wpływ na kapitał (tylko futures)"),
            new LimitTypeInfo("maxAbsShares", "liczba", "Maksymalna bezwzględna pozycja"),
            new LimitTypeInfo("maxShortShares", "liczba", "Maksymalna pozycja krótka"),
            new LimitTypeInfo("collars", "wartość", "Ograniczenia cenowe"),
        };

        // Dostępne typy limitów do bindowania w XAML
        private ObservableCollection<LimitTypeInfo> _availableLimitTypes = new ObservableCollection<LimitTypeInfo>();
        public ObservableCollection<LimitTypeInfo> AvailableLimitTypes
        {
            get => _availableLimitTypes;
            set => SetProperty(ref _availableLimitTypes, value);
        }
        
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
        
        // Properties for limits display mode
        private LimitsDisplayMode _displayMode = LimitsDisplayMode.Hierarchical;
        public LimitsDisplayMode DisplayMode
        {
            get => _displayMode;
            set
            {
                if (SetProperty(ref _displayMode, value))
                {
                    RefreshDisplayedLimits();
                    UpdateDisplayModeInfo();
                    _fileLoggingService.LogSettings("Limits display mode changed", value.ToString());
                }
            }
        }
        
        public bool IsChronologicalMode
        {
            get => DisplayMode == LimitsDisplayMode.Chronological;
            set
            {
                if (value)
                    DisplayMode = LimitsDisplayMode.Chronological;
            }
        }
        
        public bool IsHierarchicalMode
        {
            get => DisplayMode == LimitsDisplayMode.Hierarchical;
            set
            {
                if (value)
                    DisplayMode = LimitsDisplayMode.Hierarchical;
            }
        }
        
        // Displayed limits collection (sorted based on display mode)
        private ObservableCollection<ControlLimit> _displayedLimits = new ObservableCollection<ControlLimit>();
        public ObservableCollection<ControlLimit> DisplayedLimits
        {
            get => _displayedLimits;
            set => SetProperty(ref _displayedLimits, value);
        }
        
        // Original limits collection reference
        public ObservableCollection<ControlLimit> ControlLimits => _limitsService.ControlLimits;
        
        private string _limitsSummary;
        public string LimitsSummary
        {
            get => _limitsSummary;
            set => SetProperty(ref _limitsSummary, value);
        }
        
        private string _displayModeInfo;
        public string DisplayModeInfo
        {
            get => _displayModeInfo;
            set => SetProperty(ref _displayModeInfo, value);
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
                    UpdateAvailableLimitTypes(); // Dodaj tę linię
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
        public RelayCommand OpenSettingsWindowCommand { get; }

        public SettingsTabViewModel(
            RcTcpClientService clientService,
            LimitsService limitsService,
            ConfigurationService configService,
            FileLoggingService fileLoggingService,
            ThemeService themeService,
            string serverIp,
            string serverPort,
            Services.SettingsService settingsService)
        {
            _clientService = clientService;
            _limitsService = limitsService;
            _configService = configService;
            _fileLoggingService = fileLoggingService;
            _themeService = themeService;
            _settingsService = settingsService;
            
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
            OpenSettingsWindowCommand = new RelayCommand(OpenSettingsWindow);
            
            // Subscribe to message events
            _clientService.MessageReceived += OnMessageReceived;
            
            // Subscribe to limits collection changes
            _limitsService.ControlLimits.CollectionChanged += OnLimitsCollectionChanged;
            
            // Initialize with default values
            QuickScopeTypeSelectedIndex = 0;
            QuickLimitValue = "";
            RawMessagesText = "";
            
            // Initialize available limit types for default scope (ALL)
            UpdateAvailableLimitTypes();
            
            // Initialize limits summary and display mode info
            UpdateLimitsSummary();
            UpdateDisplayModeInfo();
            RefreshDisplayedLimits();
            
            // Log initialization
            _fileLoggingService.LogSettings("Settings tab initialized", $"Server: {_serverIp}:{_serverPort}");
        }
        
        private void UpdateAvailableLimitTypes()
        {
            // Zapisz aktualnie wybraną wartość
            LimitTypeInfo currentSelection = null;
            if (QuickLimitTypeSelectedIndex >= 0 && QuickLimitTypeSelectedIndex < AvailableLimitTypes.Count)
            {
                currentSelection = AvailableLimitTypes[QuickLimitTypeSelectedIndex];
            }

            // Wyczyść obecne opcje
            AvailableLimitTypes.Clear();

            // Wybierz odpowiednią listę limitów
            List<LimitTypeInfo> limitsToShow;
            if (QuickScopeTypeSelectedIndex == 0) // Wszystkie instrumenty (ALL)
            {
                limitsToShow = _allInstrumentsLimits;
            }
            else // Inne zakresy
            {
                limitsToShow = _otherScopesLimits;
            }

            // Dodaj opcje do kolekcji
            foreach (var limitInfo in limitsToShow)
            {
                AvailableLimitTypes.Add(limitInfo);
            }

            // Spróbuj przywrócić poprzednią selekcję lub wybierz pierwszy element
            bool selectionRestored = false;
            if (currentSelection != null)
            {
                for (int i = 0; i < AvailableLimitTypes.Count; i++)
                {
                    if (AvailableLimitTypes[i].Name == currentSelection.Name)
                    {
                        QuickLimitTypeSelectedIndex = i;
                        selectionRestored = true;
                        break;
                    }
                }
            }

            if (!selectionRestored && AvailableLimitTypes.Count > 0)
            {
                QuickLimitTypeSelectedIndex = 0;
            }
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
        
        private void OnLimitsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshDisplayedLimits();
            UpdateLimitsSummary();
        }
        
        private void RefreshDisplayedLimits()
        {
            var sourceList = _limitsService.ControlLimits.ToList();
            
            if (DisplayMode == LimitsDisplayMode.Chronological)
            {
                // Sort by received time (chronological order)
                sourceList = sourceList.OrderBy(l => l.ReceivedTime).ToList();
            }
            else // Hierarchical
            {
                // Sort hierarchically: ALL -> Types -> Groups -> ISIN
                sourceList.Sort((a, b) => 
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
            }
            
            // Update the displayed collection
            _displayedLimits.Clear();
            foreach (var limit in sourceList)
            {
                _displayedLimits.Add(limit);
            }
            
            UpdateDisplayModeInfo();
        }
        
        private void UpdateDisplayModeInfo()
        {
            if (DisplayMode == LimitsDisplayMode.Chronological)
            {
                DisplayModeInfo = $"Wyświetlanie: chronologiczne ({_displayedLimits.Count} limitów w kolejności otrzymania)";
            }
            else
            {
                var allCount = _displayedLimits.Count(l => l.GetScopeType() == ScopeType.AllInstruments);
                var typeCount = _displayedLimits.Count(l => l.GetScopeType() == ScopeType.InstrumentType);
                var groupCount = _displayedLimits.Count(l => l.GetScopeType() == ScopeType.InstrumentGroup);
                var isinCount = _displayedLimits.Count(l => l.GetScopeType() == ScopeType.SingleInstrument);
                
                DisplayModeInfo = $"Wyświetlanie: hierarchiczne (ALL:{allCount}, Typy:{typeCount}, Grupy:{groupCount}, ISIN:{isinCount})";
            }
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
            
            // Clear existing limits before loading fresh data from server
            _limitsService.ControlLimits.Clear();
            
            // Send Get Controls History (G) request
            await _clientService.SendGetControlsHistoryAsync();
            
            // Small delay to allow server response to be processed
            await Task.Delay(1000); // Longer delay for full reload
            
            _fileLoggingService.LogSettings("Control history load completed", "Server responded with control limits");
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
                    _fileLoggingService.LogControlLimit("Added new limit", newLimit.ToControlString());
                    
                    // Send to server - don't add locally, wait for server confirmation
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
                    
                    // Request updated control history after sending limit
                    await RequestControlHistoryUpdate();
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
                    // Always use hierarchical sorting for file save (as before)
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
            
            if (QuickLimitTypeSelectedIndex < 0 || QuickLimitTypeSelectedIndex >= AvailableLimitTypes.Count)
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
                // Get selected limit type
                string limitType = AvailableLimitTypes[QuickLimitTypeSelectedIndex].Name;
                
                string controlString = $"{QuickScopeValue},{limitType},{QuickLimitValue}";
                
                // Create ControlLimit object for validation and logging
                var newLimit = ControlLimit.FromControlString(controlString);
                if (newLimit != null)
                {
                    _fileLoggingService.LogControlLimit("Applied quick limit", controlString);
                    
                    // Send to server - don't add locally, wait for server confirmation
                    await _limitsService.SendControlLimitAsync(newLimit);
                    
                    var logMessage = $"Wysłano limit: {controlString}";
                    LogMessage(logMessage);
                    
                    // Request updated control history after sending limit
                    await RequestControlHistoryUpdate();
                    
                    // Clear quick limit form
                    QuickLimitValue = "";
                    QuickScopeTypeSelectedIndex = 0; // Reset to "All instruments"
                }
                else
                {
                    _fileLoggingService.LogSettings("Apply quick limit failed", "Invalid control string format");
                    MessageBox.Show("Błąd: nieprawidłowy format limitu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
        
        // Request control history update from server after adding a limit
        private async Task RequestControlHistoryUpdate()
        {
            try
            {
                _fileLoggingService.LogSettings("Requesting control history update", "Sending Get Controls History (G) request");
                await _clientService.SendGetControlsHistoryAsync();
                
                // Small delay to allow server response to be processed
                await Task.Delay(500);
                
                _fileLoggingService.LogSettings("Control history update requested", "Server should respond with updated limits");
            }
            catch (Exception ex)
            {
                _fileLoggingService.LogError("Failed to request control history update", ex);
            }
        }

        private void OpenSettingsWindow()
        {
            var settingsWindowViewModel = new ViewModels.SettingsWindowViewModel(_themeService, _configService);
            var settingsWindow = new Views.SettingsWindow(settingsWindowViewModel);
            settingsWindow.Show();
        }
    }
}