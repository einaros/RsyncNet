namespace RsyncNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using Delta;
    using Hash;
    using Libraries;

    internal class Program
    {
        #region Methods: private

        private static void Main(string[] args)
        {
            // Get a list of files to send
            // Request a signature of the file from the remote host
            // Calculate the deltas
            // Send the deltas to the server
            int blockSize = 512;
            IEnumerable<HashBlock> hashBlocksFromReceiver = new HashBlockGenerator(new RollingChecksum(),
                                                                       new HashAlgorithmWrapper<MD5>(MD5.Create()),
                                                                       blockSize)
                                                                       .ProcessStream(File.Open("dest.bmp", FileMode.Open));

            //var destBlocksSerialized = new MemoryStream();
            //HashBlockStreamer.Stream(destBlocks, destBlocksSerialized);
            //destBlocksSerialized.Seek(0, SeekOrigin.Begin);
            //Console.Out.WriteLine("Size of stream from remote to local: {0}.\nNumber of blocks: {1}.\nSize per block: {2}.\nIdeal size: {3}\nOverhead: {4}%",
            //    destBlocksSerialized.Length,
            //    destBlocks.Count(),
            //    destBlocksSerialized.Length / destBlocks.Count(),
            //    28 * destBlocks.Count(),
            //    100 - (28.0f * destBlocks.Count()) / destBlocksSerialized.Length * 100);
            //destBlocks = HashBlockStreamer.Destream(destBlocksSerialized);
            //Console.Out.WriteLine("--");

            var deltaGen = new DeltaGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()));
            deltaGen.Initialize(blockSize, hashBlocksFromReceiver);
            using (var fileStream = File.Open("source.bmp", FileMode.Open))
            {
                var deltas = deltaGen.GetDeltas(fileStream);
                deltaGen.Statistics.Dump();
                fileStream.Seek(0, SeekOrigin.Begin);
                var streamer = new DeltaStreamer();
                var outputStream = new MemoryStream();
                streamer.Send(deltas, fileStream, outputStream);
                Console.Out.WriteLine(outputStream.Length);
            }

            //using (var deltaStream = new MemoryStream())
            //{
            //    //deltaCalc.GetDeltaStream("source.bmp", deltaStream);
            //    deltaCalc.Statistics.Dump();
            //    deltaStream.Seek(0, SeekOrigin.Begin);
            //    using (FileStream output = File.OpenWrite("reconstructed.bmp"))
            //    {
            //        DataReconstructor.Reconstruct(deltaStream, File.OpenRead("dest.bmp"), output);
            //        output.Close();
            //    }
            //}
        }

        #endregion
    }
}