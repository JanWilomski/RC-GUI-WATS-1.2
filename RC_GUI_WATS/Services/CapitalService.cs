// Services/CapitalService.cs
using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class CapitalService
    {
        private RcTcpClientService _clientService;
        private Capital _currentCapital = new Capital();
        private readonly Dispatcher _dispatcher;

        public Capital CurrentCapital => _currentCapital;

        public event Action CapitalUpdated;

        public CapitalService(RcTcpClientService clientService)
        {
            _clientService = clientService;
            _clientService.MessageReceived += ProcessMessage;
            _dispatcher = Application.Current.Dispatcher;

            // Initialize capital values
            _currentCapital.MessagesPercentage = 0;
            _currentCapital.MessagesLimit = 0;
            _currentCapital.CapitalPercentage = 0;
            _currentCapital.CapitalLimit = 0;
            _currentCapital.CurrentMessages = 0;
        }

        private void ProcessMessage(RcMessage message)
        {
            // Liczy wszystkie otrzymane wiadomości RC (nie tylko CCG)
            if (message.Header.SequenceNumber > 0) // Only business messages have sequence numbers > 0
            {
                IncrementMessageCount();
            }

            foreach (var block in message.Blocks)
            {
                if (block.Payload.Length > 0)
                {
                    char messageType = (char)block.Payload[0];
                    
                    if (messageType == 'C')
                    {
                        ProcessCapitalMessage(block.Payload);
                    }
                    else if (messageType == 'I' || messageType == 'W' || 
                             messageType == 'D' || messageType == 'E'||messageType == 'G'||messageType == 'S')
                    {
                        // Process log messages for potential limit information
                        if (block.Payload.Length >= 3)
                        {
                            ushort msgLength = BitConverter.ToUInt16(block.Payload, 1);
                            if (block.Payload.Length >= 3 + msgLength)
                            {
                                string msg = Encoding.ASCII.GetString(block.Payload, 3, msgLength);
                                TryExtractLimitsFromLog(msg);
                            }
                        }
                    }
                }
            }
        }

        private void ProcessCapitalMessage(byte[] payload)
        {
            if (payload.Length < 25)
                return;

            try
            {
                double openCapital = BitConverter.ToDouble(payload, 1);
                double accruedCapital = BitConverter.ToDouble(payload, 9);
                double totalCapital = BitConverter.ToDouble(payload, 17);

                bool validValues = !double.IsNaN(openCapital) && !double.IsInfinity(openCapital) &&
                                  !double.IsNaN(accruedCapital) && !double.IsInfinity(accruedCapital) &&
                                  !double.IsNaN(totalCapital) && !double.IsInfinity(totalCapital);

                if (validValues)
                {
                    // Update capital values and notify on UI thread
                    _dispatcher.Invoke(() =>
                    {
                        _currentCapital.OpenCapital = openCapital;
                        _currentCapital.AccruedCapital = accruedCapital;
                        _currentCapital.TotalCapital = totalCapital;
                        
                        // Recalculate percentage when total capital changes
                        RecalculateCapitalPercentage();
                        
                        CapitalUpdated?.Invoke();
                    });
                }
            }
            catch (Exception)
            {
                // Error handling
            }
        }

        private void TryExtractLimitsFromLog(string message)
        {
            Console.WriteLine($"Processing message: '{message}'");
            
            string[] parts = message.Split(',');
            if (parts.Length != 3) 
            {
                Console.WriteLine($"Message has {parts.Length} parts, expected 3");
                return;
            }

            string scope = parts[0].Trim();            // np. "(ALL)"
            string controlName = parts[1].Trim();      // np. "maxCapital" lub "maxMessageCount"
            string valueStr = parts[2].Trim();         // np. "1100000." lub "115200"

            Console.WriteLine($"Parsed - Scope: '{scope}', Control: '{controlName}', Value: '{valueStr}'");

            // Usuń dodatkowe znaki z końca wartości (kropki, spacje, itp.)
            valueStr = CleanValueString(valueStr);
            Console.WriteLine($"Cleaned value: '{valueStr}'");

            // Sprawdź czy to limit dla wszystkich instrumentów
            if (!scope.Equals("(ALL)", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Scope '{scope}' is not (ALL), skipping");
                return;
            }

            _dispatcher.Invoke(() =>
            {
                if (controlName.Equals("maxCapital", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(valueStr, out double newCapitalLimit))
                    {
                        Console.WriteLine($"Setting Capital Limit from {_currentCapital.CapitalLimit} to {newCapitalLimit}");
                        _currentCapital.CapitalLimit = newCapitalLimit;
                        RecalculateCapitalPercentage();
                        CapitalUpdated?.Invoke();
                        
                        Console.WriteLine($"Updated Capital Limit: {newCapitalLimit}, Percentage: {_currentCapital.CapitalPercentage}%");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to parse capital limit: '{valueStr}'");
                    }
                }
                else if (controlName.Equals("maxMessageCount", StringComparison.OrdinalIgnoreCase))
                {
                    if (double.TryParse(valueStr, out double newMessagesLimit))
                    {
                        Console.WriteLine($"Setting Messages Limit from {_currentCapital.MessagesLimit} to {newMessagesLimit}");
                        _currentCapital.MessagesLimit = newMessagesLimit;
                        RecalculateMessagesPercentage();
                        CapitalUpdated?.Invoke();
                        
                        Console.WriteLine($"Updated Messages Limit: {newMessagesLimit}, Percentage: {_currentCapital.MessagesPercentage}%");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to parse messages limit: '{valueStr}'");
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown control name: '{controlName}'");
                }
            });
        }

        private string CleanValueString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Usuń białe znaki z początku i końca
            value = value.Trim();

            // Usuń dodatkowe znaki z końca (kropki, średniki, etc.)
            while (value.Length > 0 && !char.IsDigit(value[value.Length - 1]))
            {
                value = value.Substring(0, value.Length - 1);
            }

            return value;
        }

        private void RecalculateCapitalPercentage()
        {
            double limit = _currentCapital.CapitalLimit;
            double total = _currentCapital.TotalCapital;

            if (limit > 0)
            {
                _currentCapital.CapitalPercentage = Math.Round(total / limit * 100.0, 2);
            }
            else
            {
                _currentCapital.CapitalPercentage = 0;
            }
        }

        private void RecalculateMessagesPercentage()
        {
            double limit = _currentCapital.MessagesLimit;
            double current = _currentCapital.CurrentMessages;

            if (limit > 0)
            {
                _currentCapital.MessagesPercentage = Math.Round(current / limit * 100.0, 2);
            }
            else
            {
                _currentCapital.MessagesPercentage = 0;
            }
        }

        // Metoda do aktualizacji aktualnej liczby wiadomości
        public void UpdateCurrentMessages(double currentMessages)
        {
            _dispatcher.Invoke(() =>
            {
                _currentCapital.CurrentMessages = currentMessages;
                RecalculateMessagesPercentage();
                CapitalUpdated?.Invoke();
            });
        }

        // Metoda do inkrementacji liczby wiadomości
        public void IncrementMessageCount()
        {
            _dispatcher.Invoke(() =>
            {
                _currentCapital.CurrentMessages++;
                RecalculateMessagesPercentage();
                CapitalUpdated?.Invoke();
            });
        }

        // Metoda do resetowania liczników (np. przy rozłączeniu)
        public void ResetCounters()
        {
            _dispatcher.Invoke(() =>
            {
                _currentCapital.CurrentMessages = 0;
                _currentCapital.MessagesPercentage = 0;
                _currentCapital.CapitalPercentage = 0;
                CapitalUpdated?.Invoke();
            });
        }
    }
}