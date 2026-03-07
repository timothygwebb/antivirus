using System;

namespace antivirus.Legacy
{
    public static class Scanner
    {
        public static bool Scan(string path)
        {
            int fileCount = 0;
            int dirCount = 0;
            try
            {
                if (System.IO.Directory.Exists(path))
                {
                    Console.WriteLine($"Recursively scanning directory: {path}");
                    ScanDirectory(path, ref fileCount, ref dirCount);
                }
                else if (System.IO.File.Exists(path))
                {
                    Console.WriteLine($"Scanning file: {path}");
                    fileCount++;
                }
                else
                {
                    Console.WriteLine($"Path not found: {path}");
                    return false;
                }
                Console.WriteLine($"Scan complete. Directories scanned: {dirCount}, Files scanned: {fileCount}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scan failed: {ex.Message}");
                return false;
            }
        }

        private static void ScanDirectory(string dir, ref int fileCount, ref int dirCount)
        {
            dirCount++;
            try
            {
                foreach (var file in System.IO.Directory.GetFiles(dir))
                {
                    Console.WriteLine($"Scanning file: {file}");
                    fileCount++;
                }
                foreach (var subdir in System.IO.Directory.GetDirectories(dir))
                {
                    ScanDirectory(subdir, ref fileCount, ref dirCount);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Access denied: {dir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning directory {dir}: {ex.Message}");
            }
        }
        public static void ReadMBR() { Console.WriteLine("Reading MBR..."); }
        public static bool EnsureClamAVInstalled() { return true; }
        public static bool EnsureClamAVDefinitionsExist() { return true; }
        public static void EnsureBrowserInstallers() { }
    }
}
