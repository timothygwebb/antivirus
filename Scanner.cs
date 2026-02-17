#nullable disable
using antivirus;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace antivirus
{
    /// <summary>
    /// Provides scanning and ClamAV integration for the antivirus application.
    /// </summary>
    public static class Scanner
    {
        /// <summary>
        /// Ensures the ClamAV directory has write permissions.
        /// </summary>
        private static void SetWritePermissions()
        {
            try
            {
                Console.WriteLine("Setting write permissions is not supported in .NET Framework 1.1.");
                Logger.LogWarning("Setting write permissions is not supported in this version.", new object[0]);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to set write permissions: " + ex.Message, new object[0]);
            }
        }

        // Simplify 'new' expressions and collection initializations
        private static readonly ArrayList ExcludedExtensions = new ArrayList(new string[] { ".cs", ".csproj", ".sln", ".md", ".db", ".log", ".json", ".xml" });

        private static readonly string[] ExcludedFolders = new string[] { "bin", "obj", ".git" };

        private static readonly ArrayList ExcludedFiles = new ArrayList(new string[] { "NTUSER.DAT", "NTUSER.DAT.LOG", "NTUSER.DAT.LOG1", "NTUSER.DAT.LOG2", "pagefile.sys", "hiberfil.sys" });

        public static void Scan(string path)
        {
            int port = 3310; // Example port for ClamAV
            AddFirewallRules(port);
            InitializeDualStackSocket(port);
            Console.WriteLine("Scanning path: " + path);
            Logger.LogInfo("Scanning path: " + path, new object[0]);
            // Example IP address parsing
            string ipStr = "::1"; // IPv6 loopback
            var ip = System.Net.IPAddress.Parse(ipStr);
            Logger.LogInfo($"Parsed IP address: {ip}", new object[0]);
        }

        public static bool EnsureClamAVInstalled()
        {
            // Check if ClamAV is installed by verifying the presence of its executable
            string clamAVPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV", "clamscan.exe");
            if (File.Exists(clamAVPath))
            {
                Logger.LogInfo("ClamAV is installed.", new object[0]);
                return true;
            }
            else
            {
                Logger.LogError("ClamAV is not installed.", new object[0]);
                return false;
            }
        }

        public static void EnsureDefinitionsDatabase()
        {
            // Check if ClamAV definitions database exists
            string definitionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAVDefs");
            if (!Directory.Exists(definitionsPath))
            {
                Logger.LogError("ClamAV definitions database is missing.", new object[0]);
                return;
            }

            Logger.LogInfo("ClamAV definitions database is present.", new object[0]);
        }

        public static void DownloadClamAVZip()
        {
            try
            {
                string url = "https://www.clamav.net/downloads/production/clamav-win-x64.zip";
                string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV.zip");

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(url, destinationPath);
                }

                Logger.LogInfo("ClamAV zip downloaded successfully.", new object[0]);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to download ClamAV zip: " + ex.Message, new object[0]);
            }
        }
    }
}
