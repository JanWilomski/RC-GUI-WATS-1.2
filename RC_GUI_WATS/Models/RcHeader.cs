using System;
using System.Text;

namespace RC_GUI_WATS.Models
{
    public class RcHeader
    {
        public string Session { get; set; } = new string('\0', 10);
        public uint SequenceNumber { get; set; }
        public ushort BlockCount { get; set; }

        public const int HeaderSize = 16;

        public byte[] ToBytes()
        {
            byte[] data = new byte[HeaderSize];
            
            // Session (char[10])
            byte[] sessionBytes = Encoding.ASCII.GetBytes(Session);
            Array.Copy(sessionBytes, 0, data, 0, Math.Min(sessionBytes.Length, 10));
            
            // SequenceNumber (uint32 - Little Endian)
            BitConverter.GetBytes(SequenceNumber).CopyTo(data, 10);
            
            // BlockCount (uint16 - Little Endian)
            BitConverter.GetBytes(BlockCount).CopyTo(data, 14);
            
            return data;
        }

        public static RcHeader FromBytes(byte[] data)
        {
            if (data.Length < HeaderSize)
                throw new ArgumentException("Data too small for header");

            var header = new RcHeader
            {
                Session = Encoding.ASCII.GetString(data, 0, 10),
                SequenceNumber = BitConverter.ToUInt32(data, 10),
                BlockCount = BitConverter.ToUInt16(data, 14)
            };

            return header;
        }
    }
}