using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class LimitsService
    {
        private RcTcpClientService _clientService;
        private ObservableCollection<ControlLimit> _controlLimits = new ObservableCollection<ControlLimit>();

        public ObservableCollection<ControlLimit> ControlLimits => _controlLimits;

        public LimitsService(RcTcpClientService clientService)
        {
            _clientService = clientService;
            _clientService.MessageReceived += ProcessMessage;
        }

        private void ProcessMessage(RcMessage message)
        {
            foreach (var block in message.Blocks)
            {
                if (block.Payload.Length > 0)
                {
                    char messageType = (char)block.Payload[0];
                    
                    // Process Set Control messages
                    if (messageType == 'S')
                    {
                        if (block.Payload.Length >= 3)
                        {
                            ushort length = BitConverter.ToUInt16(block.Payload, 1);
                            if (block.Payload.Length >= 3 + length)
                            {
                                string controlString = Encoding.ASCII.GetString(block.Payload, 3, length);
                                UpdateControlLimit(controlString);
                            }
                        }
                    }
                    // Process Info logs that might contain control information
                    else if (messageType == 'I')
                    {
                        if (block.Payload.Length >= 3)
                        {
                            ushort msgLength = BitConverter.ToUInt16(block.Payload, 1);
                            if (block.Payload.Length >= 3 + msgLength)
                            {
                                string msg = Encoding.ASCII.GetString(block.Payload, 3, msgLength);
                                
                                // Check if this info message contains control information
                                if (msg.Contains(','))
                                {
                                    try
                                    {
                                        if (msg.Contains("(ALL)") || msg.Contains("[") || 
                                            Regex.IsMatch(msg, @"^[A-Z0-9]{12}"))
                                        {
                                            UpdateControlLimit(msg);
                                        }
                                    }
                                    catch
                                    {
                                        // Ignore parsing errors
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateControlLimit(string controlString)
        {
            var limit = ControlLimit.FromControlString(controlString);
            if (limit == null)
                return;

            // Check if such limit already exists
            var existingLimit = _controlLimits.FirstOrDefault(l =>
                l.Scope == limit.Scope && l.Name == limit.Name);

            if (existingLimit != null)
            {
                // Update existing limit
                existingLimit.Value = limit.Value;
            }
            else
            {
                // Add new limit
                _controlLimits.Insert(0, limit);
            }
        }

        public async Task LoadControlHistoryAsync()
        {
            if (!_clientService.IsConnected)
                return;

            // Clear existing data
            _controlLimits.Clear();

            // Send request for control history
            await _clientService.SendGetControlsHistoryAsync();

            // Updates will be handled by the message processor
        }

        public async Task SendControlLimitAsync(ControlLimit limit)
        {
            if (_clientService.IsConnected)
            {
                string controlString = limit.ToControlString();
                await _clientService.SendSetControlAsync(controlString);
            }
            else
            {
                throw new InvalidOperationException("Not connected to server");
            }
        }
    }
}