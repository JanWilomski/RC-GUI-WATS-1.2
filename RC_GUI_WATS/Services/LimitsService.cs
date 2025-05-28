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
            if (message.Contains("(") && message.Contains(")")&&message!="(ALL)")
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
                // Add new limit with proper sorting
                InsertLimitInSortedOrder(limit);
            }
        }

        private void InsertLimitInSortedOrder(ControlLimit newLimit)
        {
            // Find the correct position to insert the new limit
            // Sort order: (ALL) first, then instrument types, then groups, then individual ISINs
            
            int insertIndex = 0;
            var newScopeType = newLimit.GetScopeType();
            
            for (int i = 0; i < _controlLimits.Count; i++)
            {
                var existingScopeType = _controlLimits[i].GetScopeType();
                
                // Compare scope types first
                if ((int)newScopeType < (int)existingScopeType)
                {
                    insertIndex = i;
                    break;
                }
                else if ((int)newScopeType == (int)existingScopeType)
                {
                    // Same scope type, compare by name then by scope value
                    int nameComparison = string.Compare(newLimit.Name, _controlLimits[i].Name, StringComparison.OrdinalIgnoreCase);
                    if (nameComparison < 0)
                    {
                        insertIndex = i;
                        break;
                    }
                    else if (nameComparison == 0)
                    {
                        // Same name, compare by scope value
                        if (string.Compare(newLimit.Scope, _controlLimits[i].Scope, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            insertIndex = i;
                            break;
                        }
                    }
                }
                
                insertIndex = i + 1;
            }
            
            _controlLimits.Insert(insertIndex, newLimit);
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
    }
}