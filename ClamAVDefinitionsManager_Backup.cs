using System;
using System.IO;
using System.Net;

namespace antivirus
{
    public static class ClamAVDefinitionsManager
    {
        private static readonly string[] DefinitionFiles = new string[] { "main.cvd", "daily.cvd", "bytecode.cvd" };
        public static readonly string LocalDefinitionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAVDefs");

        // Checks for updates every 24 hours
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(24);
        private static DateTime _lastUpdate = DateTime.MinValue;

        public static void DownloadDefinitions()
        {
            Console.WriteLine("ClamAV definitions download failed. Please manually download main.cvd, daily.cvd, and bytecode.cvd from https://www.clamav.net/downloads and place them in the ClamAVDefs directory.");
            Logger.LogError("ClamAV definitions download failed. Please manually download main.cvd, daily.cvd, and bytecode.cvd from https://www.clamav.net/downloads and place them in the ClamAVDefs directory.", new object[0]);
        }

        public static bool DefinitionsExist()
        {
            foreach (var file in DefinitionFiles)
            {
                var localPath = Path.Combine(LocalDefinitionsPath, file);
                if (!File.Exists(localPath))
                    return false;
            }
            return true;
        }

        public static void EnsureDefinitionsUpToDate()
        {
            if (!DefinitionsExist() || DateTime.Now - _lastUpdate > UpdateInterval)
            {
                Logger.LogInfo("Updating ClamAV definitions...", new object[0]);
                DownloadDefinitions();
                _lastUpdate = DateTime.Now;
            }
        }
    }
}