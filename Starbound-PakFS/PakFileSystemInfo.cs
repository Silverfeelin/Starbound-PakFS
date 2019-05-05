using System.IO;

namespace PakFS
{
    /// <summary>
    /// Represents a file or directory in a pak file.
    /// </summary>
    public abstract class PakFileSystemInfo
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public FileAttributes Attributes { get; set; }
    }

    /// <summary>
    /// Represents a file (asset) in a pak file.
    /// </summary>
    public class PakFileInfo : PakFileSystemInfo
    {
        public PakFileInfo()
        {
            Attributes = FileAttributes.Normal;
        }
    }

    /// <summary>
    /// Represents a directory in a pak file.
    /// </summary>
    public class PakDirectoryInfo : PakFileSystemInfo
    {
        public PakDirectoryInfo()
        {
            Attributes = FileAttributes.Directory;
        }
    }
}

