using System;
using System.IO;

namespace PakFS
{
    public class Program
    {
        private static string filePath;
        private static string targetRoot;
        
        private static bool keep = false;

        static void Main(string[] args)
        {
            // Validate arg
            if (args.Length == 0)
            {
                Console.WriteLine("Can't virtualize pak file. No path supplied.");
                WaitAndExit();
                return;
            }

            filePath = args[0];
            // Validate file
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Can't virtualize pak file. File not found.");
                WaitAndExit();
                return;
            }

            Console.SetWindowSize(64, 4);
            Console.SetBufferSize(64, 4);

            // PakFile.pak => _PakFile_pak (_ to prevent Starbound from loading it, _pak to prevent name collisions).
            targetRoot = Path.Combine(Path.GetDirectoryName(filePath), $"_{Path.GetFileNameWithoutExtension(filePath)}_pak");

            // Start ProjFS provider.
            using (var provider = new PakProvider(filePath, targetRoot))
            {
                provider.StartVirtualizing();

                Console.WriteLine("Provider is running...");
                Console.WriteLine("Press any key to stop. Press X to stop and delete files.");

                // Wait for exit command.
                var k = Console.ReadKey(true);
                keep = k.Key != ConsoleKey.X;
            }
            
            if (!keep)
            {
                try
                {
                    DeleteDirectory(targetRoot, true);
                }
                catch {} // Probably a permission error.
            }
        }

        private static void WaitAndExit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            Environment.Exit(0);
        }

        private static void DeleteDirectory(string path, bool recursive = false)
        {
            if (recursive)
            {
                foreach (var item in Directory.EnumerateDirectories(path))
                {
                    DeleteDirectory(item, recursive);
                }
            }

            // This doesn't work by itself.. recursive aint recursive.
            Directory.Delete(path, recursive);
        }
    }
}
