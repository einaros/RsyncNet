namespace RsyncNetTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using RsyncNet.Hash;
    using RsyncNet.Libraries;

    [TestClass]
    public class HashBlockGeneratorTest
    {
        #region Fields: private

        private const int BLOCK_SIZE = 10;

        #endregion

        private readonly uint[] checkSums = new[]
                                                {
                                                    183829005u,
                                                    245105335u,
                                                    281150235u,
                                                    119276045u
                                                };

        private readonly string[] md5sums = new[]
                                                {
                                                    "781e5e245d69b566979b86e28d23f2c7",
                                                    "e86410fa2d6e2634fd8ac5f4b3afe7f3",
                                                    "d123d9c26465577a2d10958881c9b31a",
                                                    "a224f9f2c9355a8dc616362aa2a76e6a"
                                                };

        private string originalFile =
            "0123456789" +
            "ABCDEFGHIJ" +
            "KLMNOPQRST" +
            "UVWXYZ";

        #region Methods: public

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void Constructor_throws_for_negative_block_size()
        {
            new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), -1);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Constructor_throws_for_null_checksumProvider()
        {
            new HashBlockGenerator(null, new HashAlgorithmWrapper<MD5>(MD5.Create()), 10);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Constructor_throws_for_null_hashProvider()
        {
            new HashBlockGenerator(new RollingChecksum(), null, 10);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void Constructor_throws_for_zero_block_size()
        {
            new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), 0);
        }

        [TestMethod]
        public void ProcessStream_returns_correct_block_count()
        {
            MemoryStream stream = GetOriginalFileStream();
            var gen = new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), BLOCK_SIZE);
            Assert.AreEqual(
                Math.Ceiling(originalFile.Length/(float) BLOCK_SIZE),
                gen.ProcessStream(stream).Count());
        }

        [TestMethod]
        public void ProcessStream_returns_hashes_with_correct_length()
        {
            MemoryStream stream = GetOriginalFileStream();
            var gen = new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), BLOCK_SIZE);
            int i = 0;
            Assert.IsTrue(gen.ProcessStream(stream).All(x =>
                                                        x.Length ==
                                                        Math.Min(BLOCK_SIZE, originalFile.Length - i++*BLOCK_SIZE)));
        }

        [TestMethod]
        public void ProcessStream_returns_hashes_with_correct_md5_sums()
        {
            MemoryStream stream = GetOriginalFileStream();
            var gen = new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), BLOCK_SIZE);
            int i = 0;
            Assert.IsTrue(gen.ProcessStream(stream).All(x =>
                                                        BitConverter.ToString(x.Hash)
                                                            .Replace("-", "")
                                                            .ToLower()
                                                            .Equals(md5sums[i++].ToLower())
                              ));
        }

        [TestMethod]
        public void ProcessStream_returns_hashes_with_correct_offset()
        {
            MemoryStream stream = GetOriginalFileStream();
            var gen = new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), BLOCK_SIZE);
            int i = 0;
            Assert.IsTrue(gen.ProcessStream(stream).All(x =>
                                                        x.Offset == i++*BLOCK_SIZE));
        }

        [TestMethod]
        public void ProcessStream_returns_hashes_with_correct_rolling_sums()
        {
            MemoryStream stream = GetOriginalFileStream();
            var gen = new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), BLOCK_SIZE);
            int i = 0;
            Assert.IsTrue(gen.ProcessStream(stream).All(x => x.Checksum == checkSums[i++]));
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void ProcessStream_throws_for_null_stream()
        {
            var gen = new HashBlockGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()), BLOCK_SIZE);
            gen.ProcessStream(null).Count();
        }

        #endregion

        #region Methods: private

        private MemoryStream GetOriginalFileStream()
        {
            return new MemoryStream(Encoding.ASCII.GetBytes(originalFile));
        }

        #endregion
    }
}