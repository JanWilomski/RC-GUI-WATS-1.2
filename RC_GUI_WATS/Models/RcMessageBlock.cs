using System;

namespace RC_GUI_WATS.Models
{
    public class RcMessageBlock
    {
        public ushort Length { get; set; }
        public byte[] Payload { get; set; }

        public const int BlockHeaderSize = 2;

        public byte[] ToBytes()
        {
            byte[] data = new byte[BlockHeaderSize + Length];
            
            // Length (uint16 - Little Endian)
            BitConverter.GetBytes(Length).CopyTo(data, 0);
            
            // Payload
            if (Payload != null && Payload.Length > 0)
                Array.Copy(Payload, 0, data, BlockHeaderSize, Length);
            
            return data;
        }

        public static RcMessageBlock FromBytes(byte[] data, int offset)
        {
            if (data.Length < offset + BlockHeaderSize)
                throw new ArgumentException("Data too small for block header");

            ushort length = BitConverter.ToUInt16(data, offset);
            
            if (data.Length < offset + BlockHeaderSize + length)
                throw new ArgumentException("Data too small for block payload");

            var block = new RcMessageBlock
            {
                Length = length,
                Payload = new byte[length]
            };

            Array.Copy(data, offset + BlockHeaderSize, block.Payload, 0, length);
            
            return block;
        }
    }
}