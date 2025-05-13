using System;
using System.Collections.Generic;
using System.Text;

namespace RC_GUI_WATS.Models
{
    public class RcMessage
    {
        public RcHeader Header { get; set; }
        public List<RcMessageBlock> Blocks { get; set; } = new List<RcMessageBlock>();

        public byte[] ToBytes()
        {
            // Update block count in header
            Header.BlockCount = (ushort)Blocks.Count;
            
            // Calculate total size
            int totalSize = RcHeader.HeaderSize;
            foreach (var block in Blocks)
            {
                totalSize += RcMessageBlock.BlockHeaderSize + block.Length;
            }
            
            byte[] data = new byte[totalSize];
            
            // Copy header
            Header.ToBytes().CopyTo(data, 0);
            
            // Copy blocks
            int offset = RcHeader.HeaderSize;
            foreach (var block in Blocks)
            {
                byte[] blockData = block.ToBytes();
                blockData.CopyTo(data, offset);
                offset += blockData.Length;
            }
            
            return data;
        }

        public static RcMessage FromBytes(byte[] data)
        {
            var message = new RcMessage
            {
                Header = RcHeader.FromBytes(data)
            };

            int offset = RcHeader.HeaderSize;
            for (int i = 0; i < message.Header.BlockCount; i++)
            {
                var block = RcMessageBlock.FromBytes(data, offset);
                message.Blocks.Add(block);
                offset += RcMessageBlock.BlockHeaderSize + block.Length;
            }

            return message;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Session: {Header.Session}, Sequence: {Header.SequenceNumber}, Blocks: {Header.BlockCount}");
            
            int blockIndex = 0;
            foreach (var block in Blocks)
            {
                sb.AppendLine($"Block {blockIndex++}: Length={block.Length}");
                
                if (block.Payload.Length > 0 && block.Payload[0] >= 'A' && block.Payload[0] <= 'Z')
                {
                    char type = (char)block.Payload[0];
                    sb.AppendLine($"  Type: {type}");
                    
                    // Dekodowanie różnych typów wiadomości
                    switch (type)
                    {
                        case 'D': // Debug log
                        case 'I': // Info log
                        case 'W': // Warning log
                        case 'E': // Error log
                            if (block.Payload.Length >= 3)
                            {
                                ushort msgLength = BitConverter.ToUInt16(block.Payload, 1);
                                if (block.Payload.Length >= 3 + msgLength)
                                {
                                    string message = Encoding.ASCII.GetString(block.Payload, 3, msgLength);
                                    sb.AppendLine($"  Log: {message}");
                                }
                            }
                            break;
                        case 'P': // Position
                            if (block.Payload.Length >= 25)
                            {
                                string isin = Encoding.ASCII.GetString(block.Payload, 1, 12);
                                int net = BitConverter.ToInt32(block.Payload, 13);
                                int openLong = BitConverter.ToInt32(block.Payload, 17);
                                int openShort = BitConverter.ToInt32(block.Payload, 21);
                                sb.AppendLine($"  ISIN: {isin}, Net: {net}, Open Long: {openLong}, Open Short: {openShort}");
                            }
                            break;
                        case 'C': // Capital
                            if (block.Payload.Length >= 25)
                            {
                                double openCapital = BitConverter.ToDouble(block.Payload, 1);
                                double accruedCapital = BitConverter.ToDouble(block.Payload, 9);
                                double totalCapital = BitConverter.ToDouble(block.Payload, 17);
                                sb.AppendLine($"  Open Capital: {openCapital}, Accrued Capital: {accruedCapital}, Total Capital: {totalCapital}");
                            }
                            break;
                        case 'B': // I/O bytes
                            if (block.Payload.Length >= 3)
                            {
                                ushort msgLength = BitConverter.ToUInt16(block.Payload, 1);
                                if (block.Payload.Length >= 3 + msgLength)
                                {
                                    string message = Encoding.ASCII.GetString(block.Payload, 3, msgLength);
                                    sb.AppendLine($"  CCG Message: {message}");
                                }
                            }
                            break;
                        default:
                            sb.AppendLine($"  Nieobsługiwany typ wiadomości: {type}");
                            break;
                    }
                }
                else
                {
                    // Wyświetlanie surowych danych
                    sb.AppendLine($"  Raw data: {BitConverter.ToString(block.Payload)}");
                }
            }
            
            return sb.ToString();
        }
    }
}