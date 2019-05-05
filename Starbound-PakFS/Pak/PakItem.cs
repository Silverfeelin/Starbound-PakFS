namespace PakFS
{
    /// <summary>
    /// Class that holds asset metadata.
    /// </summary>
    public class PakItem
    {
        /// <summary>
        /// Relative asset path, starting from the root of the pak file.
        /// I.e. /scripts/myScript.lua
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Stream offset for the first byte of this asset.
        /// </summary>
        public ulong Offset { get; set; }

        /// <summary>
        /// Length of this asset in bytes.
        /// </summary>
        public ulong Length { get; set; }
    }
}
