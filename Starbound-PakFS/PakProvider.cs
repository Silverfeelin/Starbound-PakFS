// Code is based on ProjFS-Managed-API codebase by Microsoft Corporation, licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Windows.ProjFS;

namespace PakFS
{
    /// <summary>
    /// ProjFS provider for Starbound pak files.
    /// </summary>
    public class PakProvider : IRequiredCallbacks, IDisposable
    {
        private readonly string filePath;
        private readonly string targetRoot;

        private readonly VirtualizationInstance virtualizationInstance;
        private readonly ConcurrentDictionary<Guid, FileEnumeration> enumerations;

        private readonly FileStream fileStream;
        private readonly BinaryReader binaryReader;
        
        // Directory = DirectoryFiles
        private readonly Dictionary<string, List<PakItem>> fileTree;
        private readonly byte[] metadata;

        public PakProvider(string filePath, string targetRoot)
        {
            this.filePath = filePath;
            this.targetRoot = targetRoot;
            
            // Read pak file
            fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            binaryReader = new BinaryReader(fileStream);

            var reader = new PakReader();
            var metadata = reader.ReadIndex(binaryReader);
            this.metadata = Encoding.UTF8.GetBytes(metadata.ToString(Newtonsoft.Json.Formatting.Indented));
            var files = reader.FindItems(binaryReader);
            fileTree = new Dictionary<string, List<PakItem>>();

            // Map assets
            foreach (var file in files)
            {
                var dir = file.Path.Substring(0, file.Path.LastIndexOf("/") + 1);
                if (!fileTree.ContainsKey(dir)) fileTree[dir] = new List<PakItem>();
                fileTree[dir].Add(file);
            }
            
            // Set up virtualization
            var notificationMappings = new List<NotificationMapping> { new NotificationMapping(NotificationType.FileOpened | NotificationType.PreDelete | NotificationType.PreRename, string.Empty) };            
            virtualizationInstance = new VirtualizationInstance(targetRoot, 0, 0, false, notificationMappings);
            // Disallow delete/rename.
            virtualizationInstance.OnNotifyPreDelete = new NotifyPreDeleteCallback((a, b, c, d) => false);
            virtualizationInstance.OnNotifyPreRename = new NotifyPreRenameCallback((a, b, c, d) => false);            
            enumerations = new ConcurrentDictionary<Guid, FileEnumeration>();
        }

        /// <summary>
        /// Start virtualizing pak file.
        /// </summary>
        public bool StartVirtualizing() => virtualizationInstance.StartVirtualizing(this) == HResult.Ok;

        /// <summary>
        /// Stops virtualizing pak file. Files that have already been 
        /// </summary>
        public void StopVirtualizing() =>  virtualizationInstance.StopVirtualizing();

        public void Dispose() => StopVirtualizing();

        /// <summary>
        /// Converts a relative virtual path to an asset path.
        /// </summary>
        /// <param name="path">Relative path, i.e. items\\armors\\myChest.chest</param>
        /// <returns>Asset path, i.e. /items/armors/myChest.chest</returns>
        protected string GetAssetPath(string path)
        {
            if (!path.StartsWith("/")) path = "/" + path;
            return path.Replace("\\", "/");
        }
        
        /// <summary>
        /// Enmerates over the directories and files at the given asset path.
        /// </summary>
        protected IEnumerable<PakFileSystemInfo> EnumerateDirectory(string relativePath)
        {
            var fullPath = GetAssetPath(relativePath);
            var subDirs = new HashSet<string>();

            // Subdirectories
            foreach (var directory in fileTree.Where(f => f.Key.Length > fullPath.Length && f.Key.StartsWith(fullPath)))
            {
                var i = directory.Key.IndexOf("/", fullPath.Length + 1);

                var subDir = i == -1 ? directory.Key.Substring(fullPath.Length) : directory.Key.Substring(fullPath.Length, i - fullPath.Length);
                if (subDir.StartsWith("/")) subDir = subDir.Substring(1);
                if (string.IsNullOrWhiteSpace(subDir) || !subDirs.Add(subDir)) continue;

                // Subdir
                yield return new PakDirectoryInfo
                {
                    Name = subDir,
                    Size = 0
                };
            }
            
            // Files
            if (!fullPath.EndsWith("/")) fullPath += "/";

            // Metadata
            if (fullPath == "/")
            {
                yield return GetSystemInfo("/_metadata");
            }

            if (fileTree.ContainsKey(fullPath))
            {
                foreach (var item in fileTree[fullPath])
                {
                    // File
                    yield return new PakFileInfo
                    {
                        Name = item.Path.Substring(item.Path.LastIndexOf("/") + 1),
                        Size = Convert.ToInt64(item.Length),
                        Attributes = FileAttributes.ReadOnly
                    };
                }
            }
        }

        protected byte[] ReadFile(string assetPath)
        {
            if (assetPath == "/_metadata") return this.metadata;

            // Get file
            var folder = assetPath.Substring(0, assetPath.LastIndexOf("/") + 1);
            if (!fileTree.ContainsKey(folder)) return null;
            var file = fileTree[folder].Where(f => f.Path == assetPath).FirstOrDefault();
            if (file == null) return null;

            // Read data
            var data = PakReader.ReadItem(binaryReader, file);
            return data;
        }
        
        private PakFileSystemInfo GetSystemInfo(string assetPath)
        {
            // Metadata
            if (assetPath == "/_metadata")
            {
                return new PakFileInfo
                {
                    Name = "_metadata",
                    Size = metadata.Length,
                    Attributes = FileAttributes.ReadOnly
                };
            }

            // Directory
            bool isDirectory = !fileTree.FirstOrDefault(f => f.Key.Contains(assetPath)).Equals(default(KeyValuePair<string, List<PakItem>>));
            if (isDirectory)
            {
                return new PakDirectoryInfo
                {
                    Name = assetPath.Substring(assetPath.LastIndexOf("/") + 1),
                    Size = 0
                };
            }

            // File
            var i = assetPath.LastIndexOf("/");
            var dir = i == -1 ? "/" : assetPath.Substring(0, assetPath.LastIndexOf("/") + 1);
            if (fileTree.ContainsKey(dir))
            {
                var file = fileTree[dir].Where(f => f.Path == assetPath).FirstOrDefault();
                if (file != null)
                {
                    return new PakFileInfo
                    {
                        Name = assetPath.Substring(assetPath.LastIndexOf("/") + 1),
                        Size = Convert.ToInt64(file.Length),
                        Attributes = FileAttributes.ReadOnly
                    };
                }
            }

            return null;
        }

        #region IRequiredCallbacks
        
        public HResult StartDirectoryEnumerationCallback(int commandId, Guid enumerationId, string relativePath, uint triggeringProcessId, string triggeringProcessImageFileName)
        {
            var enumerable = EnumerateDirectory(relativePath)
                .OrderBy(
                    file => file.Name,
                    Comparer<string>.Create((a, b) => Utils.FileNameCompare(a, b))
                );
            var enumeration = new FileEnumeration(enumerable);
            enumeration.MoveNext(); // Start enumeration.

            var added = enumerations.TryAdd(enumerationId, enumeration);
            return added ? HResult.Ok : HResult.InternalError;            
        }
        
        public HResult GetDirectoryEnumerationCallback(int commandId, Guid enumerationId, string filterFileName, bool restartScan, IDirectoryEnumerationResults enumResult)
        {
            if (!enumerations.TryGetValue(enumerationId, out FileEnumeration enumeration))
                return HResult.InternalError;

            if (restartScan)
                enumeration.Reset();

            if (restartScan || enumeration.Filter == null)
                enumeration.Filter = filterFileName ?? "";
            
            enumeration.MoveNext();

            bool added = false;
            while (enumeration.Valid)
            {
                PakFileSystemInfo fileInfo = enumeration.Current;

                if (enumResult.Add(
                    fileName: string.IsNullOrWhiteSpace(fileInfo.Name) ? "REPLACEME" : fileInfo.Name,
                    fileSize: fileInfo.Size,
                    isDirectory: fileInfo is PakDirectoryInfo,
                    fileAttributes: fileInfo.Attributes,
                    creationTime: DateTime.Now,
                    lastAccessTime: DateTime.Now,
                    lastWriteTime: DateTime.Now,
                    changeTime: DateTime.Now))
                {
                    added = true;
                    enumeration.MoveNext();
                }
                else
                {
                    return added ? HResult.Ok : HResult.InsufficientBuffer;
                }
            }
            
            return HResult.Ok;
        }
        
        public HResult EndDirectoryEnumerationCallback(Guid enumerationId)
        {
            var removed = enumerations.TryRemove(enumerationId, out FileEnumeration enumeration);
            return removed ? HResult.Ok : HResult.InternalError;
        }

        /// <summary>
        /// Requested file info for asset that hasn't been loaded yet.
        /// </summary>
        public HResult GetPlaceholderInfoCallback(int commandId, string path, uint triggeringProcessId, string triggeringProcessImageFileName)
        {
            var assetPath = GetAssetPath(path);
            PakFileSystemInfo fileInfo = GetSystemInfo(assetPath);
            if (fileInfo == null)
                return HResult.FileNotFound;
            
            return virtualizationInstance.WritePlaceholderInfo(
                Path.Combine(Path.GetDirectoryName(path), fileInfo.Name),
                DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now,
                fileInfo.Attributes, fileInfo.Size, fileInfo is PakDirectoryInfo,
                null, null // Don't think these identifiers are necessary...
            );
        }

        /// <summary>
        /// Requested file data. Read the file and write it to ProjFS.
        /// </summary>
        public HResult GetFileDataCallback(int commandId, string relativePath, ulong byteOffset, uint length, Guid dataStreamId, byte[] contentId, byte[] providerId, uint triggeringProcessId, string triggeringProcessImageFileName)
        {
            var assetPath = GetAssetPath(relativePath);
            var pakFileSystemInfo = GetSystemInfo(assetPath);
            if (pakFileSystemInfo == null) return HResult.FileNotFound;
            
            uint dataSize = Convert.ToUInt32(pakFileSystemInfo.Size);

            try
            {
                using (IWriteBuffer writeBuffer = virtualizationInstance.CreateWriteBuffer(dataSize))
                {
                    var data = ReadFile(assetPath);
                    if (data == null) return HResult.FileNotFound;
                    writeBuffer.Stream.Write(data, 0, data.Length);
                    HResult writeResult = virtualizationInstance.WriteFileData(dataStreamId, writeBuffer, 0, dataSize);
                    return writeResult;
                }
            }
            catch (OutOfMemoryException)
            {
                return HResult.OutOfMemory;
            }
            catch (Exception)
            {
                return HResult.InternalError;
            }
        }

        #endregion
    }
}
