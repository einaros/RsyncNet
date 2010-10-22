namespace RsyncNet.Delta
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Helpers;

    public class DeltaStreamer
    {
        private int _streamChunkSize;

        public DeltaStreamer()
        {
            StreamChunkSize = 16384;
        }

        #region Properties, indexers, events and operators: public

        public int StreamChunkSize
        {
            get { return _streamChunkSize; }
            set
            {
                if (value <= 0) throw new ArgumentException("value must be a positive number greater than 0");
                _streamChunkSize = value;
            }
        }

        #endregion

        #region Methods: public

        public void Send(IEnumerable<IDelta> deltas, Stream inputStream, Stream outputStream)
        {
            if (deltas == null) throw new ArgumentNullException("deltas");
            if (deltas.Count() == 0) throw new ArgumentException("'deltas' must have one or more IDelta objects");
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            if (outputStream == null) throw new ArgumentNullException("outputStream");

            foreach (IDelta delta in deltas)
            {
                if (delta is ByteDelta)
                {
                    SendByteDelta(delta as ByteDelta, inputStream, outputStream);
                }
                else if (delta is CopyDelta)
                {
                    SendCopyDelta(delta as CopyDelta, inputStream, outputStream);
                }
            }
        }

        //public delegate void WriteNewBlockDelegate(int length);
        //public delegate void CopyBlockDelegate(long sourceOffset, int length);

        //public void Receive(Stream deltaStream, WriteNewBlockDelegate newBlockCallback, CopyBlockDelegate copyBlockCallback)
        /// <summary>
        /// Reconstructs remote data, given a delta stream and a random access / seekable input stream,
        /// all written to outputStream.
        /// </summary>
        /// <param name="deltaStream"></param>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        public void Receive(Stream deltaStream, Stream inputStream, Stream outputStream)
        {
            //int commandByte;
            //uint lengthThusFar = 0;
            //while ((commandByte = deltaStream.ReadByte()) != -1)
            //{
            //    if (commandByte == NEW_BLOCK_START_MARKER)
            //    {
            //        // delta format
            //        // N     dest     length   data
            //        // byte  uint32    uint32    bytes
            //        uint dest = StreamUtils.ReadStreamUInt(deltaStream);
            //        uint length = StreamUtils.ReadStreamUInt(deltaStream);
            //        lengthThusFar += length;
            //        //Console.Out.WriteLine("New block of length {0}. Total thus far: {1}.", length, lengthThusFar);
            //        WriteNewBlock(dest, length);
            //    }
            //    else if (commandByte == COPY_BLOCK_START_MARKER)
            //    {
            //        // delta format
            //        // C     dest     source   length
            //        // byte  uint32   uint32   uint32
            //        uint dest = StreamUtils.ReadStreamUInt(deltaStream);
            //        uint source = StreamUtils.ReadStreamUInt(deltaStream);
            //        uint length = StreamUtils.ReadStreamUInt(deltaStream);
            //        lengthThusFar += length;
            //        //Console.Out.WriteLine("Old block of length {0} from {1}. Total thus far: {2}.", length, source, lengthThusFar);
            //        CopyExistingBlock(dest, source, length);
            //    }
            //}
        }

        #endregion

        #region Methods: private

        private void SendByteDelta(ByteDelta delta, Stream inputStream, Stream outputStream)
        {
            outputStream.WriteByte(DeltaStreamConstants.NEW_BLOCK_START_MARKER);
            outputStream.Write(BitConverter.GetBytes(delta.Length), 0, sizeof(int));
            var buffer = new byte[delta.Length];
            inputStream.Seek(delta.Offset, SeekOrigin.Begin);
            long totalRead = 0;
            while (totalRead < delta.Length)
            {
                var toRead = (int) MathEx.Bounded(0, StreamChunkSize, delta.Length - totalRead);
                int readLength = inputStream.Read(buffer, 0, toRead);
                if (readLength == 0 && totalRead < delta.Length)
                    throw new IOException("Input stream offset out of bounds, or not enough data available");
                outputStream.Write(buffer, 0, readLength);
                totalRead += readLength;
            }
        }

        private void SendCopyDelta(CopyDelta delta, Stream inputStream, Stream outputStream)
        {
            if (inputStream.CanSeek == false) throw new IOException("inputStream not seekable");
            outputStream.WriteByte(DeltaStreamConstants.COPY_BLOCK_START_MARKER);
            outputStream.Write(BitConverter.GetBytes(delta.Offset), 0, sizeof(long));
            outputStream.Write(BitConverter.GetBytes(delta.Length), 0, sizeof(int));
            inputStream.Seek(delta.Length, SeekOrigin.Current);
        }

        #endregion

        #region Nested type: DeltaStreamConstants

        internal static class DeltaStreamConstants
        {
            #region Fields: public

            public static byte COPY_BLOCK_START_MARKER = (byte) 'C';
            public static byte NEW_BLOCK_START_MARKER = (byte) 'N';

            #endregion
        }

        #endregion
    }
}