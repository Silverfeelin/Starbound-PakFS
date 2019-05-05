/* WARNING:
 *   THIS FILE IS NOT LICENSED UNDER THE PROJECT LICENSE.
 *   PLEASE CONTACT KAWA IF YOU'RE PLANNING TO USE THIS CODE. CONSIDER THIS ALL RIGHTS RESERVED.
 * 
 * Special thanks to Kawa for letting me use this code.
 * http://helmet.kafuka.org
 */

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PakFS
{
    /// <summary>
    /// Class used to read and parse pak files. Code provided by Kawa.
    /// Find more of Kawa's stuff over at http://helmet.kafuka.org/.
    /// </summary>
    public class PakReader
    {
        private static readonly CultureInfo CULTURE = CultureInfo.InvariantCulture;

        /// <summary>
        /// Creates a new pak reader, which sets the current culture info to a culture-independent one.
        /// .. I don't really know what that's being used for.
        /// </summary>
        public PakReader()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CULTURE;
        }

        /// <summary>
        /// Reads the pak file from the reader.
        /// Reader stream position does not matter.
        /// </summary>
        /// <param name="reader">Opened binary reader.</param>
        /// <returns>Data in pak file.</returns>
        public PakData Read(BinaryReader reader)
        {
            // Read index
            JObject metadata = ReadIndex(reader);

            // Find items
            var itemList = FindItems(reader);

            PakData pak = new PakData();
            pak.Metadata = metadata;
            pak.Items = itemList;

            return pak;
        }

        /// <summary>
        /// Reads the index (metadata) from the reader.
        /// After reading, the reader position will be at the end of the index (start of item index).
        /// </summary>
        /// <param name="reader">Reader to read index from.</param>
        /// <returns>Index.</returns>
        public JObject ReadIndex(BinaryReader reader)
        {
            JObject metadata = new JObject();

            // Find index
            reader.BaseStream.Seek(0xC, SeekOrigin.Begin);
            var indexOffset = reader.ReadMotoInt32();

            // Build index
            reader.BaseStream.Seek(indexOffset, SeekOrigin.Begin);
            var indexHeader = new string(reader.ReadChars(5));
            if (indexHeader != "INDEX")
            {
                throw new Exception("Expected an index.");
            }

            // Count items in index
            var indexItems = reader.ReadVLQUnsigned();
            while (indexItems-- > 0)
            {
                var key = reader.ReadProperString();
                var typeByte = reader.ReadByte();

                switch (TypeHelper.GetType(typeByte))
                {
                    default:
                        throw new ArgumentException($"Pak metadata contained unknown data type for key {key}.");
                    case ValueType.Null:
                        throw new ArgumentException($"Pak metadata contained a null value for key {key}.");
                    case ValueType.Double:
                        metadata[key] = reader.ReadMotoDouble();
                        break;
                    case ValueType.Boolean:
                        throw new ArgumentException($"Pak metadata contained a boolean value for key {key}.");
                    case ValueType.SignedNumber:
                        metadata[key] = reader.ReadVLQSigned();
                        break;
                    case ValueType.String:
                        metadata[key] = reader.ReadProperString();
                        break;
                    case ValueType.Array:
                        {
                            var arrayItems = (int)reader.ReadVLQUnsigned();
                            var array = new JArray();
                            while (arrayItems-- > 0)
                            {
                                var itemType = TypeHelper.GetType(reader.ReadByte());
                                if (itemType != ValueType.String)
                                {
                                    throw new ArgumentException($"Pak metadata array with key {key} contained an item that's not a string.");
                                }
                                var val = reader.ReadProperString();
                                array.Add(val);
                            }
                            metadata[key] = array;
                        }
                        break;
                    case ValueType.Object:
                        throw new ArgumentException($"Pak metadata contained an object for key {key}.");
                }
            }

            return metadata;
        }

        /// <summary>
        /// Indexes all items ('files') for further reading.
        /// 
        /// Stream position should be correct (positioned at start of item index, which is right after the INDEX).
        /// </summary>
        public List<PakItem> FindItems(BinaryReader reader)
        {
            var items = new List<PakItem>();

            var fileCount = reader.ReadVLQUnsigned();
            for (ulong i = 0; i < fileCount; i++)
            {
                var name = reader.ReadProperString();
                var offset = reader.ReadMotoInt64();
                var length = reader.ReadMotoInt64();

                items.Add(new PakItem
                {
                    Path = name,
                    Offset = offset,
                    Length = length
                });
            }

            return items;
        }

        /// <summary>
        /// Reads a pak item, returning the item data.
        /// </summary>
        public static byte[] ReadItem(BinaryReader reader, PakItem item)
        {
            reader.BaseStream.Seek((long)item.Offset, SeekOrigin.Begin);
            byte[] bytes = reader.ReadBytes((int)item.Length);

            return bytes;
        }

        /// <summary>
        /// Type of value based on type byte.
        /// </summary>
        internal enum ValueType : byte
        {
            Undefined = 0,
            Null = 1,
            Double = 2,
            Boolean = 3,
            SignedNumber = 4,
            String = 5,
            Array = 6,
            Object = 7
        }

        /// <summary>
        /// Converts type from byte to enum.
        /// </summary>
        internal static class TypeHelper
        {
            internal static ValueType GetType(byte typeByte)
            {
                return (ValueType)typeByte;
            }
        }
    }
}
