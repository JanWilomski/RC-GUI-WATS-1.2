// Services/CcgMessageParser.cs
using System;
using System.Text;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class CcgMessageParser
    {
        public static CcgMessage ParseCcgMessage(byte[] data, DateTime receivedTime)
        {
            if (data == null || data.Length < 16)
                return null;

            try
            {
                var message = new CcgMessage
                {
                    DateReceived = receivedTime,
                    RawData = data
                };

                // Parse header (first 16 bytes)
                ushort length = BitConverter.ToUInt16(data, 0);
                ushort msgType = BitConverter.ToUInt16(data, 2);
                uint seqNum = BitConverter.ToUInt32(data, 4);
                ulong timestamp = BitConverter.ToUInt64(data, 8);

                message.Header = msgType.ToString();
                message.SequenceNumber = seqNum;
                message.TransactTime = UnixTimeToDateTime(timestamp);

                // Debug: Log message details
                System.Diagnostics.Debug.WriteLine($"Parsing CCG Message: Type={msgType}, Length={length}, DataLength={data.Length}, SeqNum={seqNum}");

                // Get message name
                if (Enum.IsDefined(typeof(CcgMessageType), msgType))
                {
                    message.Name = ((CcgMessageType)msgType).ToString();
                }
                else
                {
                    message.Name = $"Unknown({msgType})";
                    System.Diagnostics.Debug.WriteLine($"Unknown message type: {msgType}");
                }

                // Parse specific message types
                switch ((CcgMessageType)msgType)
                {
                    case CcgMessageType.OrderAdd:
                        ParseOrderAdd(data, message);
                        break;
                    case CcgMessageType.OrderAddResponse:
                        ParseOrderAddResponse(data, message);
                        break;
                    case CcgMessageType.OrderCancel:
                        ParseOrderCancel(data, message);
                        break;
                    case CcgMessageType.OrderCancelResponse:
                        ParseOrderCancelResponse(data, message);
                        break;
                    case CcgMessageType.OrderModify:
                        ParseOrderModify(data, message);
                        break;
                    case CcgMessageType.OrderModifyResponse:
                        ParseOrderModifyResponse(data, message);
                        break;
                    case CcgMessageType.Trade:
                        ParseTrade(data, message);
                        break;
                    case CcgMessageType.Reject:
                        ParseReject(data, message);
                        break;
                    case CcgMessageType.OrderMassCancel:
                        ParseOrderMassCancel(data, message);
                        break;
                    case CcgMessageType.OrderMassCancelResponse:
                        ParseOrderMassCancelResponse(data, message);
                        break;
                    case CcgMessageType.TradeCaptureReportSingle:
                        ParseTradeCaptureReportSingle(data, message);
                        break;
                    case CcgMessageType.TradeCaptureReportDual:
                        ParseTradeCaptureReportDual(data, message);
                        break;
                    default:
                        // For other message types, just parse basic info
                        break;
                }

                // Debug: Log parsed results
                System.Diagnostics.Debug.WriteLine($"Parsed: {message.Name}, InstrumentId={message.InstrumentId}, Side={message.Side}, Price={message.Price}, Qty={message.Quantity}, ClientOrderId={message.ClientOrderId}");

                return message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing CCG message: {ex.Message}");
                // If parsing fails, return basic message with error info
                return new CcgMessage
                {
                    Header = "ERROR",
                    Name = $"Parse Error: {ex.Message}",
                    DateReceived = receivedTime,
                    RawData = data
                };
            }
        }

        private static void ParseOrderAdd(byte[] data, CcgMessage message)
        {
            if (data.Length < 167) return; // OrderAdd length is 167 bytes

            try
            {
                // Header: 16 bytes
                // stpId: 1 byte (at offset 16)
                // instrumentId: 4 bytes (at offset 17)
                message.InstrumentId = BitConverter.ToUInt32(data, 17);
                
                // Skip to orderType (1 byte at offset 21), timeInForce (1 byte at offset 22)
                // side: 1 byte at offset 23
                byte sideValue = data[23];
                message.Side = sideValue == 1 ? "Buy" : sideValue == 2 ? "Sell" : $"Unknown({sideValue})";

                // price: 8 bytes at offset 24 (Price is Alias(Number) = i64)
                long priceRaw = BitConverter.ToInt64(data, 24);
                // Price is per unit, convert from fixed point (likely scaled by 10000 or 100000000)
                message.Price = (decimal)priceRaw / 100000000m; // Try 10000 first, adjust if needed

                // triggerPrice: 8 bytes at offset 32
                // quantity: 8 bytes at offset 40
                message.Quantity = BitConverter.ToUInt64(data, 40);

                // displayQty: 8 bytes at offset 48
                // capacity, account, accountType, mifidFields follow...
                // clientOrderId: 20 bytes at offset 124 (according to structure)
                if (data.Length >= 144)
                {
                    byte[] clientOrderIdBytes = new byte[20];
                    Array.Copy(data, 124, clientOrderIdBytes, 0, 20);
                    message.ClientOrderId = Encoding.ASCII.GetString(clientOrderIdBytes).TrimEnd('\0', ' ');
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderAdd: {ex.Message}");
            }
        }

        private static void ParseOrderAddResponse(byte[] data, CcgMessage message)
        {
            if (data.Length < 52) return; // OrderAddResponse length is 52 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString(); // Store orderId as string for reference
                message.Quantity = BitConverter.ToUInt64(data, 40);

                // publicOrderId: 8 bytes at offset 24
                // No price, side, or instrumentId in response - these would need to be correlated with original order
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderAddResponse: {ex.Message}");
            }
        }

        private static void ParseOrderCancel(byte[] data, CcgMessage message)
        {
            if (data.Length < 40) return; // OrderCancel length is 40 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString(); // Store orderId as reference
                
                // OrderCancel doesn't have price, side, or instrumentId directly
                // These would need to be looked up from the original order
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderCancel: {ex.Message}");
            }
        }

        private static void ParseOrderCancelResponse(byte[] data, CcgMessage message)
        {
            if (data.Length < 28) return; // OrderCancelResponse length is 28 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();
                
                // status: 1 byte at offset 24
                // reason: 2 bytes at offset 25
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderCancelResponse: {ex.Message}");
            }
        }

        private static void ParseOrderModify(byte[] data, CcgMessage message)
        {
            if (data.Length < 80) return; // OrderModify length is 80 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();

                // price: 8 bytes at offset 24
                long priceRaw = BitConverter.ToInt64(data, 24);
                message.Price = (decimal)priceRaw / 10000m;

                // triggerPrice: 8 bytes at offset 32
                // quantity: 8 bytes at offset 40
                // displayQty: 8 bytes at offset 48
                
                // OrderModify doesn't specify side or instrumentId directly
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderModify: {ex.Message}");
            }
        }

        private static void ParseOrderModifyResponse(byte[] data, CcgMessage message)
        {
            if (data.Length < 36) return; // OrderModifyResponse length is 36 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();

                // filled: 8 bytes at offset 24
                // status: 1 byte at offset 32
                // priorityFlag: 1 byte at offset 33
                // reason: 2 bytes at offset 34
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderModifyResponse: {ex.Message}");
            }
        }
        

        private static void ParseReject(byte[] data, CcgMessage message)
        {
            if (data.Length < 21) return; // Reject length is 21 bytes

            try
            {
                // Header: 16 bytes
                // refSeqNum: 4 bytes at offset 16
                uint refSeqNum = BitConverter.ToUInt32(data, 16);
                message.ClientOrderId = $"SeqNum:{refSeqNum}";

                // rejectReason: 1 byte at offset 20
                byte rejectReason = data[20];
                message.Side = $"Reject({rejectReason})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Reject: {ex.Message}");
            }
        }

        private static void ParseOrderMassCancel(byte[] data, CcgMessage message)
        {
            if (data.Length < 35) return; // OrderMassCancel length is 35 bytes

            try
            {
                // Header: 16 bytes
                // massCancelRequestType: 1 byte at offset 16
                byte requestType = data[16];
                
                // targetPartyRole: 1 byte at offset 17
                // targetPartyId: 4 bytes at offset 18
                // marketSegmentId: 4 bytes at offset 22
                // instrumentId: 4 bytes at offset 26
                message.InstrumentId = BitConverter.ToUInt32(data, 26);

                message.Side = $"MassCancel({requestType})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderMassCancel: {ex.Message}");
            }
        }

        private static void ParseOrderMassCancelResponse(byte[] data, CcgMessage message)
        {
            if (data.Length < 44) return; // OrderMassCancelResponse length is 44 bytes

            try
            {
                // Header: 16 bytes
                // totalAffectedOrders: 8 bytes at offset 16
                ulong affectedOrders = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = $"Affected:{affectedOrders}";

                // massCancelRequestType: 1 byte at offset 24
                byte requestType = data[24];
                
                // massCancelId: 8 bytes at offset 25
                // targetPartyRole: 1 byte at offset 33
                // targetPartyId: 4 bytes at offset 34
                // marketSegmentId: 4 bytes at offset 38
                // reason: 2 bytes at offset 42

                message.Side = $"MassCancelResp({requestType})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderMassCancelResponse: {ex.Message}");
            }
        }



        private static void ParseTrade(byte[] data, CcgMessage message)
        {
            if (data.Length < 52) return; // Trade length is 52 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();

                // id (TradeId): 4 bytes at offset 24
                // price: 8 bytes at offset 28
                long priceRaw = BitConverter.ToInt64(data, 28);
                message.Price = (decimal)priceRaw / 100000000m;

                // quantity: 8 bytes at offset 36
                message.Quantity = BitConverter.ToUInt64(data, 36);
                
                // leavesQty: 8 bytes at offset 44

                // Trade doesn't specify side directly - it's the result of matching
                message.Side = "Trade";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Trade: {ex.Message}");
            }
        }

        private static void ParseTradeCaptureReportSingle(byte[] data, CcgMessage message)
        {
            if (data.Length < 206) return; // TradeCaptureReportSingle length is 206 bytes

            try
            {
                // Header: 16 bytes
                // instrumentId: 4 bytes at offset 16
                message.InstrumentId = BitConverter.ToUInt32(data, 16);

                // tradeReportId: 21 bytes at offset 20
                // secondaryTradeReportId: 8 bytes at offset 41
                // tradeId: 4 bytes at offset 49
                // tradeReportTransType: 1 byte at offset 53
                // tradeReportType: 1 byte at offset 54
                // tradeType: 1 byte at offset 55
                // algorithmicTradeIndicator: 1 byte at offset 56
                // execType: 1 byte at offset 57
                // tradeReportRefId: 21 bytes at offset 58
                // lastQty: 8 bytes at offset 79
                message.Quantity = BitConverter.ToUInt64(data, 79);
                
                // lastPx: 8 bytes at offset 87
                long priceRaw = BitConverter.ToInt64(data, 87);
                message.Price = (decimal)priceRaw / 10000m;

                // settlementDate: 4 bytes at offset 95
                // side: 1 byte at offset 99
                byte sideValue = data[99];
                message.Side = sideValue == 1 ? "Buy" : sideValue == 2 ? "Sell" : $"Unknown({sideValue})";

                // counterpartyCode: 16 bytes at offset 100
                // Rest is MiFID fields and party information
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing TradeCaptureReportSingle: {ex.Message}");
            }
        }

        private static void ParseTradeCaptureReportDual(byte[] data, CcgMessage message)
        {
            if (data.Length < 271) return; // TradeCaptureReportDual length is 271 bytes

            try
            {
                // Header: 16 bytes
                // instrumentId: 4 bytes at offset 16
                message.InstrumentId = BitConverter.ToUInt32(data, 16);

                // tradeReportId: 21 bytes at offset 20
                // tradeId: 4 bytes at offset 41
                // tradeReportTransType: 1 byte at offset 45
                // tradeReportType: 1 byte at offset 46
                // tradeType: 1 byte at offset 47
                // algorithmicTradeIndicator: 1 byte at offset 48
                // execType: 1 byte at offset 49
                // tradeReportRefId: 21 bytes at offset 50
                // lastQty: 8 bytes at offset 71
                message.Quantity = BitConverter.ToUInt64(data, 71);
                
                // lastPx: 8 bytes at offset 79
                long priceRaw = BitConverter.ToInt64(data, 79);
                message.Price = (decimal)priceRaw / 10000m;

                // settlementDate: 4 bytes at offset 87
                // Then follows buy and sell party information

                message.Side = "Dual";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing TradeCaptureReportDual: {ex.Message}");
            }
        }

        private static DateTime UnixTimeToDateTime(ulong nanoseconds)
        {
            // Convert nanoseconds since Unix epoch to DateTime
            try
            {
                long ticks = (long)(nanoseconds / 100); // Convert nanoseconds to 100-nanosecond ticks
                return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(ticks).ToLocalTime();
            }
            catch
            {
                return DateTime.Now; // Fallback if conversion fails
            }
        }

        public static string GetMessageTypeName(ushort msgType)
        {
            if (Enum.IsDefined(typeof(CcgMessageType), msgType))
            {
                return ((CcgMessageType)msgType).ToString();
            }
            return $"Unknown({msgType})";
        }
    }
}