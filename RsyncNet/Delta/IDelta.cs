namespace RsyncNet.Delta
{
    using System.IO;

    public interface IDelta
    {
        #region Methods: public

        long Length { get; set; }
        long Offset { get; set; }

        #endregion
    }
}