// Services/CcgMessageDetailsParser.cs
using System;
using System.Collections.Generic;
using System.Text;
using RC_GUI_WATS.Models;

namespace RC_GUI_WATS.Services
{
    public class CcgMessageDetails
    {
        public string MessageType { get; set; }
        public string MessageName { get; set; }
        public List<CcgMessageField> Fields { get; set; } = new List<CcgMessageField>();
        public byte[] RawData { get; set; }
        public string RawDataHex { get; set; }
        public string ParseError { get; set; }
    }

    public class CcgMessageField
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public int Offset { get; set; }
        public int Length { get; set; }
    }

    public static class CcgMessageDetailsParser
    {
        public static CcgMessageDetails ParseMessageDetails(CcgMessage ccgMessage)
        {
            var details = new CcgMessageDetails
            {
                MessageType = ccgMessage.Header,
                MessageName = ccgMessage.Name,
                RawData = ccgMessage.RawData,
                RawDataHex = BitConverter.ToString(ccgMessage.RawData ?? new byte[0]).Replace("-", " ")
            };

            if (ccgMessage.RawData == null || ccgMessage.RawData.Length < 16)
            {
                details.ParseError = "Insufficient data for parsing";
                return details;
            }

            try
            {
                // Parse based on message type
                if (Enum.TryParse<CcgMessageType>(ccgMessage.Header, out var msgType))
                {
                    switch (msgType)
                    {
                        case CcgMessageType.Login:
                            ParseLogin(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.LoginResponse:
                            ParseLoginResponse(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.Logout:
                            ParseLogout(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.LogoutResponse:
                            ParseLogoutResponse(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.OrderAdd:
                            ParseOrderAdd(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.OrderAddResponse:
                            ParseOrderAddResponse(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.OrderCancel:
                            ParseOrderCancel(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.OrderCancelResponse:
                            ParseOrderCancelResponse(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.OrderModify:
                            ParseOrderModify(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.OrderModifyResponse:
                            ParseOrderModifyResponse(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.Trade:
                            ParseTrade(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.MassQuote:
                            ParseMassQuote(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.MassQuoteResponse:
                            ParseMassQuoteResponse(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.TradeCaptureReportSingle:
                            ParseTradeCaptureReportSingle(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.TradeCaptureReportDual:
                            ParseTradeCaptureReportDual(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.Reject:
                            ParseReject(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.ConnectionClose:
                            ParseConnectionClose(ccgMessage.RawData, details);
                            break;
                        case CcgMessageType.Heartbeat:
                            ParseHeartbeat(ccgMessage.RawData, details);
                            break;
                        default:
                            ParseGenericMessage(ccgMessage.RawData, details);
                            break;
                    }
                }
                else
                {
                    ParseGenericMessage(ccgMessage.RawData, details);
                }
            }
            catch (Exception ex)
            {
                details.ParseError = $"Parsing error: {ex.Message}";
                ParseGenericMessage(ccgMessage.RawData, details);
            }

            return details;
        }

        private static void ParseHeader(byte[] data, CcgMessageDetails details)
        {
            if (data.Length < 16) return;

            details.Fields.Add(new CcgMessageField
            {
                Name = "length",
                Type = "u16",
                Value = BitConverter.ToUInt16(data, 0).ToString(),
                Description = "Total length of the message",
                Offset = 0,  
                Length = 2
            });

            details.Fields.Add(new CcgMessageField
            {
                Name = "msgType",
                Type = "u16",
                Value = BitConverter.ToUInt16(data, 2).ToString(),
                Description = "Type of the message",
                Offset = 2,
                Length = 2
            });

            details.Fields.Add(new CcgMessageField
            {
                Name = "seqNum",
                Type = "u32",
                Value = BitConverter.ToUInt32(data, 4).ToString(),
                Description = "Sequence number of the message",
                Offset = 4,
                Length = 4
            });

            details.Fields.Add(new CcgMessageField
            {
                Name = "timestamp",
                Type = "u64",
                Value = BitConverter.ToUInt64(data, 8).ToString(),
                Description = "Sending time (nanoseconds since Unix epoch)",
                Offset = 8,
                Length = 8
            });
        }

        private static void ParseLogin(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 36) return;

            int offset = 16;

            var version = BitConverter.ToUInt16(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "version",
                Type = "u16",
                Value = version.ToString(),
                Description = "Protocol version",
                Offset = offset,
                Length = 2
            });
            offset += 2;

            var token = System.Text.Encoding.ASCII.GetString(data, offset, 8).TrimEnd('\0');
            details.Fields.Add(new CcgMessageField
            {
                Name = "token",
                Type = "char[8]",
                Value = $"'{token}'",
                Description = "Authentication token",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var connectionId = BitConverter.ToUInt16(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "connectionId",
                Type = "u16",
                Value = connectionId.ToString(),
                Description = "Connection ID",
                Offset = offset,
                Length = 2
            });
            offset += 2;

            var nextExpectedSeqNum = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "nextExpectedSeqNum",
                Type = "u32",
                Value = nextExpectedSeqNum.ToString(),
                Description = "Next expected message sequence number",
                Offset = offset,
                Length = 4
            });
            offset += 4;

            var lastSentSeqNum = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "lastSentSeqNum",
                Type = "u32",
                Value = lastSentSeqNum.ToString(),
                Description = "Last sent sequence number",
                Offset = offset,
                Length = 4
            });
        }

        private static void ParseLoginResponse(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 27) return;

            int offset = 16;

            var result = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "result",
                Type = "enum",
                Value = $"{result} ({GetLoginResultName(result)})",
                Description = "Login response status code",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            var nextExpectedSeqNum = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "nextExpectedSeqNum",
                Type = "u32",
                Value = nextExpectedSeqNum.ToString(),
                Description = "Next expected message sequence number",
                Offset = offset,
                Length = 4
            });
            offset += 4;

            var lastReplaySeqNum = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "lastReplaySeqNum",
                Type = "u32",
                Value = lastReplaySeqNum.ToString(),
                Description = "Last replay sequence number",
                Offset = offset,
                Length = 4
            });
            offset += 4;

            var sessionId = BitConverter.ToUInt16(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "sessionId",
                Type = "u16",
                Value = sessionId.ToString(),
                Description = "Session ID",
                Offset = offset,
                Length = 2
            });
        }

        private static void ParseLogout(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            details.Fields.Add(new CcgMessageField
            {
                Name = "(logout)",
                Type = "info",
                Value = "No additional data",
                Description = "Logout message contains only header",
                Offset = 16,
                Length = 0
            });
        }

        private static void ParseLogoutResponse(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            details.Fields.Add(new CcgMessageField
            {
                Name = "(logout_response)",
                Type = "info",
                Value = "No additional data",
                Description = "LogoutResponse message contains only header",
                Offset = 16,
                Length = 0
            });
        }

        private static void ParseOrderAdd(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 167) return;

            int offset = 16;

            details.Fields.Add(new CcgMessageField
            {
                Name = "stpId",
                Type = "u8",
                Value = data[offset].ToString(),
                Description = "ID assigned by the client used in Self Trade Prevention",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            details.Fields.Add(new CcgMessageField
            {
                Name = "instrumentId",
                Type = "u32",
                Value = BitConverter.ToUInt32(data, offset).ToString(),
                Description = "ID of the instrument being traded",
                Offset = offset,
                Length = 4
            });
            offset += 4;

            var orderType = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "orderType",
                Type = "enum",
                Value = $"{orderType} ({GetOrderTypeName(orderType)})",
                Description = "Indicates the order type",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            var timeInForce = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "timeInForce",
                Type = "enum",
                Value = $"{timeInForce} ({GetTimeInForceName(timeInForce)})",
                Description = "Indicates the order's time in force",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            var side = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "side",
                Type = "enum",
                Value = $"{side} ({GetSideName(side)})",
                Description = "Indicates the order's side (buy or sell)",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            var price = BitConverter.ToInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "price",
                Type = "i64",
                Value = $"{price} ({(decimal)price / 100000000m:F8})",
                Description = "Price per unit (smallest portion) of instrument",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var triggerPrice = BitConverter.ToInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "triggerPrice",
                Type = "i64",
                Value = $"{triggerPrice} ({(decimal)triggerPrice / 100000000m:F8})",
                Description = "Trigger price for stop orders",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var quantity = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "quantity",
                Type = "u64",
                Value = quantity.ToString(),
                Description = "Quantity of the instrument (number of Lots)",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var displayQty = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "displayQty",
                Type = "u64",
                Value = displayQty.ToString(),
                Description = "Displayed quantity for iceberg orders",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var capacity = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "capacity",
                Type = "enum",
                Value = $"{capacity} ({GetCapacityName(capacity)})",
                Description = "Capacity of the party making the order",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            var account = Encoding.ASCII.GetString(data, offset, 16).TrimEnd('\0');
            details.Fields.Add(new CcgMessageField
            {
                Name = "account",
                Type = "char[16]",
                Value = $"'{account}'",
                Description = "Account number",
                Offset = offset,
                Length = 16
            });
            offset += 16;

            // Skip MiFID fields for brevity
            offset += 16;

            if (offset + 20 <= data.Length)
            {
                var clientOrderId = Encoding.ASCII.GetString(data, offset, 20).TrimEnd('\0');
                details.Fields.Add(new CcgMessageField
                {
                    Name = "clientOrderId",
                    Type = "char[20]",
                    Value = $"'{clientOrderId}'",
                    Description = "Arbitrary user provided value associated with the order",
                    Offset = offset,
                    Length = 20
                });
            }
        }

        private static void ParseOrderAddResponse(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 52) return;

            int offset = 16;

            var orderId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "orderId",
                Type = "u64",
                Value = orderId.ToString(),
                Description = "Unique order identifier",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var publicOrderId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "publicOrderId",
                Type = "u64",
                Value = publicOrderId.ToString(),
                Description = "Public order identifier for market data",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var displayQty = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "displayQty",
                Type = "u64",
                Value = displayQty.ToString(),
                Description = "Quantity to be displayed",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var filled = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "filled",
                Type = "u64",
                Value = filled.ToString(),
                Description = "Quantity already filled",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var status = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "status",
                Type = "enum",
                Value = $"{status} ({GetOrderStatusName(status)})",
                Description = "Status of the order",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            var reason = BitConverter.ToUInt16(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "reason",
                Type = "u16",
                Value = reason.ToString(),
                Description = "Reason for rejection (if applicable)",
                Offset = offset,
                Length = 2
            });
            offset += 2;

            var execTypeReason = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "execTypeReason",
                Type = "enum",
                Value = $"{execTypeReason} ({GetExecTypeReasonName(execTypeReason)})",
                Description = "Reason for execution or lifecycle event",
                Offset = offset,
                Length = 1
            });
        }

        private static void ParseTrade(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 52) return;

            int offset = 16;

            var orderId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "orderId",
                Type = "u64",
                Value = orderId.ToString(),
                Description = "Order identifier",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var tradeId = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "id",
                Type = "u32",
                Value = tradeId.ToString(),
                Description = "ID of the trade",
                Offset = offset,
                Length = 4
            });
            offset += 4;

            var price = BitConverter.ToInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "price",
                Type = "i64",
                Value = $"{price} ({(decimal)price / 100000000m:F8})",
                Description = "Price per unit of the trade",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var quantity = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "quantity",
                Type = "u64",
                Value = quantity.ToString(),
                Description = "Quantity traded",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var leavesQty = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "leavesQty",
                Type = "u64",
                Value = leavesQty.ToString(),
                Description = "Quantity remaining on the order",
                Offset = offset,
                Length = 8
            });
        }

        private static void ParseMassQuote(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 1200) return;

            int offset = 16;

            var stpId = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "stpId",
                Type = "u8",
                Value = stpId.ToString(),
                Description = "Self Trade Prevention ID",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            var capacity = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "capacity",
                Type = "enum",
                Value = $"{capacity} ({GetCapacityName(capacity)})",
                Description = "Capacity of the party",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            // Skip account and MiFID fields for brevity
            offset += 16 + 1 + 16; // account + accountType + mifidFields
            offset += 18 + 20 + 1; // memo + clearingMemberCode + clearingMemberClearingIdentifier

            var quotesCount = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "quotes.count",
                Type = "u8",
                Value = quotesCount.ToString(),
                Description = "Number of quotes in this message",
                Offset = offset,
                Length = 1
            });
            offset += 1;

            // Parse first few quotes
            for (int i = 0; i < Math.Min((int)quotesCount, 3) && offset + 36 <= data.Length; i++)
            {
                var instrumentId = BitConverter.ToUInt32(data, offset);
                details.Fields.Add(new CcgMessageField
                {
                    Name = $"quote[{i}].instrumentId",
                    Type = "u32",
                    Value = instrumentId.ToString(),
                    Description = $"Instrument ID for quote {i}",
                    Offset = offset,
                    Length = 4
                });
                offset += 4;

                var bidPrice = BitConverter.ToInt64(data, offset);
                details.Fields.Add(new CcgMessageField
                {
                    Name = $"quote[{i}].bid.price",
                    Type = "i64",
                    Value = $"{bidPrice} ({(decimal)bidPrice / 100000000m:F8})",
                    Description = $"Bid price for quote {i}",
                    Offset = offset,
                    Length = 8
                });
                offset += 8;

                var bidQuantity = BitConverter.ToUInt64(data, offset);
                details.Fields.Add(new CcgMessageField
                {
                    Name = $"quote[{i}].bid.quantity",
                    Type = "u64",
                    Value = bidQuantity.ToString(),
                    Description = $"Bid quantity for quote {i}",
                    Offset = offset,
                    Length = 8
                });
                offset += 8;

                var askPrice = BitConverter.ToInt64(data, offset);
                details.Fields.Add(new CcgMessageField
                {
                    Name = $"quote[{i}].ask.price",
                    Type = "i64",
                    Value = $"{askPrice} ({(decimal)askPrice / 100000000m:F8})",
                    Description = $"Ask price for quote {i}",
                    Offset = offset,
                    Length = 8
                });
                offset += 8;

                var askQuantity = BitConverter.ToUInt64(data, offset);
                details.Fields.Add(new CcgMessageField
                {
                    Name = $"quote[{i}].ask.quantity",
                    Type = "u64",
                    Value = askQuantity.ToString(),
                    Description = $"Ask quantity for quote {i}",
                    Offset = offset,
                    Length = 8
                });
                offset += 8;
            }

            if (quotesCount > 3)
            {
                details.Fields.Add(new CcgMessageField
                {
                    Name = "...",
                    Type = "info",
                    Value = $"... and {(int)quotesCount - 3} more quotes",
                    Description = "Additional quotes not shown for brevity",
                    Offset = offset,
                    Length = 0
                });
            }
        }

        private static void ParseReject(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 21) return;

            int offset = 16;

            var refSeqNum = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "refSeqNum",
                Type = "u32",
                Value = refSeqNum.ToString(),
                Description = "Sequence number of the rejected message",
                Offset = offset,
                Length = 4
            });
            offset += 4;

            var rejectReason = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "rejectReason",
                Type = "enum",
                Value = $"{rejectReason} ({GetRejectReasonName(rejectReason)})",
                Description = "Reason for rejection",
                Offset = offset,
                Length = 1
            });
        }

        private static void ParseOrderCancel(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 40) return;

            int offset = 16;
            var orderId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "orderId",
                Type = "u64",
                Value = orderId.ToString(),
                Description = "Order identifier to cancel",
                Offset = offset,
                Length = 8
            });
        }

        private static void ParseOrderCancelResponse(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 28) return;

            int offset = 16;
            var orderId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "orderId",
                Type = "u64",
                Value = orderId.ToString(),
                Description = "Order identifier",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var status = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "status",
                Type = "enum",
                Value = $"{status} ({GetOrderStatusName(status)})",
                Description = "Status of the order",
                Offset = offset,
                Length = 1
            });
        }

        private static void ParseOrderModify(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 80) return;

            int offset = 16;
            var orderId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "orderId",
                Type = "u64",
                Value = orderId.ToString(),
                Description = "Order identifier to modify",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var price = BitConverter.ToInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "price",
                Type = "i64",
                Value = $"{price} ({(decimal)price / 100000000m:F8})",
                Description = "New price for the order",
                Offset = offset,
                Length = 8
            });
        }

        private static void ParseOrderModifyResponse(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 36) return;

            int offset = 16;
            var orderId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "orderId",
                Type = "u64",
                Value = orderId.ToString(),
                Description = "Order identifier",
                Offset = offset,
                Length = 8
            });
        }

        private static void ParseMassQuoteResponse(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 719) return;

            int offset = 16;
            var massQuoteId = BitConverter.ToUInt64(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "massQuoteId",
                Type = "u64",
                Value = massQuoteId.ToString(),
                Description = "Mass quote identifier",
                Offset = offset,
                Length = 8
            });
            offset += 8;

            var responsesCount = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "responses.count",
                Type = "u8",
                Value = responsesCount.ToString(),
                Description = "Number of quote responses",
                Offset = offset,
                Length = 1
            });
        }

        private static void ParseTradeCaptureReportSingle(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 206) return;

            int offset = 16;
            var instrumentId = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "instrumentId",
                Type = "u32",
                Value = instrumentId.ToString(),
                Description = "Instrument ID",
                Offset = offset,
                Length = 4
            });
            offset += 4;

            var tradeReportId = System.Text.Encoding.ASCII.GetString(data, offset, 21).TrimEnd('\0');
            details.Fields.Add(new CcgMessageField
            {
                Name = "tradeReportId",
                Type = "char[21]",
                Value = $"'{tradeReportId}'",
                Description = "Trade report identifier",
                Offset = offset,
                Length = 21
            });
        }

        private static void ParseTradeCaptureReportDual(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 271) return;

            int offset = 16;
            var instrumentId = BitConverter.ToUInt32(data, offset);
            details.Fields.Add(new CcgMessageField
            {
                Name = "instrumentId",
                Type = "u32",
                Value = instrumentId.ToString(),
                Description = "Instrument ID",
                Offset = offset,
                Length = 4
            });
        }

        private static void ParseConnectionClose(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            if (data.Length < 17) return;

            int offset = 16;
            var reason = data[offset];
            details.Fields.Add(new CcgMessageField
            {
                Name = "reason",
                Type = "enum",
                Value = $"{reason} ({GetConnectionCloseReasonName(reason)})",
                Description = "Connection close reason",
                Offset = offset,
                Length = 1
            });
        }

        private static void ParseHeartbeat(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);
            details.Fields.Add(new CcgMessageField
            {
                Name = "(heartbeat)",
                Type = "info",
                Value = "No additional data",
                Description = "Heartbeat message contains only header",
                Offset = 16,
                Length = 0
            });
        }

        private static void ParseGenericMessage(byte[] data, CcgMessageDetails details)
        {
            ParseHeader(data, details);

            if (data.Length > 16)
            {
                var remainingBytes = new byte[data.Length - 16];
                Array.Copy(data, 16, remainingBytes, 0, remainingBytes.Length);

                details.Fields.Add(new CcgMessageField
                {
                    Name = "payload",
                    Type = "bytes",
                    Value = BitConverter.ToString(remainingBytes).Replace("-", " "),
                    Description = "Message payload (hex dump)",
                    Offset = 16,
                    Length = remainingBytes.Length
                });
            }
        }

        // Helper methods for enum conversions
        private static string GetOrderTypeName(byte orderType) => orderType switch
        {
            1 => "Limit",
            2 => "Market",
            3 => "MarketToLimit",
            4 => "Iceberg",
            5 => "StopLimit",
            6 => "StopLoss",
            _ => "Unknown"
        };

        private static string GetTimeInForceName(byte timeInForce) => timeInForce switch
        {
            1 => "Day",
            2 => "GTC",
            3 => "IOC",
            4 => "FOK",
            5 => "VFA",
            6 => "GTD",
            7 => "VFC",
            8 => "GTT",
            _ => "Unknown"
        };

        private static string GetSideName(byte side) => side switch
        {
            1 => "Buy",
            2 => "Sell",
            _ => "Unknown"
        };

        private static string GetCapacityName(byte capacity) => capacity switch
        {
            1 => "Agency",
            2 => "Principal",
            3 => "RisklessPrincipal",
            _ => "Unknown"
        };

        private static string GetOrderStatusName(byte status) => status switch
        {
            1 => "New",
            2 => "Cancelled",
            3 => "Rejected",
            4 => "Filled",
            5 => "PartiallyFilled",
            6 => "Expired",
            _ => "Unknown"
        };

        private static string GetExecTypeReasonName(byte reason) => reason switch
        {
            1 => "NA",
            2 => "CancelOnDisconnect",
            3 => "Expired",
            4 => "Triggered",
            5 => "CancelOnSuspension",
            6 => "OrderRestatement",
            7 => "IcebergOrderRefill",
            8 => "CancelByStp",
            9 => "CancelByCorporateAction",
            10 => "CancelByMassCancel",
            11 => "CancelIocFokOrder",
            12 => "CancelByMarketOperations",
            13 => "Replaced",
            14 => "FirstTradeOnAggressiveOrder",
            15 => "Rejected",
            _ => "Unknown"
        };

        private static string GetRejectReasonName(byte reason) => reason switch
        {
            0 => "NA",
            1 => "MaxThroughputExceeded",
            2 => "InvalidMsgType",
            3 => "InvalidExpireTimePrecision",
            4 => "InvalidSettlementDate",
            5 => "SettlementDateRequired",
            6 => "TradeReportIdRequired",
            7 => "MissingReportIdSecondaryTradeReportIdOrTradeReportRefId",
            8 => "InvalidTradeId",
            9 => "InvalidAlgorithmicTradeIndicator",
            10 => "InvalidTradeReportId",
            11 => "InvalidGapFillSeqNum",
            _ => "Unknown"
        };

        private static string GetConnectionCloseReasonName(byte reason) => reason switch
        {
            1 => "ProtocolError",
            2 => "InvalidSeqNum",
            3 => "EndOfDay",
            4 => "SyncFail",
            5 => "AntiFloodingThresholdExceeded",
            6 => "ConnectionConfigChanged",
            7 => "CloseOps",
            8 => "Disconnect",
            _ => "Unknown"
        };

        private static string GetLoginResultName(byte result) => result switch
        {
            1 => "Ok",
            2 => "NotFound",
            3 => "InvalidToken",
            4 => "AlreadyLoggedIn",
            5 => "AccountLocked",
            6 => "LoginNotAllowed",
            7 => "InvalidLoginParameters",
            8 => "ThrottlingTemporaryLock",
            9 => "Other",
            _ => "Unknown"
        };
    }
}