using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace ReedSolomonCodes.UnitTests
{
    public static class ReedSolomonExtensions
    {
        public static bool Check(this ReedSolomonCode rs, int checkCount = 100)
        {
            for (int i = 0; i < checkCount; i++)
            {
                if (!CheckReedSolomon(rs, false) ||
                    !CheckReedSolomon(rs, true) ||
                    !CheckHorner(rs))
                {
                    return false;//fail
                }
            }
            return true;//ok
        }

        private static bool CheckReedSolomon(ReedSolomonCode rs, bool withDistortion)
        {
            int dataLength = rs.CodewordLength;
            int controlLength = rs.ParitySymbolsNumber;
            int inputLength = dataLength - controlLength;
            // generate data
            int[] inputBytes = GenerateRandomIntegers(inputLength, rs.SymbolBitsMask);
            int[] package = EncodeReedSolomon(rs, inputBytes);
            // create copy package
            int[] badPackage = new int[dataLength];
            Array.Copy(package, 0, badPackage, 0, dataLength);
            List<int> distortion = new List<int>(controlLength);
            int badSymbolCount = withDistortion ? controlLength : (controlLength / 2);
            for (int i = 0; i < badSymbolCount; i++)
            {
                int index;
                do
                {
                    index = GenerateRandomBytes(1)[0] % dataLength;
                } while (distortion.Contains(index));
                distortion.Add(index);
                badPackage[index] = badPackage[index] ^ GenerateRandomIntegers(1, rs.SymbolBitsMask)[0];
            }
            distortion.Sort();
            // decode
            int[] decoded = rs.Decode(badPackage, withDistortion ? distortion.ToArray() : null);
            // check data
            Array.Resize(ref decoded, inputLength);
            return inputBytes.SequenceEqual(decoded);
        }

        private static bool CheckHorner(ReedSolomonCode rs)
        {
            int dataLength = rs.CodewordLength + 1;
            int controlLength = rs.ParitySymbolsNumber;
            int inputLength = dataLength - controlLength;
            // generate data
            int[] inputBytes = GenerateRandomIntegers(inputLength, rs.SymbolBitsMask);
            // encode
            int[] package = rs.EncodeHorner(inputBytes);
            // create copy package
            int[] badPackage = new int[dataLength];
            Array.Copy(package, 0, badPackage, 0, dataLength);
            int[] distortion = new int[dataLength];
            int badSymbolCount = controlLength / 2;
            for (int i = 0; i < badSymbolCount; i++)
            {
                var index = GenerateRandomBytes(1)[0] % dataLength;
                distortion[(index / 2) * 2] = 1;
                badPackage[index] = badPackage[index] ^ GenerateRandomIntegers(1, rs.SymbolBitsMask)[0];
            }
            // decode
            int[] decoded = rs.DecodeHorner(badPackage, distortion);
            // check data
            Array.Resize(ref decoded, inputLength);
            return inputBytes.SequenceEqual(decoded);
        }

        public static int[] EncodeReedSolomon(this ReedSolomonCode rs, int[] inputBytes)
        {
            int inputLength = rs.CodewordLength - rs.ParitySymbolsNumber;
            if (inputBytes.Length != inputLength)
            {
                Array.Resize(ref inputBytes, inputLength);
            }
            int[] package = new int[rs.CodewordLength];
            Array.Copy(inputBytes, 0, package, 0, inputLength);
            Array.Copy(rs.Encode(inputBytes), 0, package, inputLength, rs.ParitySymbolsNumber);
            return package;
        }

        public static byte[] GenerateRandomBytes(int count)
        {
            var bytes = new byte[count];
            _cryptoProvider.GetBytes(bytes);
            return bytes;
        }

        public static long GenerateRandomNumber(long mask)
        {
            return BitConverter.ToInt64(GenerateRandomBytes(8), 0) & mask;
        }

        public static int[] GenerateRandomIntegers(int count, long mask)
        {
            var integers = new int[count];
            for (int i = 0; i < count; i++)
            {
                integers[i] = (int)GenerateRandomNumber(mask);
            }
            return integers;
        }

        private static readonly RNGCryptoServiceProvider _cryptoProvider = new RNGCryptoServiceProvider((CspParameters)null);
    }
}
