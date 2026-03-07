using System;

namespace antivirus.Legacy
{
    public static class Scanner
    {
        public static bool Scan(string path) { Console.WriteLine($"Scanning: {path}"); return true; }
        public static void ReadMBR() { Console.WriteLine("Reading MBR..."); }
        public static bool EnsureClamAVInstalled() { return true; }
        public static bool EnsureClamAVDefinitionsExist() { return true; }
        public static void EnsureBrowserInstallers() { }
    }
}
