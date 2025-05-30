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
                                        if (IsValidControlScope(msg))
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

        private bool IsValidControlScope(string message)
        {
            // Check for different scope types:
            // 1. (ALL) - all instruments
            // 2. (INSTRUMENT_TYPE) - instrument types in parentheses (but not (ALL))
            // 3. [pattern] - instrument groups in square brackets
            // 4. ISIN codes - typically 12 characters starting with letters
            
            if (message.Contains("(ALL)"))
                return true;
                
            // Check for instrument types in parentheses (excluding (ALL))
            if (message.Contains("(") && message.Contains(")") && message != "(ALL)")
                return true;
                
            // Check for instrument groups in square brackets
            if (message.Contains("[") && message.Contains("]"))
                return true;
                
            // Check for ISIN codes (12 characters, starting with 2 letters followed by alphanumeric)
            if (Regex.IsMatch(message, @"^[A-Z]{2}[A-Z0-9]{10}"))
                return true;
                
            return false;
        }

        public void UpdateControlLimit(string controlString)
        {
            var limit = ControlLimit.FromControlString(controlString);
            if (limit == null)
                return;

            // Set received time to current time (when we got confirmation from server)
            limit.ReceivedTime = DateTime.Now;

            // Check if such limit already exists
            var existingLimit = _controlLimits.FirstOrDefault(l =>
                l.Scope == limit.Scope && l.Name == limit.Name);

            if (existingLimit != null)
            {
                // Update existing limit value and received time
                existingLimit.Value = limit.Value;
                existingLimit.ReceivedTime = limit.ReceivedTime;
            }
            else
            {
                // Add new limit to the end of collection (chronological order)
                // This happens only when we receive confirmation from server
                _controlLimits.Add(limit);
            }
        }

        public async Task LoadControlHistoryAsync()
        {
            if (!_clientService.IsConnected)
                return;

            // Clear existing data
            _controlLimits.Clear();

            // Send request for control history - Get Controls History (G)
            await _clientService.SendGetControlsHistoryAsync();

            // Updates will be handled by the message processor when server responds
            // Server will send sequence of 'I' messages with control strings
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

        public int GetLimitCountByType(ScopeType scopeType)
        {
            return _controlLimits.Count(l => l.GetScopeType() == scopeType);
        }

        public string GetLimitsSummary()
        {
            var allCount = GetLimitCountByType(ScopeType.AllInstruments);
            var typeCount = GetLimitCountByType(ScopeType.InstrumentType);
            var groupCount = GetLimitCountByType(ScopeType.InstrumentGroup);
            var isinCount = GetLimitCountByType(ScopeType.SingleInstrument);
            
            return $"Limity: ALL({allCount}), Typy({typeCount}), Grupy({groupCount}), ISIN({isinCount})";
        }

        // Helper methods for getting limits in different orders
        public System.Collections.Generic.List<ControlLimit> GetLimitsChronological()
        {
            return _controlLimits.OrderBy(l => l.ReceivedTime).ToList();
        }

        public System.Collections.Generic.List<ControlLimit> GetLimitsHierarchical()
        {
            var sortedLimits = _controlLimits.ToList();
            
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
            
            return sortedLimits;
        }
    }
}