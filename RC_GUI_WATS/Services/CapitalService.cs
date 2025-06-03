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
        }

        private void ProcessMessage(RcMessage message)
        {
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
            // if ((message.Contains("%") || message.Contains("percent")) && message.Contains("of"))
            // {
            //     try
            //     {
            //         // Logic to extract percentage and limit values from log messages
            //         // (Simplified from original code)
            //         int percentIndex = message.IndexOf('%');
            //         if (percentIndex < 0)
            //         {
            //             percentIndex = message.IndexOf("percent");
            //             if (percentIndex < 0)
            //                 return;
            //         }
            //
            //         // Find the beginning of the number before %
            //         int i = percentIndex - 1;
            //         while (i >= 0 && (char.IsDigit(message[i]) || message[i] == '.'))
            //             i--;
            //
            //         string percentText = message.Substring(i + 1, percentIndex - i - 1).Trim();
            //         double percent = double.Parse(percentText);
            //
            //         // Find "of" and the number after it
            //         int ofIndex = message.IndexOf("of", percentIndex);
            //         if (ofIndex < 0)
            //             return;
            //
            //         // Find start of number after "of"
            //         i = ofIndex + 2;
            //         while (i < message.Length && char.IsWhiteSpace(message[i]))
            //             i++;
            //
            //         // Find end of number
            //         int j = i;
            //         while (j < message.Length && (char.IsDigit(message[j]) || message[j] == '.'))
            //             j++;
            //
            //         string limitText = message.Substring(i, j - i).Trim();
            //         double limit = double.Parse(limitText);
            //
            //         // Update limits on UI thread
            //         _dispatcher.Invoke(() =>
            //         {
            //             // Determine if it's about messages or capital
            //             if (message.Contains("message", StringComparison.OrdinalIgnoreCase) ||
            //                 message.Contains("order", StringComparison.OrdinalIgnoreCase))
            //             {
            //                 _currentCapital.MessagesPercentage = percent;
            //                 _currentCapital.MessagesLimit = limit;
            //             }
            //             else if (message.Contains("capital", StringComparison.OrdinalIgnoreCase) ||
            //                      message.Contains("position", StringComparison.OrdinalIgnoreCase))
            //             {
            //                 _currentCapital.CapitalPercentage = percent;
            //                 _currentCapital.CapitalLimit = limit;
            //             }
            //
            //             CapitalUpdated?.Invoke();
            //         });
            //     }
            //     catch
            //     {
            //         // Handle parsing errors
            //     }
            // }
            
            string[] parts = message.Split(',');
            if (parts.Length != 3) return;

            string scope = parts[0].Trim();            // np. "(ALL)"
            string controlName = parts[1].Trim();      // np. "maxCapital"
            string valueStr = parts[2].Trim();         // np. "5000"

            // Interesuje nas tylko nazwa "maxCapital" i zakres "(ALL)"
            if (scope.Equals("(ALL)", StringComparison.OrdinalIgnoreCase) &&
                controlName.Equals("maxCapital", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(valueStr, out double newLimit))
                {
                    // Ustawiamy nowy limit kapitału
                    _currentCapital.CapitalLimit = newLimit;

                    // Jeśli już mamy TotalCapital, przeliczmy procent
                    RecalculatePercentage();

                    // Powiadom UI o zmianie
                    CapitalUpdated?.Invoke();
                }
            }
        }
        private void RecalculatePercentage()
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
    }
}