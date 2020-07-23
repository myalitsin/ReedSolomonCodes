using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ReedSolomonCodes.UnitTests
{
    [TestClass]
    public class ReedSolomonTest
    {
        [TestMethod]
        public void ReedSolomon7X3Check0()
        {
            Assert.AreEqual(true, ReedSolomon.Create7X3(false).Check());
        }

        [TestMethod]
        public void ReedSolomon7X3Check1()
        {
            Assert.AreEqual(true, ReedSolomon.Create7X3().Check());
        }

        [TestMethod]
        public void ReedSolomon15X11Check0()
        {
            Assert.AreEqual(true, ReedSolomon.Create15X11(false).Check());
        }

        [TestMethod]
        public void ReedSolomon15X11Check1()
        {
            Assert.AreEqual(true, ReedSolomon.Create15X11().Check());
        }

        [TestMethod]
        public void ReedSolomon31X27Check0()
        {
            Assert.AreEqual(true, ReedSolomon.Create31X27(false).Check());
        }

        [TestMethod]
        public void ReedSolomon31X27Check1()
        {
            Assert.AreEqual(true, ReedSolomon.Create31X27().Check());
        }

        [TestMethod]
        public void ReedSolomon255X233Check0()
        {
            Assert.AreEqual(true, ReedSolomon.Create255X233(false).Check());
        }

        [TestMethod]
        public void ReedSolomon255X233Check1()
        {
            Assert.AreEqual(true, ReedSolomon.Create255X233().Check());
        }

        [TestMethod]
        public void ReedSolomon255X239Check0()
        {
            Assert.AreEqual(true, ReedSolomon.Create255X239(false).Check());
        }

        [TestMethod]
        public void ReedSolomon255X239Check1()
        {
            Assert.AreEqual(true, ReedSolomon.Create255X239().Check());
        }

        [TestMethod]
        public void ReedSolomonCustom1Check()
        {
            Assert.AreEqual(true, ReedSolomon.Create(false, 8, 50).Check());
        }

        [TestMethod]
        public void ReedSolomonCustom2Check()
        {
            Assert.AreEqual(true, ReedSolomon.Create(false, 9, 50).Check());
        }

        [TestMethod]
        public void ReedSolomonBlockCheck()
        {
            byte[] source = {1, 2, 3};
            var rs = ReedSolomon.Create255X239(false);
            // adding data length before data
            int sourceLength = source.Length;
            byte[] bytes = new byte[rs.CodewordLength - rs.ParitySymbolsNumber];
            bytes[0] = (byte) sourceLength;
            Array.Copy(source, 0, bytes, 1, sourceLength);
            // adding redundancy
            bytes = rs.EncodeBlock(bytes);
            // distortion of data
            bytes[0] = bytes[1] = bytes[2] = 0;
            // data recovery
            bytes = rs.DecodeBlock(bytes);
            // formation of initial data
            byte[] destination = new byte[bytes[0]];
            Array.Copy(bytes, 1, destination, 0, destination.Length);
            Assert.AreEqual(true, source.SequenceEqual(destination));
        }

        [TestMethod]
        public void ReedSolomonBlocksCheck()
        {
            byte[] source = new byte[1000];
            // init data
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = (byte)i;
            }
            var rs = ReedSolomon.Create255X239(false);
            // adding redundancy
            byte[] bytes = rs.EncodeBlocks(source);
            // distortion of data
            for (int i = 0; i < bytes.Length; i+= 50)
            {
                bytes[i] = 33;
            }
            // data recovery
            bytes = rs.DecodeBlocks(bytes);
            byte[] destination = new byte[source.Length];
            Array.Copy(bytes, 0, destination, 0, destination.Length);
            Assert.AreEqual(true, source.SequenceEqual(destination));
        }

        [TestMethod]
        public void ReedSolomon255X239Check()
        {
            byte[] source = new byte[2000];
            // init data
            for (int i = 0; i < source.Length; i++)
            {
                source[i] = (byte)((i + 55) * DateTime.Now.Ticks);
            }
            // adding redundancy
            byte[] bytes = ReedSolomon255X239.Encode(source);
            // distortion of data
            for (int i = 0; i < bytes.Length; i += 40)
            {
                bytes[i] = 44;
            }
            // data recovery
            byte[] destination = ReedSolomon255X239.Decode(bytes);
            Assert.AreEqual(true, source.SequenceEqual(destination));
        }
    }
}
