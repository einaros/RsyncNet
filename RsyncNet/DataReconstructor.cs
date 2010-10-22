namespace RsyncNet
{
    using System.IO;

    public class DataReconstructor
    {
        #region Methods: public

        public static void Reconstruct(Stream deltaStream, Stream sourceStream, Stream outputStream)
        {
            /*DeltaStreamProtocol.ProcessDeltaStream(deltaStream,
                                                       (destOffset, length) =>
                                                           {
                                                               var buffer = new byte[length];
                                                               deltaStream.Read(buffer, 0, (int) length);
                                                               outputStream.Write(buffer, 0, (int) length);
                                                           },
                                                       (destOffset, sourceOffset, length) =>
                                                           {
                                                               var buffer = new byte[length];
                                                               sourceStream.Seek(sourceOffset, SeekOrigin.Begin);
                                                               sourceStream.Read(buffer, 0, (int) length);
                                                               outputStream.Write(buffer, 0, (int) length);
                                                           });
             */
        }

        #endregion
    }
}