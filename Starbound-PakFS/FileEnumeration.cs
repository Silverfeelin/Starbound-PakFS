using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Windows.ProjFS;

namespace PakFS
{
    internal class FileEnumeration: IEnumerator<PakFileSystemInfo>
    {
        private readonly IEnumerable<PakFileSystemInfo> files;
        private IEnumerator<PakFileSystemInfo> enumerator;

        public string Filter { get; set; }

        public FileEnumeration(IEnumerable<PakFileSystemInfo> files)
        {
            this.files = files;
            Reset();
        }

        /// <summary>
        /// Gets the current file, if any.
        /// For validity, see <see cref="Valid"/>.
        /// </summary>
        public PakFileSystemInfo Current => enumerator?.Current;
        
        [Obsolete] // IEnumerator implementation
        object IEnumerator.Current => enumerator?.Current;
        
        /// <summary>
        /// Returns a value indicating whether the current item lies within the enumerable files.
        /// False before the enumeration is started and after the enumeration ended.
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// Returns a value indicating whether the current item matches <see cref="Filter"/>.
        /// </summary>
        public bool IsMatch => string.IsNullOrWhiteSpace(Filter) || Utils.IsFileNameMatch(Current.Name, Filter);

        /// <summary>
        /// Advances the enumerator to the next valid file.
        /// </summary>
        /// <returns>True if the enumeration advanced to the next file, false if no valid files were found.</returns>
        public bool MoveNext()
        {
            do Valid = enumerator.MoveNext();
            while (Valid && !IsMatch);
            return Valid;
        }
        
        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            enumerator = null;
        }

        /// <summary>
        /// Resets the enumerator without advancing it.
        /// </summary>
        public void Reset()
        {
            enumerator = files.GetEnumerator();
        }
    }
}
