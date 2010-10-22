namespace RsyncNet.Hash
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using Libraries;

    public class HashBlockGenerator
    {
        public IRollingChecksum ChecksumProvider { get; set; }
        public IHashAlgorithm HashAlgorithm { get; set; }
        private readonly int _blockSize;

        public HashBlockGenerator(IRollingChecksum checksumProvider, IHashAlgorithm hashProvider, int blockSize)
        {
            if (checksumProvider == null) throw new ArgumentNullException("checksumProvider");
            if (hashProvider == null) throw new ArgumentNullException("hashProvider");
            if (blockSize <= 0) throw new ArgumentException("blockSize must be greater than zero");
            ChecksumProvider = checksumProvider;
            HashAlgorithm = hashProvider;
            _blockSize = blockSize;
        }

        #region Methods: public

        public IEnumerable<HashBlock> ProcessStream(Stream inputStream)
        {
            if (inputStream == null) throw new ArgumentNullException("inputStream");
            MD5 md5Hasher = MD5.Create();
            var signatureGenerator = new RollingChecksum();
            int read;
            var buffer = new byte[_blockSize];
            long offset = 0;
            while ((read = inputStream.Read(buffer, 0, _blockSize)) > 0)
            {
                signatureGenerator.ProcessBlock(buffer, 0, (uint) read);
                yield return new HashBlock
                                 {
                                     Hash = md5Hasher.ComputeHash(buffer, 0, read),
                                     Checksum = signatureGenerator.Value,
                                     Offset = (uint) offset,
                                     Length = (uint) read
                                 };
                offset += read;
            }
        }

        #endregion
    }
}