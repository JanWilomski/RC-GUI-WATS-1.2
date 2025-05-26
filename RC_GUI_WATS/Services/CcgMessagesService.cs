// Services/CcgMessagesService.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class CcgMessagesService
    {
        private RcTcpClientService _clientService;
        private ObservableCollection<CcgMessage> _ccgMessages = new ObservableCollection<CcgMessage>();
        private readonly Dispatcher _dispatcher;
        
        // Track rewind status
        private bool _isRewindInProgress = false;
        private uint _lastProcessedSequenceNumber = 0;
        
        public ObservableCollection<CcgMessage> CcgMessages => _ccgMessages;
        
        // Events
        public event Action<int> HistoricalMessagesLoaded;
        public event Action RewindCompleted;
        
        public bool IsRewindInProgress => _isRewindInProgress;
        public uint LastProcessedSequenceNumber => _lastProcessedSequenceNumber;
        
        public CcgMessagesService(RcTcpClientService clientService)
        {
            _clientService = clientService;
            _clientService.MessageReceived += ProcessMessage;
            _dispatcher = Application.Current.Dispatcher;
        }
        
        private void ProcessMessage(RcMessage message)
        {
            foreach (var block in message.Blocks)
            {
                if (block.Payload.Length > 0)
                {
                    char messageType = (char)block.Payload[0];
                    
                    // Check for rewind complete message ('r')
                    if (messageType == 'r')
                    {
                        OnRewindCompleted();
                        continue;
                    }
                    
                    // Process CCG messages (type 'B')
                    if (messageType == 'B')
                    {
                        ProcessCcgMessage(block.Payload, message.Header.SequenceNumber);
                    }
                }
            }
        }
        
        private void ProcessCcgMessage(byte[] payload, uint rcSequenceNumber)
        {
            if (payload.Length < 3)
                return;
                
            try
            {
                // Extract length of CCG message
                ushort ccgMessageLength = BitConverter.ToUInt16(payload, 1);
                
                if (payload.Length < 3 + ccgMessageLength)
                    return;
                    
                // Extract CCG message bytes
                byte[] ccgMessageData = new byte[ccgMessageLength];
                Array.Copy(payload, 3, ccgMessageData, 0, ccgMessageLength);
                
                var ccgMessage = ParseCcgMessage(ccgMessageData);
                if (ccgMessage != null)
                {
                    // Skip only heartbeat messages (msgType = 13)
                    if (IsHeartbeatMessage(ccgMessage))
                        return;
                    
                    // Mark if this is historical data (during rewind)
                    ccgMessage.IsHistorical = _isRewindInProgress;
                    ccgMessage.RcSequenceNumber = rcSequenceNumber;
                    
                    // Track sequence numbers
                    if (rcSequenceNumber > _lastProcessedSequenceNumber)
                        _lastProcessedSequenceNumber = rcSequenceNumber;
                    
                    // Update ObservableCollection on UI thread
                    _dispatcher.Invoke(() =>
                    {
                        // Add to collection (limit to last 2000 messages for performance)
                        if (_ccgMessages.Count >= 2000)
                        {
                            _ccgMessages.RemoveAt(_ccgMessages.Count - 1);
                        }
                        
                        // Insert at beginning for chronological order (newest first)
                        if (_isRewindInProgress)
                        {
                            // During rewind, add at the end to maintain chronological order
                            _ccgMessages.Add(ccgMessage);
                        }
                        else
                        {
                            // Real-time messages go to the top
                            _ccgMessages.Insert(0, ccgMessage);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash
                Console.WriteLine($"Error parsing CCG message: {ex.Message}");
            }
        }
        
        private void OnRewindCompleted()
        {
            Console.WriteLine($"Rewind completed. Loaded {_ccgMessages.Count} historical CCG messages.");
            
            _isRewindInProgress = false;
            
            // Update UI on UI thread
            _dispatcher.Invoke(() =>
            {
                // Reverse the order of historical messages so newest are first
                var historicalMessages = new List<CcgMessage>(_ccgMessages);
                _ccgMessages.Clear();
                
                // Add messages in reverse order (newest first)
                for (int i = historicalMessages.Count - 1; i >= 0; i--)
                {
                    _ccgMessages.Add(historicalMessages[i]);
                }
            });
            
            // Notify subscribers
            HistoricalMessagesLoaded?.Invoke(_ccgMessages.Count);
            RewindCompleted?.Invoke();
        }
        
        public void StartRewind()
        {
            _isRewindInProgress = true;
            Console.WriteLine("Starting rewind operation...");
        }
        
        private bool IsHeartbeatMessage(CcgMessage ccgMessage)
        {
            // Based on documentation:
            // 1. Heartbeat messages have msgType = 13
            // 2. "A message consisting of just a header is a heartbeat"
            
            if (ccgMessage.Name == "Heartbeat")
                return true;
                
            // Additional check: if message has only header data (16 bytes) and no significant payload
            if (ccgMessage.RawData.Length == 16)
                return true;
                
            return false;
        }
        
        private CcgMessage ParseCcgMessage(byte[] data)
        {
            // Handle messages of any length, minimum header is 16 bytes
            if (data.Length < 16)
            {
                // Very short messages - still try to parse what we can
                var shortMessage = new CcgMessage
                {
                    DateReceived = DateTime.Now,
                    RawData = data,
                    RawMessage = BitConverter.ToString(data),
                    Header = $"Short message ({data.Length} bytes)",
                    Name = "Unknown",
                    TransactTime = DateTime.Now,
                    IsHistorical = _isRewindInProgress,
                    RcSequenceNumber = 0
                };
                return shortMessage;
            }
                
            try
            {
                var ccgMessage = new CcgMessage
                {
                    DateReceived = DateTime.Now,
                    RawData = data,
                    RawMessage = BitConverter.ToString(data),
                    IsHistorical = _isRewindInProgress,
                    RcSequenceNumber = 0
                };
                
                // Parse GPW WATS binary message header (based on documentation)
                // Header structure: length(2) + msgType(2) + seqNum(4) + timestamp(8)
                
                ushort messageLength = BitConverter.ToUInt16(data, 0);
                ushort msgType = BitConverter.ToUInt16(data, 2);
                uint seqNum = BitConverter.ToUInt32(data, 4);
                ulong timestamp = BitConverter.ToUInt64(data, 8);
                
                ccgMessage.MsgSeqNum = seqNum;
                ccgMessage.Header = $"Len:{messageLength} Type:{msgType} Seq:{seqNum}";
                ccgMessage.Name = GetMessageTypeName(msgType);
                
                // Convert timestamp to DateTime (assuming nanoseconds since epoch)
                try
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    ccgMessage.TransactTime = epoch.AddTicks((long)(timestamp / 100)); // Convert nanoseconds to ticks
                }
                catch
                {
                    ccgMessage.TransactTime = DateTime.Now;
                }
                
                // Parse message-specific fields based on message type
                ParseMessageSpecificFields(ccgMessage, msgType, data);
                
                return ccgMessage;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing CCG message details: {ex.Message}");
                
                // Return a basic message with raw data if parsing fails
                return new CcgMessage
                {
                    DateReceived = DateTime.Now,
                    RawData = data,
                    RawMessage = BitConverter.ToString(data),
                    Header = $"Parse error ({data.Length} bytes)",
                    Name = "ParseError",
                    TransactTime = DateTime.Now,
                    IsHistorical = _isRewindInProgress,
                    RcSequenceNumber = 0
                };
            }
        }
        
        private void ParseMessageSpecificFields(CcgMessage ccgMessage, ushort msgType, byte[] data)
        {
            try
            {
                switch (msgType)
                {
                    case 2: // Login
                        ParseLogin(ccgMessage, data);
                        break;
                    case 3: // LoginResponse
                        ParseLoginResponse(ccgMessage, data);
                        break;
                    case 4: // OrderAdd
                        ParseOrderAdd(ccgMessage, data);
                        break;
                    case 5: // OrderAddResponse  
                        ParseOrderAddResponse(ccgMessage, data);
                        break;
                    case 6: // OrderCancel
                        ParseOrderCancel(ccgMessage, data);
                        break;
                    case 7: // OrderCancelResponse
                        ParseOrderCancelResponse(ccgMessage, data);
                        break;
                    case 8: // OrderModify
                        ParseOrderModify(ccgMessage, data);
                        break;
                    case 9: // OrderModifyResponse
                        ParseOrderModifyResponse(ccgMessage, data);
                        break;
                    case 10: // Trade
                        ParseTrade(ccgMessage, data);
                        break;
                    case 11: // Logout
                        ParseLogout(ccgMessage, data);
                        break;
                    case 12: // ConnectionClose
                        ParseConnectionClose(ccgMessage, data);
                        break;
                    case 13: // Heartbeat - should be filtered out but just in case
                        ParseHeartbeat(ccgMessage, data);
                        break;
                    case 14: // LogoutResponse
                        ParseLogoutResponse(ccgMessage, data);
                        break;
                    case 15: // Reject
                        ParseReject(ccgMessage, data);
                        break;
                    case 18: // TradeCaptureReportSingle
                        ParseTradeCaptureReportSingle(ccgMessage, data);
                        break;
                    case 19: // TradeCaptureReportDual
                        ParseTradeCaptureReportDual(ccgMessage, data);
                        break;
                    case 20: // TradeCaptureReportResponse
                        ParseTradeCaptureReportResponse(ccgMessage, data);
                        break;
                    case 23: // TradeBust
                        ParseTradeBust(ccgMessage, data);
                        break;
                    case 24: // MassQuote
                        ParseMassQuote(ccgMessage, data);
                        break;
                    case 25: // MassQuoteResponse
                        ParseMassQuoteResponse(ccgMessage, data);
                        break;
                    case 28: // RequestForExecution
                        ParseRequestForExecution(ccgMessage, data);
                        break;
                    case 29: // OrderMassCancel
                        ParseOrderMassCancel(ccgMessage, data);
                        break;
                    case 30: // OrderMassCancelResponse
                        ParseOrderMassCancelResponse(ccgMessage, data);
                        break;
                    case 31: // BidOfferUpdate
                        ParseBidOfferUpdate(ccgMessage, data);
                        break;
                    case 32: // MarketMakerCommand
                        ParseMarketMakerCommand(ccgMessage, data);
                        break;
                    case 33: // MarketMakerCommandResponse
                        ParseMarketMakerCommandResponse(ccgMessage, data);
                        break;
                    case 34: // GapFill
                        ParseGapFill(ccgMessage, data);
                        break;
                    default:
                        // For unknown message types, try to extract basic information
                        ParseGenericMessage(ccgMessage, data);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing message type {msgType}: {ex.Message}");
                // Set basic info even if parsing fails
                ccgMessage.Symbol = $"ParseError-{msgType}";
            }
        }
        
        private string GetMessageTypeName(ushort msgType)
        {
            // Based on GPW WATS documentation message types
            switch (msgType)
            {
                case 2: return "Login";
                case 3: return "LoginResponse";
                case 4: return "OrderAdd";
                case 5: return "OrderAddResponse";
                case 6: return "OrderCancel";
                case 7: return "OrderCancelResponse";
                case 8: return "OrderModify";
                case 9: return "OrderModifyResponse";
                case 10: return "Trade";
                case 11: return "Logout";
                case 12: return "ConnectionClose";
                case 13: return "Heartbeat";
                case 14: return "LogoutResponse";
                case 15: return "Reject";
                case 18: return "TradeCaptureReportSingle";
                case 19: return "TradeCaptureReportDual";
                case 20: return "TradeCaptureReportResponse";
                case 23: return "TradeBust";
                case 24: return "MassQuote";
                case 25: return "MassQuoteResponse";
                case 28: return "RequestForExecution";
                case 29: return "OrderMassCancel";
                case 30: return "OrderMassCancelResponse";
                case 31: return "BidOfferUpdate";
                case 32: return "MarketMakerCommand";
                case 33: return "MarketMakerCommandResponse";
                case 34: return "GapFill";
                default: return $"Unknown({msgType})";
            }
        }
        
        public void Clear()
        {
            _dispatcher.Invoke(() =>
            {
                _ccgMessages.Clear();
                _lastProcessedSequenceNumber = 0;
            });
        }
        
        // Placeholder methods for all the parsing functions
        private void ParseLogin(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "LOGIN"; }
        private void ParseLoginResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "LOGIN_RESP"; }
        private void ParseLogout(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "LOGOUT"; }
        private void ParseLogoutResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "LOGOUT_RESP"; }
        private void ParseConnectionClose(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "CONN_CLOSE"; }
        private void ParseHeartbeat(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "HEARTBEAT"; }
        private void ParseReject(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "REJECT"; }
        private void ParseTradeBust(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "TRADE_BUST"; }
        private void ParseTradeCaptureReportSingle(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "TCR_SINGLE"; }
        private void ParseTradeCaptureReportDual(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "TCR_DUAL"; }
        private void ParseTradeCaptureReportResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "TCR_RESP"; }
        private void ParseMassQuote(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "MASS_QUOTE"; }
        private void ParseMassQuoteResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "MASS_QUOTE_RESP"; }
        private void ParseRequestForExecution(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "RFE"; }
        private void ParseOrderMassCancel(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "MASS_CANCEL"; }
        private void ParseOrderMassCancelResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "MASS_CANCEL_RESP"; }
        private void ParseBidOfferUpdate(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "BID_OFFER"; }
        private void ParseMarketMakerCommand(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "MM_CMD"; }
        private void ParseMarketMakerCommandResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "MM_CMD_RESP"; }
        private void ParseGapFill(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "GAP_FILL"; }
        private void ParseOrderAdd(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "ORDER_ADD"; }
        private void ParseOrderAddResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "ORDER_ADD_RESP"; }
        private void ParseOrderCancel(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "ORDER_CANCEL"; }
        private void ParseOrderCancelResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "ORDER_CANCEL_RESP"; }
        private void ParseOrderModify(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "ORDER_MODIFY"; }
        private void ParseOrderModifyResponse(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "ORDER_MODIFY_RESP"; }
        private void ParseTrade(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "TRADE"; }
        private void ParseGenericMessage(CcgMessage ccgMessage, byte[] data) { ccgMessage.Symbol = "UNKNOWN"; }
    }
}