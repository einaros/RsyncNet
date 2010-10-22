namespace RsyncNet.Helpers
{
    using System;
    using System.IO;

    public class StreamUtils
    {
        #region Methods: public

        public static uint ReadStreamUInt(Stream stream)
        {
            var uintBuffer = new byte[4];
            if (stream.Read(uintBuffer, 0, 4) != 4)
            {
                throw new IOException("Not enough data available on stream");
            }
            return BitConverter.ToUInt32(uintBuffer, 0);
        }

        #endregion
    }
}