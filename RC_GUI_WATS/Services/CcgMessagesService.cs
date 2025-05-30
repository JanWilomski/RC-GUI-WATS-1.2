// Services/CcgMessagesService.cs
using System;
using System.Collections.ObjectModel;
using System.Linq;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class CcgMessagesService
    {
        private RcTcpClientService _clientService;
        private InstrumentsService _instrumentsService;
        private ObservableCollection<CcgMessage> _ccgMessages = new ObservableCollection<CcgMessage>();
        private const int MAX_MESSAGES = 1000; // Limit to prevent memory issues
        public ObservableCollection<CcgMessage> CcgMessages => _ccgMessages;

        public event Action<CcgMessage> NewCcgMessageReceived;
        public event Action MessagesCleared;

        public CcgMessagesService(RcTcpClientService clientService, InstrumentsService instrumentsService)
        {
            _clientService = clientService;
            _instrumentsService = instrumentsService;
            _clientService.MessageReceived += ProcessMessage;

            // Subscribe to instruments updates to re-map existing messages
            _instrumentsService.StatusUpdated += OnInstrumentsUpdated;
        }

        private void OnInstrumentsUpdated(string status)
        {
            // Re-map all existing messages when instruments are loaded/updated
            if (status.Contains("Loaded") && _ccgMessages.Count > 0)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    foreach (var message in _ccgMessages)
                    {
                        MapInstrumentData(message);
                    }
                });
            }
        }

        private void ProcessMessage(RcMessage message)
        {
            foreach (var block in message.Blocks)
            {
                if (block.Payload.Length > 0 && block.Payload[0] == (byte)'B')
                {
                    ProcessCcgMessage(block.Payload);
                }
            }
        }

        private void ProcessCcgMessage(byte[] payload)
        {
            if (payload.Length < 3)
                return;

            try
            {
                // Extract CCG message from RC message payload
                // Format: 'B' + uint16 length + CCG binary data
                ushort ccgLength = BitConverter.ToUInt16(payload, 1);

                if (payload.Length < 3 + ccgLength)
                    return;

                // Extract the actual CCG binary message
                byte[] ccgData = new byte[ccgLength];
                Array.Copy(payload, 3, ccgData, 0, ccgLength);

                // DEBUG: Log first few messages to see what we're getting
                if (_ccgMessages.Count < 10)
                {
                    string hexDump = BitConverter.ToString(ccgData.Take(Math.Min(ccgData.Length, 20)).ToArray());
                    System.Diagnostics.Debug.WriteLine(
                        $"CCG Message #{_ccgMessages.Count}: Length={ccgLength}, First 20 bytes: {hexDump}");

                    if (ccgData.Length >= 4)
                    {
                        ushort length = BitConverter.ToUInt16(ccgData, 0);
                        ushort msgType = BitConverter.ToUInt16(ccgData, 2);
                        System.Diagnostics.Debug.WriteLine($"  Parsed Length: {length}, MsgType: {msgType}");
                    }
                }

                // Check if this is a heartbeat message (skip them)
                if (ccgData.Length >= 4)
                {
                    ushort msgType = BitConverter.ToUInt16(ccgData, 2);
                    if (msgType == (ushort)CcgMessageType.Heartbeat)
                    {
                        // Skip heartbeat messages - they're handled by HeartbeatMonitorService
                        return;
                    }

                    // Skip Login messages if there are too many (probably parsing error)
                    if (msgType == (ushort)CcgMessageType.Login || msgType == (ushort)CcgMessageType.LoginResponse)
                    {
                        var loginCount = _ccgMessages.Count(m => m.Name == "Login" || m.Name == "LoginResponse");
                        if (loginCount > 5) // Allow only first 5 login messages
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping Login message (already have {loginCount})");
                            return;
                        }
                    }
                }

                // Parse the CCG message
                var ccgMessage = CcgMessageParser.ParseCcgMessage(ccgData, DateTime.Now);

                if (ccgMessage != null)
                {
                    // Map instrument data (ISIN and ProductCode)
                    MapInstrumentData(ccgMessage);

                    // Add to collection (on UI thread)
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        // Maintain size limit
                        while (_ccgMessages.Count >= MAX_MESSAGES)
                        {
                            _ccgMessages.RemoveAt(0);
                        }

                        // Insert at beginning for newest-first display
                        _ccgMessages.Insert(0, ccgMessage);
                    });

                    // Notify subscribers
                    NewCcgMessageReceived?.Invoke(ccgMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing CCG message: {ex.Message}");
            }
        }

        private void MapInstrumentData(CcgMessage ccgMessage)
        {
            if (ccgMessage.InstrumentId.HasValue && _instrumentsService.Instruments.Count > 0)
            {
                try
                {
                    // Find instrument by InstrumentID
                    var instrument = _instrumentsService.Instruments.FirstOrDefault(
                        i => i.InstrumentID == ccgMessage.InstrumentId.Value);

                    if (instrument != null)
                    {
                        ccgMessage.ISIN = instrument.ISIN;
                        ccgMessage.ProductCode = instrument.ProductCode;

                        System.Diagnostics.Debug.WriteLine(
                            $"Mapped InstrumentID {ccgMessage.InstrumentId} -> ISIN: {instrument.ISIN}, ProductCode: {instrument.ProductCode}");
                    }
                    else
                    {
                        // Clear mapping if instrument not found
                        ccgMessage.ISIN = "";
                        ccgMessage.ProductCode = "";

                        System.Diagnostics.Debug.WriteLine(
                            $"InstrumentID {ccgMessage.InstrumentId} not found in instruments collection");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error mapping instrument data: {ex.Message}");
                    ccgMessage.ISIN = "";
                    ccgMessage.ProductCode = "";
                }
            }
            else
            {
                // No InstrumentId or no instruments loaded
                ccgMessage.ISIN = "";
                ccgMessage.ProductCode = "";
            }
        }

        public void ClearMessages()
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => { _ccgMessages.Clear(); });
            MessagesCleared?.Invoke();
        }

        public ObservableCollection<CcgMessage> GetFilteredMessages(
            string messageTypeFilter = null,
            string sideFilter = null,
            uint? instrumentIdFilter = null,
            DateTime? fromTime = null,
            DateTime? toTime = null)
        {
            var filtered = new ObservableCollection<CcgMessage>();

            foreach (var msg in _ccgMessages)
            {
                bool include = true;

                // Message type filter
                if (!string.IsNullOrEmpty(messageTypeFilter) &&
                    !msg.Name.Contains(messageTypeFilter, StringComparison.OrdinalIgnoreCase))
                {
                    include = false;
                }

                // Side filter
                if (!string.IsNullOrEmpty(sideFilter) &&
                    !string.Equals(msg.Side, sideFilter, StringComparison.OrdinalIgnoreCase))
                {
                    include = false;
                }

                // Instrument ID filter
                if (instrumentIdFilter.HasValue && msg.InstrumentId != instrumentIdFilter.Value)
                {
                    include = false;
                }

                // Time range filter
                if (fromTime.HasValue && msg.DateReceived < fromTime.Value)
                {
                    include = false;
                }

                if (toTime.HasValue && msg.DateReceived > toTime.Value)
                {
                    include = false;
                }

                if (include)
                {
                    filtered.Add(msg);
                }
            }

            return filtered;
        }

        public int GetMessageCount() => _ccgMessages.Count;

        public int GetMessageCountByType(string messageType)
        {
            return _ccgMessages.Count(m =>
                string.Equals(m.Name, messageType, StringComparison.OrdinalIgnoreCase));
        }

        public CcgMessage GetLatestMessageByType(string messageType)
        {
            return _ccgMessages.FirstOrDefault(m =>
                string.Equals(m.Name, messageType, StringComparison.OrdinalIgnoreCase));
        }

        // Statistics methods
        public (int Orders, int Trades, int Cancels, int Quotes, int Others) GetMessageStatistics()
        {
            int orders = 0, trades = 0, cancels = 0, quotes = 0, others = 0;

            foreach (var msg in _ccgMessages)
            {
                switch (msg.Name.ToLower())
                {
                    case "orderadd":
                    case "orderaddresponse":
                    case "ordermodify":
                    case "ordermodifyresponse":
                        orders++;
                        break;
                    case "trade":
                    case "tradecapturereportsingle":
                    case "tradecapturereportdual":
                        trades++;
                        break;
                    case "ordercancel":
                    case "ordercancelresponse":
                    case "ordermasscancel":
                    case "ordermasscancelresponse":
                        cancels++;
                        break;
                    case "massquote":
                    case "massquoteresponse":
                        quotes++;
                        break;
                    default:
                        others++;
                        break;
                }
            }

            return (orders, trades, cancels, quotes, others);
        }

        // Method to get instrument mapping statistics
        public (int WithMapping, int WithoutMapping, int Total) GetInstrumentMappingStatistics()
        {
            int withMapping = 0;
            int withoutMapping = 0;
            int total = 0;

            foreach (var msg in _ccgMessages)
            {
                if (msg.InstrumentId.HasValue)
                {
                    total++;
                    if (!string.IsNullOrEmpty(msg.ISIN))
                    {
                        withMapping++;
                    }
                    else
                    {
                        withoutMapping++;
                    }
                }
            }

            return (withMapping, withoutMapping, total);
        }
    }
}