using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using RC_GUI_WATS.Models;
using RC_GUI_WATS.Services;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.IO;
using System.Globalization;
using Microsoft.Win32;


namespace RC_GUI_WATS
{
    public partial class MainWindow : Window
    {
        private RcTcpClient _rcClient;
        private Capital _currentCapital = new Capital();
        private ObservableCollection<Position> _positions = new ObservableCollection<Position>();
        private Dictionary<string, Position> _positionsByIsin = new Dictionary<string, Position>();
        private string _serverIp = "172.31.136.4";
        private int _serverPort = 19083;

        private ObservableCollection<Instrument> _instruments = new ObservableCollection<Instrument>();
        private string _instrumentsFilePath = string.Empty;

        // Kolekcja limitów kontroli
        private ObservableCollection<ControlLimit> _controlLimits = new ObservableCollection<ControlLimit>();

        public MainWindow()
        {
            InitializeComponent();

            LoadConfigurationSettings();

            _rcClient = new RcTcpClient();
            _rcClient.MessageReceived += OnMessageReceived;
            _rcClient.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Inicjalizacja DataGrid dla pozycji
            _positions = new ObservableCollection<Position>();
            PositionsDataGrid.ItemsSource = _positions;

            // Inicjalizacja DataGrid dla limitów
            LimitsDataGrid.ItemsSource = _controlLimits;

            // Ustawienie wartości domyślnych
            _currentCapital.MessagesPercentage = 0;
            _currentCapital.MessagesLimit = 0;
            _currentCapital.CapitalPercentage = 0;
            _currentCapital.CapitalLimit = 0;


            QuickScopeTypeComboBox.SelectedIndex = 0;
            QuickLimitTypeComboBox.SelectedIndex = 0;

            InstrumentsDataGrid.ItemsSource = _instruments;

            //LoadInstrumentsFromCsv();

            UpdateCapitalDisplay();

            // Automatyczne połączenie przy starcie aplikacji
            if(AppConfig.AutoConnect)
            {
                ConnectToServerAsync();
            }
            



        }

        private async void ConnectToServerAsync()
        {
            try
            {
                StatusBarText.Text = $"Łączenie z {_serverIp}:{_serverPort}...";
                await _rcClient.ConnectAsync(_serverIp, _serverPort);

                // Wyczyść bieżące dane przed pobieraniem nowych
                _positions.Clear();
                _positionsByIsin.Clear();

                StatusBarText.Text = "Pobieranie historycznych danych...";

                // Rewindowanie wiadomości - pobieranie wszystkich historycznych danych
                await _rcClient.SendRewindAsync(0);

                // Po połączeniu, pobierz historię kontroli
                await LoadControlHistoryAsync();
            }
            catch (Exception ex)
            {
                StatusBarText.Text = $"Błąd połączenia: {ex.Message}";
                // Nie pokazujemy MessageBox, żeby nie blokować UI przy starcie
                Console.WriteLine($"Błąd podczas łączenia: {ex.Message}");
            }
        }

        private void LoadConfigurationSettings()
        {
            try
            {
                // Load server connection settings
                _serverIp = AppConfig.ServerIP;
                _serverPort = AppConfig.ServerPort;

                // Update UI elements with loaded settings
                if (ServerIpTextBox != null)
                    ServerIpTextBox.Text = _serverIp;
                if (ServerPortTextBox != null)
                    ServerPortTextBox.Text = _serverPort.ToString();

                // Load instruments file path if specified
                _instrumentsFilePath = AppConfig.InstrumentsFilePath;
                if (!string.IsNullOrEmpty(_instrumentsFilePath))
                {
                    // Update UI
                    if (InstrumentsFilePathText != null)
                        InstrumentsFilePathText.Text = System.IO.Path.GetFileName(_instrumentsFilePath);
                    
                    // Load instruments if file exists
                    if (System.IO.File.Exists(_instrumentsFilePath))
                    {
                        LoadInstrumentsFromCsv(_instrumentsFilePath);
                    }
                }

                StatusBarText.Text = "Konfiguracja załadowana";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                StatusBarText.Text = "Błąd podczas ładowania konfiguracji";
            }
        }

        private void OnMessageReceived(RcMessage message)
        {
            // Aktualizacja UI musi być wykonana w wątku UI
            Dispatcher.Invoke(() =>
            {
                // Logowanie surowych danych dla debugowania
                LogRawMessage(message);

                // Przetwarzanie wszystkich bloków wiadomości
                foreach (var block in message.Blocks)
                {
                    if (block.Payload.Length > 0)
                    {
                        char messageType = (char)block.Payload[0];

                        switch (messageType)
                        {
                            case 'P': // Position
                                ProcessPositionMessage(block.Payload);
                                break;
                            case 'C': // Capital
                                ProcessCapitalMessage(block.Payload);
                                break;
                            case 'D': // Debug log
                            case 'I': // Info log
                            case 'W': // Warning log
                            case 'E': // Error log
                                ProcessLogMessage(messageType, block.Payload);
                                break;
                            case 'r': // Rewind complete
                                StatusBarText.Text = "Pobieranie historycznych danych zakończone";
                                break;
                            case 'S': // Set Control
                                ProcessSetControlMessage(block.Payload);
                                break;
                            case 'B': // I/O bytes
                                ProcessIoBytesMessage(block.Payload);
                                break;
                            default:
                                // Nieznany typ wiadomości
                                StatusBarText.Text = $"Otrzymano nieznany typ wiadomości: {messageType}";
                                break;
                        }
                    }
                }

                // Po przetworzeniu wszystkich bloków, zaktualizuj UI
                UpdateCapitalDisplay();
            });
        }

        private void LogRawMessage(RcMessage message)
        {
            // Jeśli nie mamy RawMessagesTextBox, nic nie robimy
            if (RawMessagesTextBox == null)
                return;

            StringBuilder sb = new StringBuilder();

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

                    // Pokazanie surowych danych hexadecymalnie
                    sb.AppendLine($"Dane: {BitConverter.ToString(block.Payload)}");

                    // Próba pokazania jako tekst ASCII
                    try
                    {
                        string text = Encoding.ASCII.GetString(block.Payload);
                        sb.AppendLine($"Tekst: {text}");
                    }
                    catch { }
                }

                sb.AppendLine();
            }

            sb.AppendLine();

            // Ograniczenie rozmiaru logu
            if (RawMessagesTextBox.Text.Length > 10000)
            {
                RawMessagesTextBox.Text = RawMessagesTextBox.Text.Substring(5000);
            }

            RawMessagesTextBox.AppendText(sb.ToString());
            RawMessagesTextBox.ScrollToEnd();
        }

        private void ProcessPositionMessage(byte[] payload)
        {
            if (payload.Length < 25)
            {
                RawMessagesTextBox.AppendText($"ERROR: Wiadomość Position za krótka: {payload.Length} bajtów zamiast minimum 25\n");
                return;
            }

            string isin = Encoding.ASCII.GetString(payload, 1, 12).Trim('\0');
            int net = BitConverter.ToInt32(payload, 13);
            int openLong = BitConverter.ToInt32(payload, 17);
            int openShort = BitConverter.ToInt32(payload, 21);

            // Debug
            RawMessagesTextBox.AppendText($"POSITION: ISIN={isin}, Net={net}, OpenLong={openLong}, OpenShort={openShort}\n");

            // Ignoruj puste ISIN
            if (string.IsNullOrWhiteSpace(isin))
            {
                RawMessagesTextBox.AppendText($"Pominięto pustą pozycję\n");
                return;
            }

            bool isUpdated = false;
            // Sprawdź, czy pozycja już istnieje
            if (_positionsByIsin.TryGetValue(isin, out Position existingPosition))
            {
                // Aktualizuj istniejącą pozycję tylko jeśli wartości się zmieniły
                if (existingPosition.Net != net || existingPosition.OpenLong != openLong || existingPosition.OpenShort != openShort)
                {
                    existingPosition.Net = net;
                    existingPosition.OpenLong = openLong;
                    existingPosition.OpenShort = openShort;
                    isUpdated = true;
                    RawMessagesTextBox.AppendText($"Zaktualizowano pozycję: {isin}\n");
                }
            }
            else
            {
                // Dodaj nową pozycję
                var newPosition = new Position
                {
                    ISIN = isin,
                    Ticker = GetTickerFromIsin(isin),
                    Name = GetNameFromIsin(isin),
                    Net = net,
                    OpenLong = openLong,
                    OpenShort = openShort
                };

                _positions.Add(newPosition);
                _positionsByIsin[isin] = newPosition;
                isUpdated = true;
                RawMessagesTextBox.AppendText($"Dodano nową pozycję: {isin}, liczba pozycji: {_positions.Count}\n");
            }

            // Odśwież UI tylko jeśli dane zostały zaktualizowane
            if (isUpdated)
            {
                RefreshPositionsGrid();
            }
        }

        // Metoda do odświeżania DataGrid pozycji
        private void RefreshPositionsGrid()
        {
            // Wywołaj w wątku UI
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (PositionsDataGrid != null && PositionsDataGrid.ItemsSource != null)
                    {
                        // Wersja 1: Używając Items.Refresh
                        PositionsDataGrid.Items.Refresh();

                        // Wersja 2: Alternatywne podejście, jeśli refresh nie działa
                        // CollectionViewSource.GetDefaultView(PositionsDataGrid.ItemsSource).Refresh();

                        StatusBarText.Text = $"Liczba pozycji: {_positions.Count}";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas odświeżania tabeli pozycji: {ex.Message}");
                }
            });
        }

        private void ProcessCapitalMessage(byte[] payload)
        {
            if (payload.Length < 25)
            {
                RawMessagesTextBox.AppendText($"ERROR: Wiadomość Capital za krótka: {payload.Length} bajtów zamiast minimum 25\n");
                return;
            }

            // Wyświetl surowe dane do analizy
            RawMessagesTextBox.AppendText($"CAPITAL pełne dane: {BitConverter.ToString(payload)}\n");

            try
            {
                // Zgodnie z dokumentacją:
                // - Offset 1: 8-bajtowy double IEEE 754 (Open Capital)
                // - Offset 9: 8-bajtowy double IEEE 754 (Accrued Capital)
                // - Offset 17: 8-bajtowy double IEEE 754 (Total Capital)

                double openCapital = BitConverter.ToDouble(payload, 1);
                double accruedCapital = BitConverter.ToDouble(payload, 9);
                double totalCapital = BitConverter.ToDouble(payload, 17);

                RawMessagesTextBox.AppendText($"CAPITAL (surowe wartości): Open={openCapital}, Accrued={accruedCapital}, Total={totalCapital}\n");

                // Aktualizuj model tylko jeśli wartości są sensowne
                // Sprawdźmy, czy wartości są w rozsądnym zakresie - jeśli nie, mogą być konieczne dodatkowe 
                // transformacje lub inne podejście do interpretacji danych
                bool validValues = !double.IsNaN(openCapital) && !double.IsInfinity(openCapital) &&
                                !double.IsNaN(accruedCapital) && !double.IsInfinity(accruedCapital) &&
                                !double.IsNaN(totalCapital) && !double.IsInfinity(totalCapital);

                if (validValues)
                {
                    // Zaktualizuj model
                    _currentCapital.OpenCapital = openCapital;
                    _currentCapital.AccruedCapital = accruedCapital;
                    _currentCapital.TotalCapital = totalCapital;

                    RawMessagesTextBox.AppendText($"CAPITAL (po aktualizacji): Open={_currentCapital.OpenCapital}, Accrued={_currentCapital.AccruedCapital}, Total={_currentCapital.TotalCapital}\n");

                    // Upewnij się, że UI jest natychmiast aktualizowane
                    UpdateCapitalDisplay();
                }
                else
                {
                    RawMessagesTextBox.AppendText($"Odrzucono nieprawidłowe wartości kapitału\n");

                    // Spróbujmy alternatywnej interpretacji - może to jednak Int32 lub Int64
                    long openCapitalInt64 = BitConverter.ToInt64(payload, 1);
                    long accruedCapitalInt64 = BitConverter.ToInt64(payload, 9);
                    long totalCapitalInt64 = BitConverter.ToInt64(payload, 17);

                    int openCapitalInt32 = BitConverter.ToInt32(payload, 1);
                    int accruedCapitalInt32 = BitConverter.ToInt32(payload, 9);
                    int totalCapitalInt32 = BitConverter.ToInt32(payload, 17);

                    RawMessagesTextBox.AppendText($"Alternatywne interpretacje:\n");
                    RawMessagesTextBox.AppendText($"Int64: Open={openCapitalInt64}, Accrued={accruedCapitalInt64}, Total={totalCapitalInt64}\n");
                    RawMessagesTextBox.AppendText($"Int32: Open={openCapitalInt32}, Accrued={accruedCapitalInt32}, Total={totalCapitalInt32}\n");

                    // Więcej debugowania - wypisz wszystkie bajty jako wartości liczbowe
                    RawMessagesTextBox.AppendText("Wartości poszczególnych bajtów:\n");
                    for (int i = 0; i < 8; i++)
                    {
                        RawMessagesTextBox.AppendText($"Open[{i}]={payload[1 + i]}, Accrued[{i}]={payload[9 + i]}, Total[{i}]={payload[17 + i]}\n");
                    }
                }
            }
            catch (Exception ex)
            {
                RawMessagesTextBox.AppendText($"BŁĄD przy przetwarzaniu danych Capital: {ex.Message}\n");
            }
        }

        private void ProcessLogMessage(char logType, byte[] payload)
        {
            if (payload.Length < 3)
                return;

            ushort msgLength = BitConverter.ToUInt16(payload, 1);
            if (payload.Length < 3 + msgLength)
                return;

            string message = Encoding.ASCII.GetString(payload, 3, msgLength);
            string logLevel = "";

            switch (logType)
            {
                case 'D': logLevel = "DEBUG"; break;
                case 'I': logLevel = "INFO"; break;
                case 'W': logLevel = "WARNING"; break;
                case 'E': logLevel = "ERROR"; break;
            }

            StatusBarText.Text = $"[{logLevel}] {message}";

            // Sprawdź, czy wiadomość info zawiera kontrolę
            // Po żądaniu GetControlsHistory serwer wysyła historię kontroli jako wiadomości 'I'
            if (logType == 'I' && message.Contains(','))
            {
                // Próba interpretacji jako kontroli
                try
                {
                    // Sprawdź, czy to może być kontrola (np. "(ALL),halt,Y")
                    if (message.Contains("(ALL)") || message.Contains("[") || Regex.IsMatch(message, @"^[A-Z0-9]{12}"))
                    {
                        UpdateControlLimit(message);
                    }
                }
                catch
                {
                    // Ignoruj błędy przy próbie interpretacji - to może nie być kontrola
                }
            }

            // Często limity są przekazywane w wiadomościach logów
            TryExtractLimitsFromLog(message);
        }

        private void TryExtractLimitsFromLog(string message)
        {
            RawMessagesTextBox.AppendText($"Analiza wiadomości pod kątem limitów: {message}\n");

            // Sprawdź, czy wiadomość zawiera informacje o procentach i limitach
            if ((message.Contains("%") || message.Contains("percent")) && message.Contains("of"))
            {
                try
                {
                    // Szukaj pozycji % lub słowa percent
                    int percentIndex = message.IndexOf('%');
                    if (percentIndex < 0)
                    {
                        percentIndex = message.IndexOf("percent");
                        if (percentIndex < 0)
                            return;
                    }

                    // Znajdź początek liczby przed %
                    int i = percentIndex - 1;
                    while (i >= 0 && (char.IsDigit(message[i]) || message[i] == '.'))
                        i--;

                    string percentText = message.Substring(i + 1, percentIndex - i - 1).Trim();
                    double percent = double.Parse(percentText);

                    // Znajdź "of" i liczbę po nim
                    int ofIndex = message.IndexOf("of", percentIndex);
                    if (ofIndex < 0)
                        return;

                    // Znajdź początek liczby po "of"
                    i = ofIndex + 2;
                    while (i < message.Length && char.IsWhiteSpace(message[i]))
                        i++;

                    // Znajdź koniec liczby
                    int j = i;
                    while (j < message.Length && (char.IsDigit(message[j]) || message[j] == '.'))
                        j++;

                    string limitText = message.Substring(i, j - i).Trim();
                    double limit = double.Parse(limitText);

                    RawMessagesTextBox.AppendText($"Znaleziono limit: {percent}% of {limit}\n");

                    // Określ, czy dotyczy wiadomości czy kapitału
                    if (message.Contains("message", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("order", StringComparison.OrdinalIgnoreCase))
                    {
                        _currentCapital.MessagesPercentage = percent;
                        _currentCapital.MessagesLimit = limit;
                        RawMessagesTextBox.AppendText("Zaktualizowano limity wiadomości\n");
                    }
                    else if (message.Contains("capital", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("position", StringComparison.OrdinalIgnoreCase))
                    {
                        _currentCapital.CapitalPercentage = percent;
                        _currentCapital.CapitalLimit = limit;
                        RawMessagesTextBox.AppendText("Zaktualizowano limity kapitału\n");
                    }

                    // Aktualizuj UI
                    UpdateCapitalDisplay();
                }
                catch (Exception ex)
                {
                    // Nie udało się przetworzyć wiadomości
                    RawMessagesTextBox.AppendText($"Błąd podczas analizy limitów: {ex.Message}\n");
                }
            }
        }

        private void ProcessSetControlMessage(byte[] payload)
        {
            if (payload.Length < 3)
                return;

            ushort length = BitConverter.ToUInt16(payload, 1);
            if (payload.Length < 3 + length)
                return;

            string controlString = Encoding.ASCII.GetString(payload, 3, length);
            StatusBarText.Text = $"Control: {controlString}";
            RawMessagesTextBox.AppendText($"SET CONTROL: {controlString}\n");

            // Dodaj lub zaktualizuj limit w kolekcji
            UpdateControlLimit(controlString);

            // Sprawdzenie, czy kontrola zawiera informacje o limitach
            if (controlString.Contains("maxOrderRate"))
            {
                try
                {
                    // Format: (ALL),maxOrderRate,100/s
                    string[] parts = controlString.Split(',');
                    if (parts.Length >= 3)
                    {
                        string rateValue = parts[2];
                        // Usunięcie '/s' lub innych jednostek
                        if (rateValue.Contains("/"))
                        {
                            rateValue = rateValue.Split('/')[0];
                        }
                        double rate = double.Parse(rateValue);

                        _currentCapital.MessagesLimit = rate;
                        RawMessagesTextBox.AppendText($"Znaleziono limit wiadomości: {rate}\n");
                        UpdateCapitalDisplay();
                    }
                }
                catch (Exception ex)
                {
                    RawMessagesTextBox.AppendText($"Błąd podczas parsowania kontroli: {ex.Message}\n");
                }
            }
            else if (controlString.Contains("maxAbsShares") || controlString.Contains("maxShortShares"))
            {
                // Parsowanie limitów pozycji dla konkretnych instrumentów
                try
                {
                    string[] parts = controlString.Split(',');
                    if (parts.Length >= 3)
                    {
                        string isin = parts[0];
                        string limitType = parts[1];
                        int limitValue = int.Parse(parts[2]);

                        RawMessagesTextBox.AppendText($"Limit dla instrumentu {isin}: {limitType}={limitValue}\n");
                    }
                }
                catch (Exception ex)
                {
                    RawMessagesTextBox.AppendText($"Błąd podczas parsowania limitów instrumentu: {ex.Message}\n");
                }
            }
        }

        private void ProcessIoBytesMessage(byte[] payload)
        {
            if (payload.Length < 3)
                return;

            ushort length = BitConverter.ToUInt16(payload, 1);
            if (payload.Length < 3 + length)
                return;

            string message = Encoding.ASCII.GetString(payload, 3, length);

            // Sprawdzenie, czy wiadomość CCG zawiera informacje o limitach
            RawMessagesTextBox.AppendText($"I/O BYTES: {message}\n");
            TryExtractLimitsFromLog(message); // Używamy tej samej metody, jeśli format jest podobny
        }

        // Metoda do pobierania historii kontroli
        private async Task LoadControlHistoryAsync()
        {
            if (!_rcClient.IsConnected)
                return;

            StatusBarText.Text = "Pobieranie historii kontroli...";

            // Wyczyść istniejące dane
            _controlLimits.Clear();

            // Wyślij żądanie historii kontroli
            await _rcClient.SendGetControlsHistoryAsync();

            // Aktualizacja zostanie obsłużona przez obsługę wiadomości typu 'I'/'S'
        }

        // Metoda do dodawania/aktualizacji limitu
        private void UpdateControlLimit(string controlString)
        {
            var limit = ControlLimit.FromControlString(controlString);
            if (limit == null)
                return;

            // Sprawdź, czy taki limit już istnieje
            var existingLimit = _controlLimits.FirstOrDefault(l =>
                l.Scope == limit.Scope && l.Name == limit.Name);

            if (existingLimit != null)
            {
                // Aktualizuj istniejący limit
                existingLimit.Value = limit.Value;

                // Odśwież widok
                Dispatcher.Invoke(() =>
                {
                    if (LimitsDataGrid != null)
                        LimitsDataGrid.Items.Refresh();
                });
            }
            else
            {
                // Dodaj nowy limit na początku kolekcji, aby najnowsze były na górze
                Dispatcher.Invoke(() =>
                {
                    _controlLimits.Insert(0, limit); // Dodaj na początek zamiast _controlLimits.Add(limit);
                });
            }

            RawMessagesTextBox.AppendText($"Zaktualizowano limit: {limit.Scope}, {limit.Name}, {limit.Value}\n");
        }

        // Obsługa przycisku dodawania limitu
        private void AddLimitButton_Click(object sender, RoutedEventArgs e)
        {
            var addLimitWindow = new AddLimitWindow();
            if (addLimitWindow.ShowDialog() == true)
            {
                var newLimit = addLimitWindow.ControlLimit;
                if (newLimit != null)
                {
                    // Dodaj do kolekcji
                    _controlLimits.Add(newLimit);

                    // Wyślij do serwera
                    SendControlLimit(newLimit);
                }
            }
        }

        // Obsługa przycisku odświeżania limitów
        private async void RefreshLimitsButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadControlHistoryAsync();
        }

        // Obsługa przycisku stosowania limitu
        private void ApplyLimitButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is ControlLimit limit)
            {
                SendControlLimit(limit);
            }
        }

        // Obsługa przycisku usuwania limitu
        private void RemoveLimitButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.DataContext is ControlLimit limit)
            {
                _controlLimits.Remove(limit);
            }
        }

        // Metoda do wysyłania limitu do serwera
        private async void SendControlLimit(ControlLimit limit)
        {
            if (_rcClient.IsConnected)
            {
                try
                {
                    string controlString = limit.ToControlString();
                    await _rcClient.SendSetControlAsync(controlString);
                    StatusBarText.Text = $"Wysłano limit: {controlString}";
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

        // Pomocnicza metoda do testowania aktualizacji kapitału
        private void TestCapitalUpdate()
        {
            // Testowe wartości
            _currentCapital.OpenCapital = 123;
            _currentCapital.AccruedCapital = 123;
            _currentCapital.TotalCapital = _currentCapital.OpenCapital + _currentCapital.AccruedCapital;

            // Aktualizuj UI
            UpdateCapitalDisplay();

            // Zaloguj
            Console.WriteLine("Testowo zaktualizowano wartości kapitału");
            if (RawMessagesTextBox != null)
            {
                RawMessagesTextBox.AppendText("TEST: Zaktualizowano wartości kapitału\n");
                RawMessagesTextBox.AppendText($"TEST CAPITAL: Open={_currentCapital.OpenCapital}, Accrued={_currentCapital.AccruedCapital}, Total={_currentCapital.TotalCapital}\n");
            }
        }

        // Obsługa przycisku testowego 
        private void TestCapitalButton_Click(object sender, RoutedEventArgs e)
        {
            TestCapitalUpdate();
        }

        // Pomocnicze metody do pozyskiwania danych o instrumentach
        private string GetTickerFromIsin(string isin)
        {
            // Tymczasowa logika
            switch (isin)
            {
                case "PLBZ00000044": return "SPL";
                case "PLZATRM00012": return "ATT";
                case "PLPEKAO00016": return "PEO";
                case "PLPZU0000011": return "PZU";
                case "PLPKO0000016": return "PKO";
                case "PLPGER000010": return "PGE";
                case "PLGPF0030688": return "FW20M2520";
                case "PLPKN0000018": return "PKN";
                case "PLKRK0000010": return "KRU";
                case "LU2337380790": return "ALE";
                default: return isin.Substring(Math.Max(0, isin.Length - 3));
            }
        }

        private string GetNameFromIsin(string isin)
        {
            // Tymczasowa logika
            switch (isin)
            {
                case "PLBZ00000044": return "SANPL";
                case "PLZATRM00012": return "GRUPAAZOTY";
                case "PLPEKAO00016": return "PEKAO";
                case "PLPZU0000011": return "PZU";
                case "PLPKO0000016": return "PKOBP";
                case "PLPGER000010": return "PGE";
                case "PLGPF0030688": return "FW20M2520";
                case "PLPKN0000018": return "PKNORLEN";
                case "PLKRK0000010": return "KRUK";
                case "LU2337380790": return "ALLEGRO";
                default: return "Unknown";
            }
        }

        private void UpdateCapitalDisplay()
        {
            try
            {
                // Dodajmy więcej logowania, aby zobaczyć, czy ta metoda jest wywoływana
                Console.WriteLine($"UpdateCapitalDisplay: Open={_currentCapital.OpenCapital}, Accrued={_currentCapital.AccruedCapital}, Total={_currentCapital.TotalCapital}");
                if (RawMessagesTextBox != null)
                {
                    RawMessagesTextBox.AppendText($"UPDATE UI: Open={_currentCapital.OpenCapital}, Accrued={_currentCapital.AccruedCapital}, Total={_currentCapital.TotalCapital}\n");
                }

                // Aktualizacja wyświetlanych wartości kapitału
                OpenCapitalTextBlock.Text = _currentCapital.OpenCapital.ToString("0.00");
                AccruedCapitalTextBlock.Text = _currentCapital.AccruedCapital.ToString("0.00");
                TotalCapitalTextBlock.Text = _currentCapital.TotalCapital.ToString("0.00");

                // Aktualizacja limitów
                MessagesPercentageTextBlock.Text = $"{_currentCapital.MessagesPercentage}%";
                MessagesLimitTextBlock.Text = _currentCapital.MessagesLimit.ToString();
                CapitalPercentageTextBlock.Text = $"{_currentCapital.CapitalPercentage}%";
                CapitalLimitTextBlock.Text = _currentCapital.CapitalLimit.ToString();

                // Ustawienie kolorów w zależności od procentów
                MessagesPercentageTextBlock.Foreground = GetBrushForPercentage(_currentCapital.MessagesPercentage);
                CapitalPercentageTextBlock.Foreground = GetBrushForPercentage(_currentCapital.CapitalPercentage);
            }
            catch (Exception ex)
            {
                // Logowanie błędów przy aktualizacji UI
                Console.WriteLine($"Błąd podczas aktualizacji UI: {ex.Message}");
                if (RawMessagesTextBox != null)
                {
                    RawMessagesTextBox.AppendText($"ERROR: Błąd podczas aktualizacji UI: {ex.Message}\n");
                }
            }
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

        private void OnConnectionStatusChanged(bool isConnected)
        {
            Dispatcher.Invoke(() =>
            {
                if (isConnected)
                {
                    ConnectionStatus.Text = "Połączony";
                    ConnectionStatus.Foreground = Brushes.Green;
                    ConnectButton.IsEnabled = false;
                    DisconnectButton.IsEnabled = true;
                    StatusBarText.Text = $"Połączony z {_serverIp}:{_serverPort}";
                }
                else
                {
                    ConnectionStatus.Text = "Rozłączony";
                    ConnectionStatus.Foreground = Brushes.Red;
                    ConnectButton.IsEnabled = true;
                    DisconnectButton.IsEnabled = false;
                    StatusBarText.Text = "Rozłączony";
                }
            });
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Odczytaj IP i port z pól tekstowych, jeśli są dostępne
                if (ServerIpTextBox != null && !string.IsNullOrEmpty(ServerIpTextBox.Text))
                    _serverIp = ServerIpTextBox.Text;

                if (ServerPortTextBox != null && !string.IsNullOrEmpty(ServerPortTextBox.Text))
                    _serverPort = int.Parse(ServerPortTextBox.Text);

                StatusBarText.Text = $"Łączenie z {_serverIp}:{_serverPort}...";
                await _rcClient.ConnectAsync(_serverIp, _serverPort);

                // Wyczyść bieżące dane przed pobieraniem nowych
                _positions.Clear();
                _positionsByIsin.Clear();

                StatusBarText.Text = "Pobieranie historycznych danych...";

                // Rewindowanie wiadomości - pobieranie wszystkich historycznych danych
                await _rcClient.SendRewindAsync(0);

                // Pobierz historię kontroli
                await LoadControlHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas łączenia: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusBarText.Text = "Błąd połączenia";
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            _rcClient.Disconnect();
        }

        private async void AllSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rcClient.IsConnected)
            {
                // Toggle halt dla wszystkich instrumentów
                await _rcClient.SendSetControlAsync("(ALL),halt,Y");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_rcClient.IsConnected)
            {
                _rcClient.Disconnect();
            }
        }

        private async Task LoadHistoricalDataAsync()
        {
            if (!_rcClient.IsConnected)
                return;

            StatusBarText.Text = "Pobieranie historycznych danych...";

            // Wyczyść dane przed pobieraniem
            _positions.Clear();
            _positionsByIsin.Clear();

            // Rewindowanie wiadomości - pobieranie wszystkich historycznych danych
            await _rcClient.SendRewindAsync(0);

            // Możemy dodać krótkie opóźnienie, aby dać czas na przetworzenie danych
            await Task.Delay(500);

            // Odśwież UI
            RefreshPositionsGrid();
        }





        // Obsługa zmiany typu zakresu dla szybkiej zmiany limitów
        private void QuickScopeTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Dostosuj pole zakresu w zależności od wybranego typu
            if (QuickScopeTypeComboBox.SelectedIndex == 0) // Wszystkie instrumenty
            {
                QuickScopeValueTextBox.Text = "(ALL)";
                QuickScopeValueTextBox.IsEnabled = false;
            }
            else if (QuickScopeTypeComboBox.SelectedIndex == 1) // Grupa instrumentów
            {
                QuickScopeValueTextBox.Text = "[11*]";
                QuickScopeValueTextBox.IsEnabled = true;
            }
            else if (QuickScopeTypeComboBox.SelectedIndex == 2) // Pojedynczy instrument
            {
                QuickScopeValueTextBox.Text = "PLPKO0000016";
                QuickScopeValueTextBox.IsEnabled = true;
            }
        }

        // Obsługa przycisku szybkiej zmiany limitu
        private async void ApplyQuickLimitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_rcClient.IsConnected)
            {
                MessageBox.Show("Nie jesteś połączony z serwerem!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Walidacja
            if (string.IsNullOrWhiteSpace(QuickScopeValueTextBox.Text))
            {
                MessageBox.Show("Podaj wartość zakresu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (QuickLimitTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Wybierz typ limitu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(QuickLimitValueTextBox.Text))
            {
                MessageBox.Show("Podaj wartość limitu", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Utworzenie ciągu kontrolnego
                string scope = QuickScopeValueTextBox.Text;
                string limitType = (QuickLimitTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                string limitValue = QuickLimitValueTextBox.Text;

                string controlString = $"{scope},{limitType},{limitValue}";

                // Wysłanie do serwera
                await _rcClient.SendSetControlAsync(controlString);

                StatusBarText.Text = $"Wysłano limit: {controlString}";
                RawMessagesTextBox.AppendText($"Wysłano limit: {controlString}\n");

                await LoadControlHistoryAsync();


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wysyłania limitu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Add this method to the MainWindow class in MainWindow.xaml.cs
        private void SaveLimitsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_controlLimits.Count == 0)
            {
                MessageBox.Show("Brak limitów do zapisania.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "csv",
                Title = "Zapisz limity do pliku"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    StatusBarText.Text = "Zapisywanie limitów...";
                    
                    // Sort limits according to requested order:
                    // 1. ALL limits first
                    // 2. Group limits next
                    // 3. Individual instrument limits last
                    var sortedLimits = _controlLimits.ToList();
                    
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
                        
                        // If same name, compare by scope value (for groups and ISINs)
                        return string.Compare(a.Scope, b.Scope, StringComparison.OrdinalIgnoreCase);
                    });
                    
                    // Write to file
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        // Write header
                        writer.WriteLine("Scope,Name,Value");
                        
                        // Write sorted limits
                        foreach (var limit in sortedLimits)
                        {
                            writer.WriteLine(limit.ToControlString());
                        }
                    }
                    
                    StatusBarText.Text = $"Zapisano {sortedLimits.Count} limitów do pliku {Path.GetFileName(saveFileDialog.FileName)}";
                    MessageBox.Show($"Zapisano {sortedLimits.Count} limitów do pliku.", "Zapisano", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas zapisywania limitów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusBarText.Text = "Błąd podczas zapisywania limitów";
                }
            }
        }        




        //==============================instruments=======================================

        private void SelectInstrumentsFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                Title = "Select Instruments CSV File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _instrumentsFilePath = openFileDialog.FileName;
                InstrumentsFilePathText.Text = Path.GetFileName(_instrumentsFilePath);
                LoadInstrumentsFromCsv(_instrumentsFilePath);
            }
        }

        private void LoadInstrumentsFromCsv(string filePath)
        {
            try
            {
                StatusBarText.Text = "Loading instruments file...";
                _instruments.Clear();
                
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);
                
                // Get headers (first line)
                if (lines.Length > 0)
                {
                    string[] headers = lines[0].Split(',');
                    
                    // Process data rows
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                string[] values = line.Split(',');
                                
                                if (values.Length >= 57) // Ensure we have all columns
                                {
                                    var instrument = new Instrument
                                    {
                                        InstrumentID = ParseInt(values[0]),
                                        ISIN = values[1],
                                        ProductCode = values[2],
                                        ReferencePrice = ParseDouble(values[3]),
                                        Multiplier = ParseDouble(values[4]),
                                        MIC = values[5],
                                        InstrumentTypeID = ParseInt(values[6]),
                                        InstrumentType = values[7],
                                        InstrumentSubtypeID = ParseInt(values[8]),
                                        InstrumentSubtype = values[9],
                                        MarketStructureID = ParseInt(values[10]),
                                        MarketStructureName = values[11],
                                        Currency = values[12],
                                        CollarGroupId = ParseInt(values[13]),
                                        OrderStaticCollarModeId = ParseInt(values[14]),
                                        OrderStaticCollarMode = values[15],
                                        OrderStaticCollarExpressionTypeId = ParseInt(values[16]),
                                        OrderStaticCollarExpressionType = values[17],
                                        OrderStaticCollarLowerBound = ParseDouble(values[18]),
                                        OrderStaticCollarValue = ParseDouble(values[19]),
                                        OrderStaticCollarLowerBid = ParseDouble(values[20]),
                                        OrderStaticCollarUpperBid = ParseDouble(values[21]),
                                        OrderStaticCollarUpperAsk = ParseDouble(values[22]),
                                        OrderStaticCollarLowerAsk = ParseDouble(values[23]),
                                        FirstTradingDate = ParseDate(values[24]),
                                        LastTradingDate = ParseDate(values[25]),
                                        ProductID = ParseInt(values[26]),
                                        LotSize = ParseInt(values[27]),
                                        PriceExpressionType = ParseInt(values[28]),
                                        CalendarID = ParseInt(values[29]),
                                        TickTableID = ParseInt(values[30]),
                                        TradingScheduleID = ParseInt(values[31]),
                                        NominalValueType = ParseInt(values[32]),
                                        StrikePrice = ParseDouble(values[33]),
                                        SettlementCalendarID = ParseInt(values[34]),
                                        BondCouponType = ParseInt(values[35]),
                                        Liquidity = ParseBool(values[36]),
                                        MarketModelTypeID = ParseInt(values[37]),
                                        MarketModelType = values[38],
                                        IssuerRegCountry = values[39],
                                        UnderlyingInstrumentID = ParseInt(values[40]),
                                        CFICode = values[41],
                                        IssueSize = ParseInt(values[42]),
                                        NominalCurrency = values[43],
                                        USIndicator = ParseInt(values[44]),
                                        ExpiryDate = ParseInt(values[45]),
                                        VersionNumber = ParseInt(values[46]),
                                        SettlementType = ParseInt(values[47]),
                                        OptionType = ParseInt(values[48]),
                                        ExerciseType = ParseInt(values[49]),
                                        ProductName = values[50],
                                        Status = ParseInt(values[51]),
                                        InitialPhaseID = ParseInt(values[52]),
                                        ThresholdMax = ParseDouble(values[53]),
                                        ThresholdMin = ParseDouble(values[54]),
                                        IsLeverage = ParseBool(values[55]),
                                        NominalValue = ParseDouble(values[56])
                                    };
                                    
                                    _instruments.Add(instrument);
                                }
                                else
                                {
                                    Console.WriteLine($"Warning: Line {i} has only {values.Length} values, expected 57");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                            }
                        }
                    }
                }
                
                StatusBarText.Text = $"Loaded {_instruments.Count} instruments from {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading instruments file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusBarText.Text = "Error loading instruments file";
            }
        }

        // Helper methods for parsing values
        private int ParseInt(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        private double ParseDouble(string value)
        {
            // Handle both culture-specific number formats
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;
            return 0;
        }

        private DateTime ParseDate(string value)
        {
            if (DateTime.TryParse(value, out DateTime result))
                return result;
            return DateTime.MinValue;
        }

        private bool ParseBool(string value)
        {
            if (bool.TryParse(value, out bool result))
                return result;
            
            // Handle "1"/"0" as bool
            if (value == "1")
                return true;
            if (value == "0") 
                return false;
                
            return false;
        }
    }
}