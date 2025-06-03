// Services/CcgMessageParser.cs - Improved version for Order Book
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
                        ParseOrderAdd(message);
                        break;
                    case CcgMessageType.OrderAddResponse:
                        ParseOrderAddResponse(message);
                        break;
                    case CcgMessageType.OrderCancel:
                        ParseOrderCancel(message);
                        break;
                    case CcgMessageType.OrderCancelResponse:
                        ParseOrderCancelResponse(message);
                        break;
                    case CcgMessageType.OrderModify:
                        ParseOrderModify(message);
                        break;
                    case CcgMessageType.OrderModifyResponse:
                        ParseOrderModifyResponse(message);
                        break;
                    case CcgMessageType.Trade:
                        ParseTrade(message);
                        break;
                    case CcgMessageType.Reject:
                        ParseReject(message);
                        break;
                    case CcgMessageType.OrderMassCancel:
                        ParseOrderMassCancel(message);
                        break;
                    case CcgMessageType.OrderMassCancelResponse:
                        ParseOrderMassCancelResponse(message);
                        break;
                    case CcgMessageType.TradeCaptureReportSingle:
                        ParseTradeCaptureReportSingle(data, message);
                        break;
                    case CcgMessageType.TradeCaptureReportDual:
                        ParseTradeCaptureReportDual(data, message);
                        break;
                    case CcgMessageType.MassQuote:
                        ParseMassQuote(data, message);
                        break;
                    case CcgMessageType.MassQuoteResponse:
                        ParseMassQuoteResponse(data, message);
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

        private static void ParseOrderAdd(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 167) return; // OrderAdd length is 167 bytes

            try
            {
                // Header: 16 bytes
                // stpId: 1 byte (at offset 16)
                // instrumentId: 4 bytes (at offset 17)
                message.InstrumentId = BitConverter.ToUInt32(data, 17);
                
                // orderType: 1 byte (at offset 21)
                byte orderType = data[21];
                message.Side = GetOrderTypeName(orderType);
                
                // timeInForce: 1 byte (at offset 22)
                // side: 1 byte (at offset 23)
                byte sideValue = data[23];
                message.Side = sideValue == 1 ? "Buy" : sideValue == 2 ? "Sell" : $"Unknown({sideValue})";

                // price: 8 bytes (at offset 24) - Price is Alias(Number) = i64
                long priceRaw = BitConverter.ToInt64(data, 24);
                message.Price = (decimal)priceRaw / 100000000m; // 8 decimal places scaling

                // triggerPrice: 8 bytes (at offset 32)
                // quantity: 8 bytes (at offset 40)
                message.Quantity = BitConverter.ToUInt64(data, 40);

                // displayQty: 8 bytes (at offset 48)
                // Skip capacity, account, accountType, mifidFields...
                // clientOrderId: 20 bytes (at offset 144)
                if (data.Length >= 164)
                {
                    byte[] clientOrderIdBytes = new byte[20];
                    Array.Copy(data, 144, clientOrderIdBytes, 0, 20);
                    message.ClientOrderId = Encoding.ASCII.GetString(clientOrderIdBytes).TrimEnd('\0', ' ');
                }
                
                System.Diagnostics.Debug.WriteLine($"OrderAdd: InstrumentId={message.InstrumentId}, Side={message.Side}, Price={message.Price}, Qty={message.Quantity}, ClientOrderId={message.ClientOrderId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderAdd: {ex.Message}");
            }
        }

        private static void ParseOrderAddResponse(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 52) return; // OrderAddResponse length is 52 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString(); // Store orderId as string for order book tracking
                
                // publicOrderId: 8 bytes at offset 24
                ulong publicOrderId = BitConverter.ToUInt64(data, 24);
                
                // displayQty: 8 bytes at offset 32
                // filled: 8 bytes at offset 40
                message.Quantity = BitConverter.ToUInt64(data, 40); // Use filled quantity
                
                // status: 1 byte at offset 48
                byte status = data[48];
                message.Side = GetOrderStatusName(status);
                
                System.Diagnostics.Debug.WriteLine($"OrderAddResponse: OrderId={orderId}, PublicOrderId={publicOrderId}, Status={status}, Filled={message.Quantity}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderAddResponse: {ex.Message}");
            }
        }

        private static void ParseOrderCancel(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 40) return; // OrderCancel length is 40 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString(); // Store orderId as reference
                message.Side = "Cancel";
                
                System.Diagnostics.Debug.WriteLine($"OrderCancel: OrderId={orderId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderCancel: {ex.Message}");
            }
        }

        private static void ParseOrderCancelResponse(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 28) return; // OrderCancelResponse length is 28 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();
                
                // status: 1 byte at offset 24
                byte status = data[24];
                message.Side = GetOrderStatusName(status);
                
                System.Diagnostics.Debug.WriteLine($"OrderCancelResponse: OrderId={orderId}, Status={status}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderCancelResponse: {ex.Message}");
            }
        }

        private static void ParseOrderModify(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 80) return; // OrderModify length is 80 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();

                // price: 8 bytes at offset 24
                long priceRaw = BitConverter.ToInt64(data, 24);
                message.Price = (decimal)priceRaw / 100000000m;

                // triggerPrice: 8 bytes at offset 32
                // quantity: 8 bytes at offset 40
                message.Quantity = BitConverter.ToUInt64(data, 40);
                
                message.Side = "Modify";
                
                System.Diagnostics.Debug.WriteLine($"OrderModify: OrderId={orderId}, Price={message.Price}, Qty={message.Quantity}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderModify: {ex.Message}");
            }
        }

        private static void ParseOrderModifyResponse(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 36) return; // OrderModifyResponse length is 36 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();

                // filled: 8 bytes at offset 24
                message.Quantity = BitConverter.ToUInt64(data, 24);
                
                // status: 1 byte at offset 32
                byte status = data[32];
                message.Side = GetOrderStatusName(status);
                
                System.Diagnostics.Debug.WriteLine($"OrderModifyResponse: OrderId={orderId}, Status={status}, Filled={message.Quantity}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderModifyResponse: {ex.Message}");
            }
        }

        private static void ParseTrade(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 52) return; // Trade length is 52 bytes

            try
            {
                // Header: 16 bytes
                // orderId: 8 bytes at offset 16
                ulong orderId = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = orderId.ToString();

                // id (TradeId): 4 bytes at offset 24
                uint tradeId = BitConverter.ToUInt32(data, 24);
                
                // price: 8 bytes at offset 28 (now i64 according to latest spec)
                long priceRaw = BitConverter.ToInt64(data, 28);
                message.Price = (decimal)priceRaw / 100000000m;

                // quantity: 8 bytes at offset 36
                message.Quantity = BitConverter.ToUInt64(data, 36);
                
                // leavesQty: 8 bytes at offset 44
                ulong leavesQty = BitConverter.ToUInt64(data, 44);

                message.Side = "Trade";
                
                System.Diagnostics.Debug.WriteLine($"Trade: OrderId={orderId}, TradeId={tradeId}, Price={message.Price}, Qty={message.Quantity}, LeavesQty={leavesQty}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Trade: {ex.Message}");
            }
        }

        private static void ParseReject(CcgMessage message)
        {
            var data = message.RawData;
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
                
                System.Diagnostics.Debug.WriteLine($"Reject: RefSeqNum={refSeqNum}, Reason={rejectReason}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing Reject: {ex.Message}");
            }
        }

        private static void ParseOrderMassCancel(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 35) return; // OrderMassCancel length is 35 bytes

            try
            {
                // Header: 16 bytes
                // massCancelRequestType: 1 byte at offset 16
                byte requestType = data[16];
                
                // instrumentId: 4 bytes at offset 26
                message.InstrumentId = BitConverter.ToUInt32(data, 26);

                message.Side = $"MassCancel({requestType})";
                
                System.Diagnostics.Debug.WriteLine($"OrderMassCancel: RequestType={requestType}, InstrumentId={message.InstrumentId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderMassCancel: {ex.Message}");
            }
        }

        private static void ParseOrderMassCancelResponse(CcgMessage message)
        {
            var data = message.RawData;
            if (data.Length < 44) return; // OrderMassCancelResponse length is 44 bytes

            try
            {
                // Header: 16 bytes
                // totalAffectedOrders: 8 bytes at offset 16
                ulong affectedOrders = BitConverter.ToUInt64(data, 16);
                message.ClientOrderId = $"Affected:{affectedOrders}";

                // massCancelRequestType: 1 byte at offset 24
                byte requestType = data[24];
                
                message.Side = $"MassCancelResp({requestType})";
                message.Quantity = affectedOrders;
                
                System.Diagnostics.Debug.WriteLine($"OrderMassCancelResponse: RequestType={requestType}, AffectedOrders={affectedOrders}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing OrderMassCancelResponse: {ex.Message}");
            }
        }

        private static void ParseMassQuote(byte[] data, CcgMessage message)
        {
            if (data.Length < 1200) return; // MassQuote length is 1200 bytes

            try
            {
                // Header: 16 bytes
                // stpId: 1 byte at offset 16
                byte stpId = data[16];
                
                // capacity: 1 byte at offset 17
                byte capacity = data[17];
                
                // Skip to quotes.count: 1 byte at offset 90
                byte quotesCount = data[90];
                
                message.Side = "MassQuote";
                message.ClientOrderId = $"STP:{stpId}";
                message.Quantity = quotesCount; // Store quotes count as quantity
                
                // Parse first quote if available
                if (quotesCount > 0 && data.Length >= 91 + 36)
                {
                    uint instrumentId = BitConverter.ToUInt32(data, 91);
                    message.InstrumentId = instrumentId;
                    
                    // Parse bid price (first price in the quote) - now i64
                    long bidPriceRaw = BitConverter.ToInt64(data, 95);
                    message.Price = (decimal)bidPriceRaw / 100000000m;
                    
                    message.ClientOrderId = $"STP:{stpId},Quotes:{quotesCount},Instr:{instrumentId}";
                }
                
                System.Diagnostics.Debug.WriteLine($"MassQuote parsed: STP={stpId}, Capacity={capacity}, QuotesCount={quotesCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing MassQuote: {ex.Message}");
            }
        }

        private static void ParseMassQuoteResponse(byte[] data, CcgMessage message)
        {
            if (data.Length < 719) return; // MassQuoteResponse length is 719 bytes

            try
            {
                // Header: 16 bytes
                // massQuoteId: 8 bytes at offset 16
                ulong massQuoteId = BitConverter.ToUInt64(data, 16);
                
                // responses.count: 1 byte at offset 24
                byte responsesCount = data[24];
                
                message.Side = "MassQuoteResp";
                message.ClientOrderId = $"QuoteId:{massQuoteId}";
                message.Quantity = responsesCount; // Store responses count as quantity
                
                System.Diagnostics.Debug.WriteLine($"MassQuoteResponse parsed: QuoteId={massQuoteId}, ResponsesCount={responsesCount}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing MassQuoteResponse: {ex.Message}");
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

                // Skip to lastQty: 8 bytes at offset 79
                message.Quantity = BitConverter.ToUInt64(data, 79);
                
                // lastPx: 8 bytes at offset 87 (now i64)
                long priceRaw = BitConverter.ToInt64(data, 87);
                message.Price = (decimal)priceRaw / 100000000m;

                // side: 1 byte at offset 99
                byte sideValue = data[99];
                message.Side = sideValue == 1 ? "Buy" : sideValue == 2 ? "Sell" : $"Unknown({sideValue})";
                
                System.Diagnostics.Debug.WriteLine($"TradeCaptureReportSingle: InstrumentId={message.InstrumentId}, Side={message.Side}, Price={message.Price}, Qty={message.Quantity}");
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

                // Skip to lastQty: 8 bytes at offset 71
                message.Quantity = BitConverter.ToUInt64(data, 71);
                
                // lastPx: 8 bytes at offset 79 (now i64)
                long priceRaw = BitConverter.ToInt64(data, 79);
                message.Price = (decimal)priceRaw / 100000000m;

                message.Side = "Dual";
                
                System.Diagnostics.Debug.WriteLine($"TradeCaptureReportDual: InstrumentId={message.InstrumentId}, Price={message.Price}, Qty={message.Quantity}");
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

        private static string GetOrderTypeName(byte orderType)
        {
            return orderType switch
            {
                1 => "Limit",
                2 => "Market",
                3 => "MarketToLimit",
                4 => "Iceberg",
                5 => "StopLimit",
                6 => "StopLoss",
                _ => $"Unknown({orderType})"
            };
        }

        private static string GetOrderStatusName(byte status)
        {
            return status switch
            {
                1 => "New",
                2 => "Cancelled",
                3 => "Rejected",
                4 => "Filled",
                5 => "PartiallyFilled",
                6 => "Expired",
                _ => $"Unknown({status})"
            };
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