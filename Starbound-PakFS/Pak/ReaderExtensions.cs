/* WARNING:
 *   THIS FILE IS NOT LICENSED UNDER THE PROJECT LICENSE.
 *   PLEASE CONTACT KAWA IF YOU'RE PLANNING TO USE THIS CODE. CONSIDER THIS ALL RIGHTS RESERVED.
 * 
 * Special thanks to Kawa for letting me use this code.
 * http://helmet.kafuka.org
 */

using System;
using System.IO;

namespace PakFS
{
    /// <summary>
    /// Extensions for the BinaryReader class to read Pak data.
    /// These have only been tested on an Intel system (little-endian byte order).
    /// </summary>
    public static class ReaderExtensions
    {
        /// <summary>
        /// Reads an UInt16 from the binary reader.
        /// </summary>
        public static ushort ReadMotoInt16(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);

            ConditionalReverse(bytes);

            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        /// Reads an UInt32 from the binary reader and advances the position by 4 bytes.
        /// </summary>
        public static uint ReadMotoInt32(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);

            ConditionalReverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary>
        /// Reads an UInt64 from the binary reader and advances the position by 8 bytes.
        /// </summary>
        public static ulong ReadMotoInt64(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);

            ConditionalReverse(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

        /// <summary>
        /// Reads a variable length quantity unsigned number from the binary reader and advances the position to the end of the number.
        /// </summary>
        public static ulong ReadVLQUnsigned(this BinaryReader reader)
        {
            ulong x = 0;
            for (var i = 0; i < 10; ++i)
            {
                var oct = reader.ReadByte();
                x = (ulong)(x << 7) | (ulong)((ulong)oct & 127);
                if ((oct & 128) == 0)
                    return x;
            }
            throw new Exception("Failed to read VLQ.");
        }

        /// <summary>
        /// Reads a floating point number from the binary reader and advances the position by 8 bytes.
        /// </summary>
        public static double ReadMotoDouble(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);

            ConditionalReverse(bytes);

            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Reads a variable length quantity signed number from the binary reader and advances the position to the end of the number.
        /// </summary>
        public static long ReadVLQSigned(this BinaryReader reader)
        {
            ulong source = ReadVLQUnsigned(reader);
            bool negative = (source & 1) == 1;
            if (negative)
                return -(long)(source >> 1) - 1;
            else
                return (long)(source >> 1);
        }

        /// <summary>
        /// Reads a length-prefixed string from the reader and advances the position to the end of the string.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static string ReadProperString(this BinaryReader reader)
        {
            var len = (int)reader.ReadVLQUnsigned();
            var bytes = reader.ReadBytes(len);
            using (var str = new BinaryReader(new MemoryStream(bytes)))
            {
                return new string(str.ReadChars((int)str.BaseStream.Length));
            }
        }

        /// <summary>
        /// Conditionally reverses the elements in the array if <see cref="BitConverter.IsLittleEndian"/> is true.
        /// This changes big-endian data into little-endian data and vice versa.
        /// </summary>
        /// <param name="array"></param>
        private static void ConditionalReverse(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(array);
            }
        }
    }
}
