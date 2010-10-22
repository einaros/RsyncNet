namespace RsyncNetTests
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using RsyncNet;
    using RsyncNet.Hash;

    [TestClass]
    public class HashBlockStreamerTest
    {
        #region Methods: public

        [TestMethod]
        public void Destream_can_read_streamed_blocks()
        {
            var blocks = new HashBlock[5];
            for (int index = 0; index < blocks.Length; index++)
            {
                blocks[index] = new HashBlock
                                    {
                                        Checksum = (uint) index,
                                        Length = index * 2,
                                        Offset = 9223372036854775807L,
                                        Hash = new byte[16]
                                    };
                new Random().NextBytes(blocks[index].Hash);
            }
            var ms = new MemoryStream();
            HashBlockStreamer.Stream(blocks, ms);
            ms.Seek(0, SeekOrigin.Begin);
            HashBlock[] newBlocks = HashBlockStreamer.Destream(ms);
            Assert.AreEqual(blocks.Length, newBlocks.Length);
            for (int index = 0; index < newBlocks.Length; index++)
            {
                HashBlock newBlock = newBlocks[index];
                HashBlock oldBlock = blocks[index];
                Assert.AreEqual(oldBlock.Checksum, newBlock.Checksum);
                Assert.AreEqual(oldBlock.Length, newBlock.Length);
                Assert.AreEqual(oldBlock.Offset, newBlock.Offset);
                Assert.IsTrue(oldBlock.Hash.SequenceEqual(newBlock.Hash));
            }
        }

        [TestMethod]
        public void Stream_writes_valid_byte_length()
        {
            var blocks = new HashBlock[5];
            for (int index = 0; index < blocks.Length; index++)
            {
                blocks[index] = new HashBlock {Hash = new byte[16]};
            }
            var ms = new MemoryStream();
            HashBlockStreamer.Stream(blocks, ms);
            Assert.AreEqual(blocks.Length*32 + 4, ms.Length);
        }

        [TestMethod]
        public void Stream_writes_valid_length()
        {
            var blocks = new HashBlock[5];
            for (int index = 0; index < blocks.Length; index++)
            {
                blocks[index] = new HashBlock {Hash = new byte[16]};
            }
            var ms = new MemoryStream();
            HashBlockStreamer.Stream(blocks, ms);
            ms.Seek(0, SeekOrigin.Begin);
            var buf = new byte[4];
            ms.Read(buf, 0, 4);
            Assert.AreEqual((uint) blocks.Length, BitConverter.ToUInt32(buf, 0));
        }

        #endregion
    }
}