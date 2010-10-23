using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleApp
{
    using System.IO;
    using System.Security.Cryptography;
    using RsyncNet.Delta;
    using RsyncNet.Hash;
    using RsyncNet.Libraries;

    class Program
    {
        static void Main(string[] args)
        {
            int blockSize = 512;

            // Compute hashes
            IEnumerable<HashBlock> hashBlocksFromReceiver;
            using (FileStream sourceStream = File.Open("../../dest.bmp", FileMode.Open))
            {
                hashBlocksFromReceiver = new HashBlockGenerator(new RollingChecksum(),
                                                                new HashAlgorithmWrapper<MD5>(MD5.Create()),
                                                                blockSize).ProcessStream(sourceStream).ToArray();
            }

            // Stream the hash blocks
            var hashBlockStream = new MemoryStream();
            HashBlockStreamer.Stream(hashBlocksFromReceiver, hashBlockStream);

            // Receive the hash block stream
            hashBlockStream.Seek(0, SeekOrigin.Begin);
            Console.Out.WriteLine("Hash block stream length: {0}", hashBlockStream.Length);
            hashBlocksFromReceiver = HashBlockStreamer.Destream(hashBlockStream);

            // Compute deltas
            var deltaStream = new MemoryStream();
            using (FileStream fileStream = File.Open("../../source.bmp", FileMode.Open))
            {
                var deltaGen = new DeltaGenerator(new RollingChecksum(), new HashAlgorithmWrapper<MD5>(MD5.Create()));
                deltaGen.Initialize(blockSize, hashBlocksFromReceiver);
                IEnumerable<IDelta> deltas = deltaGen.GetDeltas(fileStream);
                deltaGen.Statistics.Dump();
                fileStream.Seek(0, SeekOrigin.Begin);
                var streamer = new DeltaStreamer();
                streamer.Send(deltas, fileStream, deltaStream);
                Console.Out.WriteLine("Delta stream length: {0}", deltaStream.Length);
            }

            // Rewind the delta stream (obviously wouldn't apply from a network pipe)
            deltaStream.Seek(0, SeekOrigin.Begin);

            // Reconstruct file
            using (FileStream sourceStream = File.Open("../../dest.bmp", FileMode.Open))
            {
                using (FileStream outStream = File.Open("../../reconstructed.bmp", FileMode.Create))
                {
                    var streamer = new DeltaStreamer();
                    streamer.Receive(deltaStream, sourceStream, outStream);
                    outStream.Close();
                }
            }
        }
    }
}
