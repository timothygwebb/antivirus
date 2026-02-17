using System;
using System.IO;
using System.Net;

namespace antivirus
{
    public static class ClamAVDefinitionsManager
    {
        private static readonly string[] DefinitionFiles = new string[] { "main.cvd", "daily.cvd", "bytecode.cvd" };
        // Use the same ClamAVDir as in Scanner.cs
        public static readonly string ClamAVDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ClamAV");

        // Checks for updates every 24 hours
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(24);
        private static DateTime _lastUpdate = DateTime.MinValue;

        public static void DownloadDefinitions()
        {
            Console.WriteLine("ClamAV definitions download failed. Please manually download main.cvd/.cld, daily.cvd/.cld, and bytecode.cvd/.cld from https://www.clamav.net/downloads and place them in the ClamAV directory.");
            Logger.LogError("ClamAV definitions download failed. Please manually download main.cvd/.cld, daily.cvd/.cld, and bytecode.cvd/.cld from https://www.clamav.net/downloads and place them in the ClamAV directory.", Array.Empty<object>());
        }

        public static bool DefinitionsExist()
        {
            foreach (var file in DefinitionFiles)
            {
                var localPath = Path.Combine(ClamAVDir, file);
                if (!File.Exists(localPath))
                    return false;
            }
            return true;
        }

        public static void EnsureDefinitionsUpToDate()
        {
            if (!DefinitionsExist() || DateTime.Now - _lastUpdate > UpdateInterval)
            {
                Logger.LogInfo("Updating ClamAV definitions...", Array.Empty<object>());
                DownloadDefinitions();
                _lastUpdate = DateTime.Now;
            }
        }
    }
}
