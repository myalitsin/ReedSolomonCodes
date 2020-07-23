using System;
using System.Collections.Generic;
using System.Linq;
using HammingCodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReedSolomonCodes.UnitTests
{
    [TestClass]
    public class ReedSolomonHammingTest
    {
        [TestMethod]
        public void ReedSolomonMixCheck()
        {
            Assert.AreEqual(true, TestMix());
        }

        private static void Split(int[] inputBytes, out int[] inputBytes1, out int[] inputBytes2)
        {
            int length = inputBytes.Length / 2;
            inputBytes1 = new int[length];
            inputBytes2 = new int[length];
            for (int i = 0; i < length; i++)
            {
                inputBytes1[i] = inputBytes[i * 2];
                inputBytes2[i] = inputBytes[i * 2 + 1];
            }
        }

        private static int[] Mix(HammingCode h, int[] inputBytes1, int[] inputBytes2)
        {
            int length = inputBytes1.Length;
            int[] package = new int[3 * length];
            for (int i = 0; i < length; i++)
            {
                package[3 * i] = inputBytes1[i];
                package[3 * i + 1] = inputBytes2[i];
                int information = inputBytes1[i] | (inputBytes2[i] << 8);
                package[3 * i + 2] = (byte)h.Encode(information);
            }
            return package;
        }

        private static int[] GenerateRandomErrors(int[] package, decimal errorPercent, out decimal realErrorPercent)
        {
            int length = package.Length;
            int[] badPackage = new int[length];
            Array.Copy(package, 0, badPackage, 0, length);
            int bitsNumber = length * 8;
            realErrorPercent = 0;
            for (int i = 0; i < bitsNumber; i++)
            {
                var rnd = ReedSolomonExtensions.GenerateRandomNumber(0xFFFFFFFF);
                bool invert = ((decimal)rnd * 100 / 0xFFFFFFFF) < errorPercent;
                if (invert)
                {
                    badPackage[i / 8] ^= 1 << (i % 8);
                    realErrorPercent++;
                }
            }
            realErrorPercent = realErrorPercent * 100 / bitsNumber;
            return badPackage;
        }

        private static int[] UnMix(HammingCode h, int[] inputBytes, out int[] inputBytes1, out int[] inputBytes2, out int errorsCount)
        {
            int length = inputBytes.Length / 3;
            List<int> distortion = new List<int>();
            inputBytes1 = new int[length];
            inputBytes2 = new int[length];
            errorsCount = 0;
            for (int i = 0; i < length; i++)
            {
                int information = inputBytes[3 * i] | (inputBytes[3 * i + 1] << 8);
                int codeword = information + (inputBytes[3 * i + 2] << h.InformationBitsNumber);
                long? decoded = h.Decode(codeword);
                inputBytes1[i] = (int)(decoded ?? inputBytes[3 * i]) & 0xFF;
                inputBytes2[i] = (int)((decoded ?? inputBytes[3 * i + 1]) >> 8) & 0xFF;
                if (decoded == null)
                {
                    distortion.Add(i);
                }
                else if (decoded != codeword)
                {
                    errorsCount++;
                }
            }
            return distortion.ToArray();
        }

        private static int[] Joint(int[] inputBytes1, int[] inputBytes2, int inputLength)
        {
            int[] joined = new int[inputLength * 2];
            for (int i = 0; i < inputLength; i++)
            {
                joined[i * 2] = inputBytes1[i];
                joined[i * 2 + 1] = inputBytes2[i];
            }
            return joined;
        }

        private static int[] EncodeMix(ReedSolomonCode rs, HammingCode h, int[] inputBytes)
        {
            Split(inputBytes, out var input1, out var input2);
            int[] encoded = Mix(h, rs.EncodeReedSolomon(input1), rs.EncodeReedSolomon(input2));
            return encoded;
        }

        private static int[] DecodeMix(ReedSolomonCode rs, HammingCode h, int[] package, out int errorsCount, out int distortionCount)
        {
            int[] distortionIndexes = UnMix(h, package, out var input1, out var input2, out errorsCount);
            distortionCount = distortionIndexes.Length;
            int dataLength = rs.CodewordLength;
            int[] decoded1 = rs.Decode(input1, distortionIndexes);
            int[] decoded2 = rs.Decode(input2, distortionIndexes);
            int[] decoded = Joint(decoded1, decoded2, dataLength - rs.ParitySymbolsNumber);
            return decoded;
        }

        private static bool TestMix()
        {
            var rs = ReedSolomon.Create255X239();
            var h = Hamming.Create24X16();
            int dataLength = rs.CodewordLength;
            int controlLength = rs.ParitySymbolsNumber;
            int inputLength = dataLength - controlLength;
            // generate data
            int[] inputBytes = ReedSolomonExtensions.GenerateRandomIntegers(2 * inputLength, rs.SymbolBitsMask);
            // encode
            int[] package = EncodeMix(rs, h, inputBytes);
            decimal errorPercent = (decimal)0.7;
            // add errors
            int[] badPackage = GenerateRandomErrors(package, errorPercent, out var realErrorPercent);
            if (realErrorPercent == 0)
                return false;
            // decode
            int[] decoded = DecodeMix(rs, h, badPackage, out int errorsCount, out int distortionCount);
            if ((errorsCount == 0) && (distortionCount == 0))
                return false;
            // check data
            Array.Resize(ref decoded, inputBytes.Length);
            return inputBytes.SequenceEqual(decoded);
        }
    }
}
