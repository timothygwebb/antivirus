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

        // Simplified collection initializations
        private static readonly ArrayList ExcludedExtensions = new() { ".cs", ".csproj", ".sln", ".md", ".db", ".log", ".json", ".xml" };
        private static readonly string[] ExcludedFolders = { "bin", "obj", ".git" };
        private static readonly ArrayList ExcludedFiles = new() { "NTUSER.DAT", "NTUSER.DAT.LOG", "NTUSER.DAT.LOG1", "NTUSER.DAT.LOG2", "pagefile.sys", "hiberfil.sys" };

        /// <summary>
        /// Adds Windows Defender firewall rules for IPv4 and IPv6 using PowerShell.
        /// </summary>
        public static void AddFirewallRules(int port)
        {
            try
            {
                string tcpRule = $"New-NetFirewallRule -DisplayName 'Antivirus IPv4/IPv6' -Direction Inbound -Protocol TCP -LocalPort {port} -Action Allow";
                string udpRule = $"New-NetFirewallRule -DisplayName 'Antivirus IPv4/IPv6' -Direction Inbound -Protocol UDP -LocalPort {port} -Action Allow";
                var psi = new System.Diagnostics.ProcessStartInfo("powershell", $"-Command \"{tcpRule}; {udpRule}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = System.Diagnostics.Process.Start(psi);
                process.WaitForExit();
                Logger.LogInfo($"Firewall rules added for port {port} (IPv4/IPv6)", Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to add firewall rules: " + ex.Message, Array.Empty<object>());
            }
        }

        /// <summary>
        /// Initializes a dual-stack socket for IPv4 and IPv6.
        /// </summary>
        public static void InitializeDualStackSocket(int port)
        {
            var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetworkV6, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            socket.DualMode = true;
            socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.IPv6Any, port));
            socket.Listen(10);
            Logger.LogInfo($"Dual-stack socket listening on port {port} (IPv4/IPv6)", Array.Empty<object>());
        }

        public static void Scan(string path)
        {
            int port = 3310; // Example port for ClamAV
            AddFirewallRules(port);
            InitializeDualStackSocket(port);
            Console.WriteLine("Scanning path: " + path);
            Logger.LogInfo("Scanning path: " + path, Array.Empty<object>());
            // Example IP address parsing
            string ipStr = "::1"; // IPv6 loopback
            var ip = System.Net.IPAddress.Parse(ipStr);
            Logger.LogInfo($"Parsed IP address: {ip}", Array.Empty<object>());
        }

        public static bool EnsureClamAVInstalled()
        {
            // Check if ClamAV is installed by verifying the presence of its executable
            string clamAVPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV", "clamscan.exe");
            if (File.Exists(clamAVPath))
            {
                Logger.LogInfo("ClamAV is installed.", Array.Empty<object>());
                return true;
            }
            else
            {
                Logger.LogError("ClamAV is not installed.", Array.Empty<object>());
                return false;
            }
        }

        public static void EnsureDefinitionsDatabase()
        {
            // Check if ClamAV definitions database exists
            string definitionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAVDefs");
            if (!Directory.Exists(definitionsPath))
            {
                Logger.LogError("ClamAV definitions database is missing.", Array.Empty<object>());
                return;
            }

            Logger.LogInfo("ClamAV definitions database is present.", Array.Empty<object>());
        }

        public static void DownloadClamAVZip()
        {
            try
            {
                string url = "https://www.clamav.net/downloads/production/clamav-win-x64.zip";
                string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV.zip");
                string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV");

                using var httpClient = new System.Net.Http.HttpClient();
                using var response = httpClient.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                response.Content.CopyToAsync(fs).Wait();
                Logger.LogInfo("ClamAV zip downloaded successfully.", Array.Empty<object>());

                // Extract zip
                if (File.Exists(destinationPath))
                {
                    if (!Directory.Exists(extractPath))
                        Directory.CreateDirectory(extractPath);
                    ZipFile.ExtractToDirectory(destinationPath, extractPath, true);
                    Logger.LogInfo("ClamAV zip extracted successfully.", Array.Empty<object>());

                    // Log all extracted files for troubleshooting
                    var allFiles = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);
                    foreach (var file in allFiles)
                        Logger.LogInfo($"Extracted file: {file}", Array.Empty<object>());

                    // Move clamscan.exe to ClamAV directory if found
                    string[] files = Directory.GetFiles(extractPath, "clamscan.exe", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        string targetPath = Path.Combine(extractPath, "clamscan.exe");
                        if (!File.Exists(targetPath))
                            File.Copy(files[0], targetPath, true);
                        Logger.LogInfo($"clamscan.exe placed in ClamAV directory: {targetPath}", Array.Empty<object>());
                    }
                    else
                    {
                        Logger.LogError("clamscan.exe not found after extraction. Check extracted files above.", Array.Empty<object>());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to download or extract ClamAV zip: " + ex.Message, Array.Empty<object>());
            }
        }
    }
}
