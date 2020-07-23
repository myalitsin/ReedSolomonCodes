using System;

namespace ReedSolomonCodes
{
    public static class ReedSolomon255X239
    {
        public static byte[] Encode(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            var rs = ReedSolomon.Create255X239(false);
            int length = bytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(length);
            byte[] b = new byte[length + lengthBytes.Length];
            Array.Copy(lengthBytes, 0, b, 0, lengthBytes.Length);
            Array.Copy(bytes, 0, b, lengthBytes.Length, bytes.Length);
            return rs.EncodeBlocks(b);
        }

        public static byte[] Decode(byte[] bytes)
        {
            var rs = ReedSolomon.Create255X239(false);
            int dataLength = rs.CodewordLength;
            if ((bytes == null) || ((bytes.Length % dataLength) != 0))
            {
                return null;
            }
            bytes = rs.DecodeBlocks(bytes);
            int length = BitConverter.ToInt32(bytes, 0);
            length = Math.Min(length, bytes.Length - sizeof(Int32));
            byte[] b = new byte[length];
            Array.Copy(bytes, sizeof(Int32), b, 0, length);
            return b;
        }
    }
}
