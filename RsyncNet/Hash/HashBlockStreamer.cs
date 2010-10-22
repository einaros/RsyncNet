namespace RsyncNet.Hash
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Helpers;

    public class HashBlockStreamer
    {
        #region Methods: public

        public static HashBlock[] Destream(Stream inputStream)
        {
            uint count = StreamUtils.ReadStreamUInt(inputStream);
            var hashBlocks = new HashBlock[count];
            for (int i = 0; i < count; ++i)
            {
                hashBlocks[i] = new HashBlock {Hash = new byte[16]};
                inputStream.Read(hashBlocks[i].Hash, 0, 16);
                hashBlocks[i].Length = StreamUtils.ReadStreamUInt(inputStream);
                hashBlocks[i].Offset = StreamUtils.ReadStreamUInt(inputStream);
                hashBlocks[i].Checksum = StreamUtils.ReadStreamUInt(inputStream);
            }
            return hashBlocks;
        }

        public static void Stream(IEnumerable<HashBlock> hashBlocks, Stream outputStream)
        {
            outputStream.Write(BitConverter.GetBytes((uint) hashBlocks.Count()), 0, 4);
            foreach (HashBlock block in hashBlocks)
            {
                outputStream.Write(block.Hash, 0, 16);
                outputStream.Write(BitConverter.GetBytes(block.Length), 0, 4);
                outputStream.Write(BitConverter.GetBytes(block.Offset), 0, 4);
                outputStream.Write(BitConverter.GetBytes(block.Checksum), 0, 4);
            }
        }

        #endregion
    }
}