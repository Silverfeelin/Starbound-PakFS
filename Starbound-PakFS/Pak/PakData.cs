using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace PakFS
{
    /// <summary>
    /// Data container for pak data.
    /// Can be used to read the pak metadata or further parse items in the pak.
    /// </summary>
    public class PakData
    {
        /// <summary>
        /// Parsed pak metadata.
        /// This object reflects the _metadata JSON for this pak.
        /// </summary>
        public JObject Metadata { get; set; }

        /// <summary>
        /// Pak items (files).
        /// To read the data for each file, use a BinaryReader together with <see cref="PakItem.Offset"/> and <see cref="PakItem.Length"/>.
        /// </summary>
        public List<PakItem> Items { get; set; }
    }
}
