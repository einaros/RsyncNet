namespace RsyncNetTests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Moq.Language.Flow;
    using RsyncNet.Helpers;
    using RsyncNet.Libraries;

    [TestClass]
    public class SlidingStreamBufferTest
    {
        #region Methods: public

        [TestMethod]
        public void Constructor_accepts_valid_arguments()
        {
            new SlidingStreamBuffer(new MemoryStream(), 100);
        }

        [TestMethod]
        public void Constructor_doesnt_pull_data_from_stream()
        {
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            new SlidingStreamBuffer(streamMock.Object, 100);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Constructor_throws_for_null_stream()
        {
            new SlidingStreamBuffer(null, 100);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void Constructor_throws_for_window_size_less_than_one()
        {
            new SlidingStreamBuffer(new MemoryStream(), 0);
        }

        [TestMethod]
        public void GetBuffer_calls_warmup_if_necessary()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => 50);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.GetBuffer();
            streamMock.VerifyAll();
        }

        [TestMethod]
        public void GetBuffer_returns_a_buffer_corresponding_to_GetNumBytesAvailable()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => 50);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            streamMock.VerifyAll();
            Assert.AreEqual(buf.GetNumBytesAvailable(), buf.GetBuffer().Length);
        }

        [TestMethod]
        public void GetBuffer_returns_a_buffer_with_valid_data()
        {
            int window = 100;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) =>
                             {
                                 int actualLength = MathEx.Bounded(0, Math.Min(l, buffer.Length), buffer.Length);
                                 Array.Copy(buffer, 0, s, o, actualLength);
                                 return actualLength;
                             });
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            VerifyBufferPartialEquality(buffer, buf.GetBuffer());
        }

        [TestMethod]
        public void GetBuffer_when_moved_beyond_end_of_stream_yields_empty_buffer()
        {
            int window = 10;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var buf = new SlidingStreamBuffer(new MemoryStream(buffer), window, window);
            buf.Warmup();
            buf.MoveForward(buffer.Length + 1);
            Assert.IsTrue(buf.GetBuffer().Length == 0);
        }

        [TestMethod]
        public void GetBuffer_when_moved_to_end_of_stream_yields_empty_buffer()
        {
            int window = 10;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var buf = new SlidingStreamBuffer(new MemoryStream(buffer), window, window);
            buf.Warmup();
            buf.MoveForward(buffer.Length);
            Assert.IsTrue(buf.GetBuffer().Length == 0);
        }

        [TestMethod]
        public void GetByteAt_calls_warmup_if_necessary()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => 50);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.GetByteAt(0);
            streamMock.VerifyAll();
        }

        [TestMethod]
        public void GetByteAt_retrieves_correct_byte()
        {
            int window = 100;
            var buffer = new byte[window];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) =>
                             {
                                 int actualLength = MathEx.Bounded(0, Math.Min(l, buffer.Length), buffer.Length);
                                 Array.Copy(buffer, 0, s, o, actualLength);
                                 return actualLength;
                             });
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            Assert.AreEqual(buffer[5], buf.GetByteAt(5));
        }

        [TestMethod]
        [ExpectedException(typeof (IndexOutOfRangeException))]
        public void GetByteAt_throws_exception_for_index_over_windowSize()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => window);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.GetByteAt(window);
        }

        [TestMethod]
        [ExpectedException(typeof (IndexOutOfRangeException))]
        public void GetByteAt_throws_exception_for_negative_index()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => 50);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.GetByteAt(-1);
        }

        [TestMethod]
        public void GetNumBytesAvailable_returns_less_than_windowSize_for_short_read()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) => 50);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            Assert.AreEqual(50, buf.GetNumBytesAvailable());
        }

        [TestMethod]
        public void GetNumBytesAvailable_returns_windowSize_for_full_read()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) => l);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            Assert.AreEqual(window, buf.GetNumBytesAvailable());
        }

        [TestMethod]
        public void GetNumBytesAvailable_when_moved_beyond_end_of_stream_returns_zero()
        {
            int window = 10;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var buf = new SlidingStreamBuffer(new MemoryStream(buffer), window, window);
            buf.Warmup();
            buf.MoveForward(buffer.Length + 1);
            Assert.AreEqual(0, buf.GetNumBytesAvailable());
        }

        [TestMethod]
        public void GetNumBytesAvailable_when_moved_to_end_of_stream_returns_zero()
        {
            int window = 10;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var buf = new SlidingStreamBuffer(new MemoryStream(buffer), window, window);
            buf.Warmup();
            buf.MoveForward(buffer.Length);
            Assert.AreEqual(0, buf.GetNumBytesAvailable());
        }

        [TestMethod]
        public void Indexer_retrieves_correct_byte()
        {
            int window = 100;
            var buffer = new byte[window];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) =>
                             {
                                 int actualLength = MathEx.Bounded(0, Math.Min(l, buffer.Length), buffer.Length);
                                 Array.Copy(buffer, 0, s, o, actualLength);
                                 return actualLength;
                             });
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            Assert.AreEqual(buffer[5], buf[5]);
        }

        [TestMethod]
        [ExpectedException(typeof (IndexOutOfRangeException))]
        public void Indexer_throw_exception_for_negative_index()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => 50);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            int i = buf[-1];
        }

        [TestMethod]
        [ExpectedException(typeof (IndexOutOfRangeException))]
        public void Indexer_throws_exception_for_index_over_windowSize()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => window);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            int i = buf[window];
        }

        [TestMethod]
        public void MoveForward_beyond_windowSize_with_no_more_data_in_stream_yields_NumAvailable_lower_than_windowSize()
        {
            int window = 10;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var buf = new SlidingStreamBuffer(new MemoryStream(buffer), window, window);
            buf.Warmup();
            buf.MoveForward(window + 1);
            Assert.AreEqual(window - 1, buf.GetNumBytesAvailable());
        }

        [TestMethod]
        public void MoveForward_causes_more_data_to_be_read_when_no_padding_buffer_is_supplied()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => l);
            var buf = new SlidingStreamBuffer(streamMock.Object, window, 0);
            buf.Warmup();
            buf.MoveForward(1);
            buf.GetByteAt(0);
            streamMock.Verify(x =>
                              x.Read(
                                  It.IsAny<byte[]>(),
                                  It.IsAny<int>(),
                                  It.IsAny<int>()),
                              Times.Exactly(2));
        }

        [TestMethod]
        public void MoveForward_doesnt_cause_more_data_to_be_read_when_a_padding_buffer_is_supplied()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => l);
            var buf = new SlidingStreamBuffer(streamMock.Object, window, 3);
            buf.Warmup();
            buf.MoveForward(1);
            buf.MoveForward(1);
            buf.MoveForward(1);
            buf.GetByteAt(0);
            streamMock.Verify(x =>
                              x.Read(
                                  It.IsAny<byte[]>(),
                                  It.IsAny<int>(),
                                  It.IsAny<int>()),
                              Times.Exactly(1));
        }

        [TestMethod]
        public void MoveForward_pulls_at_most_windowSize_plus_extrabuffer_minus_totalValidBytes()
        {
            int window = 100;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) =>
                             {
                                 if (l > buffer.Length - o) Assert.Fail("Too much data being read");
                                 return l;
                             });
            var buf = new SlidingStreamBuffer(streamMock.Object, window, window);
            buf.Warmup();
            buf.MoveForward(window + 1);
            buf.MoveForward(1);
        }

        [TestMethod]
        public void MoveForward_pulls_data_to_the_correct_offset_when_less_than_windowSize_bytes_remain()
        {
            int window = 100;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            bool warmupFillComplete = false;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) =>
                             {
                                 if (warmupFillComplete && o != window - 1) Assert.Fail("Wrong offset being read to");
                                 warmupFillComplete = true;
                                 return l;
                             });
            var buf = new SlidingStreamBuffer(streamMock.Object, window, window);
            buf.Warmup();
            buf.MoveForward(window + 1);
        }

        [TestMethod]
        public void MoveForward_shifts_index_for_byte_retrieval()
        {
            int window = 100;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock)
                .Returns((byte[] s, int o, int l) =>
                             {
                                 int actualLength = MathEx.Bounded(0, Math.Min(l, buffer.Length), buffer.Length);
                                 Array.Copy(buffer, 0, s, o, actualLength);
                                 return actualLength;
                             });
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            Assert.AreEqual(buffer[5], buf[5]);
            buf.MoveForward(1);
            buf.MoveForward(3);
            Assert.AreEqual(buffer[9], buf[5]);
        }

        [TestMethod]
        public void MoveForward_stops_at_end_of_stream()
        {
            int window = 10;
            var buffer = new byte[window*2];
            for (int i = 0; i < buffer.Length; ++i) buffer[i] = (byte) (200 - i);
            var buf = new SlidingStreamBuffer(new MemoryStream(buffer), window);
            buf.Warmup();
            buf.MoveForward(window + 1);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void MoveForward_throws_for_negative_size()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => l);
            var buf = new SlidingStreamBuffer(streamMock.Object, window, 0);
            buf.Warmup();
            buf.MoveForward(-1);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentException))]
        public void MoveForward_throws_for_zero_size()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            SetupMockStreamRead(streamMock).Returns((byte[] s, int o, int l) => l);
            var buf = new SlidingStreamBuffer(streamMock.Object, window, 0);
            buf.Warmup();
            buf.MoveForward(0);
        }

        [TestMethod]
        public void Warmup_pulls_atleast_windowSize_data_from_stream()
        {
            int window = 100;
            var streamMock = new Mock<MemoryStream>(MockBehavior.Strict);
            streamMock.Setup(x =>
                             x.Read(
                                 It.IsAny<byte[]>(),
                                 It.IsAny<int>(),
                                 It.Is<int>(i => i >= window)))
                .Returns((byte[] s, int o, int l) => l);
            var buf = new SlidingStreamBuffer(streamMock.Object, window);
            buf.Warmup();
            streamMock.VerifyAll();
        }

        #endregion

        #region Methods: private

        private ISetup<MemoryStream, int> SetupMockStreamRead(Mock<MemoryStream> streamMock)
        {
            return streamMock.Setup(x =>
                                    x.Read(
                                        It.IsAny<byte[]>(),
                                        It.IsAny<int>(),
                                        It.IsAny<int>()));
        }

        private static void VerifyBufferPartialEquality(byte[] largeBuffer, byte[] smallBuffer)
        {
            if (largeBuffer.Length < smallBuffer.Length)
                throw new ArgumentException("Small buffer larger than large buffer");
            for (int i = 0; i < smallBuffer.Length; ++i)
                if (smallBuffer[i] != largeBuffer[i]) Assert.Fail("Data mismatch");
        }

        #endregion
    }
}