using System;
using System.Linq;

namespace ReedSolomonCodes
{
    public static class ReedSolomonExtensions
    {
        public static byte[] EncodeBlock(this ReedSolomonCode rs, byte[] bytes)
        {
            int dataLength = rs.CodewordLength - rs.ParitySymbolsNumber;
            if ((bytes == null) || (bytes.Length != dataLength))
            {
                return null;
            }
            int[] dataInput = new int[dataLength];
            int[] dataOutput = new int[rs.CodewordLength];
            for (int i = 0; i < dataLength; i++)
            {
                dataInput[i] = dataOutput[i] = bytes[i];
            }
            rs.Encode(dataInput, dataOutput, dataLength);
            return dataOutput.Select(i => (byte) i).ToArray();
        }

        public static byte[] DecodeBlock(this ReedSolomonCode rs, byte[] bytes)
        {
            if ((bytes == null) || (bytes.Length != rs.CodewordLength))
            {
                return null;
            }
            int[] decoded = rs.Decode(bytes.Select(b => (int)b).ToArray(), null);
            byte[] result = new byte[rs.CodewordLength - rs.ParitySymbolsNumber];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (byte) decoded[i];
            }
            return result;
        }

        public static byte[] EncodeBlocks(this ReedSolomonCode rs, byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            int blockSize = rs.CodewordLength;
            int dataLength = blockSize - rs.ParitySymbolsNumber;
            int blocksNumber = (bytes.Length + dataLength - 1) / dataLength;
            int[] dataOutput = new int[blocksNumber * blockSize];
            for (int b = 0; b < blocksNumber; b++)
            {
                int[] dataInput = new int[dataLength];
                int dataLengthToCopy = Math.Min(dataLength, bytes.Length - b * dataLength);
                for (int i = 0; i < dataLengthToCopy; i++)
                {
                    dataInput[i] = dataOutput[b * blockSize + i] = bytes[b * dataLength + i];
                }
                rs.Encode(dataInput, dataOutput, b * blockSize + dataLength);
            }
            return dataOutput.Select(i => (byte)i).ToArray();
        }

        public static byte[] DecodeBlocks(this ReedSolomonCode rs, byte[] bytes)
        {
            int dataLength = rs.CodewordLength;
            if ((bytes == null) || ((bytes.Length % dataLength) != 0))
            {
                return null;
            }
            int blocksNumber = bytes.Length / dataLength;
            int infoLength = dataLength - rs.ParitySymbolsNumber;
            byte[] result = new byte[infoLength * blocksNumber];
            for (int b = 0; b < blocksNumber; b++)
            {
                int[] dataInput = new int[dataLength];
                for (int i = 0; i < dataLength; i++)
                {
                    dataInput[i] = bytes[b * dataLength + i];
                }
                int[] decoded = rs.Decode(dataInput, null);
                for (int i = 0; i < infoLength; i++)
                {
                    result[b * infoLength + i] = (byte)decoded[i];
                }
            }
            return result;
        }
    }
}
