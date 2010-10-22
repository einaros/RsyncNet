namespace RsyncNet.Helpers
{
    using System;
    using System.IO;

    public class StreamUtils
    {
        #region Methods: public

        public static int ReadStreamInt(Stream stream)
        {
            var uintBuffer = new byte[4];
            if (stream.Read(uintBuffer, 0, 4) != 4)
            {
                throw new IOException("Not enough data available on stream");
            }
            return BitConverter.ToInt32(uintBuffer, 0);
        }

        public static long ReadStreamLong(Stream stream)
        {
            var uintBuffer = new byte[8];
            if (stream.Read(uintBuffer, 0, 8) != 8)
            {
                throw new IOException("Not enough data available on stream");
            }
            return BitConverter.ToInt64(uintBuffer, 0);
        }

        public static uint ReadStreamUInt(Stream stream)
        {
            var uintBuffer = new byte[4];
            if (stream.Read(uintBuffer, 0, 4) != 4)
            {
                throw new IOException("Not enough data available on stream");
            }
            return BitConverter.ToUInt32(uintBuffer, 0);
        }

        public static ulong ReadStreamULong(Stream stream)
        {
            var uintBuffer = new byte[8];
            if (stream.Read(uintBuffer, 0, 8) != 8)
            {
                throw new IOException("Not enough data available on stream");
            }
            return BitConverter.ToUInt64(uintBuffer, 0);
        }

        #endregion
    }
}