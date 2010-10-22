namespace RsyncNetTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using RsyncNet.Delta;
    using RsyncNet.Hash;
    using RsyncNet.Libraries;

    [TestClass]
    public class DeltaGeneratorTest
    {
        #region Methods: public

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Constructor_throws_for_null_checksumProvider()
        {
            new DeltaGenerator(null, new HashAlgorithmWrapper<MD5>(MD5.Create()));
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Constructor_throws_for_null_hashProvider()
        {
            new DeltaGenerator(new RollingChecksum(), null);
        }

        [TestMethod]
        public void GetDeltas_returns_one_ByteDelta_object_when_no_hashes_are_provided()
        {
            DeltaGenerator gen = GetValidDeltaGenerator();
            int blockSize = 10;
            gen.Initialize(blockSize, null);
            var bytes = new byte[blockSize*3];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte) (bytes.Length - i);
            }
            IEnumerable<IDelta> deltas = gen.GetDeltas(new MemoryStream(bytes));
            Assert.AreEqual(1, deltas.Count());
            Assert.IsInstanceOfType(deltas.First(), typeof (ByteDelta));
            var byteDelta = deltas.First() as ByteDelta;
            Assert.AreEqual(0, byteDelta.Offset);
            Assert.AreEqual(bytes.Length, byteDelta.Length);
        }

        [TestMethod]
        public void GetDeltas_returns_three_CopyDeltas_object_when_matching_hash_is_provided_for_triple_repeat()
        {
            var checksumMock = new Mock<IRollingChecksum>();
            checksumMock.Setup(x => x.Value).Returns(0);
            var hashMock = new Mock<IHashAlgorithm>();
            byte[] dummyBytes = InitializeDummyMD5Hash(hashMock);
            var gen = new DeltaGenerator(checksumMock.Object, hashMock.Object);
            int blockSize = 10;
            var magicHashBlock = new HashBlock {Checksum = 0, Hash = dummyBytes, Offset = 42, Length = 43};
            gen.Initialize(blockSize, new[] {magicHashBlock});
            var bytes = new byte[blockSize*3];
            for (int i = 0; i < bytes.Length; ++i) bytes[i] = 0;
            IEnumerable<IDelta> deltas = gen.GetDeltas(new MemoryStream(bytes));
            Assert.AreEqual(3, deltas.Count());
            Assert.IsTrue(deltas.All(d => d is CopyDelta));
            Assert.IsTrue(deltas.All(d => (d as CopyDelta).Offset == magicHashBlock.Offset));
        }

        [TestMethod]
        public void GetDeltas_returns_two_CopyDelta_objects_and_a_middle_ByteDelta_for_interspaced_new_data()
        {
            var checksumMock = new Mock<IRollingChecksum>();
            int checksumCallNumber = 0;
            // Returns "1" for calls number 2 through 11 inclusive.
            checksumMock.Setup(x => x.Value)
                .Returns(() => (uint) (++checksumCallNumber > 1 && checksumCallNumber <= 11 ? 1 : 0));
            var hashMock = new Mock<IHashAlgorithm>();
            byte[] dummyBytes = InitializeDummyMD5Hash(hashMock);
            var gen = new DeltaGenerator(checksumMock.Object, hashMock.Object);
            int blockSize = 10;
            var magicHashBlock = new HashBlock {Checksum = 0, Hash = dummyBytes, Offset = 42, Length = 43};
            gen.Initialize(blockSize, new[] {magicHashBlock});
            var bytes = new byte[blockSize*3];
            for (int i = 0; i < bytes.Length; ++i) bytes[i] = 0;
            // Should immediately get a match for an entire block. 
            // Then upon trying to match a new one get a mismatch for the next
            // 10 bytes
            IEnumerable<IDelta> deltas = gen.GetDeltas(new MemoryStream(bytes));
            Assert.AreEqual(3, deltas.Count());
            Assert.IsTrue(
                deltas.ElementAt(0) is CopyDelta &&
                deltas.ElementAt(1) is ByteDelta &&
                deltas.ElementAt(2) is CopyDelta);
            Assert.AreEqual(10, (deltas.ElementAt(1) as ByteDelta).Offset);
            Assert.AreEqual(10, (deltas.ElementAt(1) as ByteDelta).Length);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void GetDeltas_throws_for_null_stream()
        {
            var dummyBlock =
                new HashBlock
                    {
                        Checksum = 0,
                        Length = 0,
                        Offset = 0,
                        Hash = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8}
                    };
            DeltaGenerator gen = GetValidDeltaGenerator();
            gen.Initialize(10, new[] {dummyBlock});
            gen.GetDeltas(null).Count();
        }

        [TestMethod]
        [ExpectedException(typeof (InvalidOperationException))]
        public void GetDeltas_throws_if_not_initialized()
        {
            DeltaGenerator gen = GetValidDeltaGenerator();
            gen.GetDeltas(new MemoryStream());
        }

        [TestMethod]
        public void Initialize_accepts_null_hashblock_array()
        {
            DeltaGenerator gen = GetValidDeltaGenerator();
            gen.Initialize(10, null);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void Initialize_throws_for_negative_blocksize()
        {
            DeltaGenerator gen = GetValidDeltaGenerator();
            gen.Initialize(-1, new HashBlock[10]);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void Initialize_throws_for_zero_blocksize()
        {
            DeltaGenerator gen = GetValidDeltaGenerator();
            gen.Initialize(0, new HashBlock[10]);
        }

        #endregion

        #region Methods: private

        private static DeltaGenerator GetValidDeltaGenerator()
        {
            return new DeltaGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()));
        }

        private static byte[] InitializeDummyMD5Hash(Mock<IHashAlgorithm> hashMock)
        {
            var dummyBytes = new byte[16];
            for (int i = 0; i < dummyBytes.Length; ++i) dummyBytes[i] = 0;
            // Always returns the same fake MD5 hash
            hashMock
                .Setup(x =>
                       x.ComputeHash(
                           It.IsAny<byte[]>(),
                           It.IsAny<int>(),
                           It.IsAny<int>()))
                .Returns(dummyBytes);
            return dummyBytes;
        }

        #endregion
    }
}