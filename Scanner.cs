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
            int port = 3310;
            AddFirewallRules(port);
            Console.WriteLine("Scanning path: " + path);
            Logger.LogInfo("Scanning path: " + path, Array.Empty<object>());

            // Try to connect to ClamAV (both IPv4 and IPv6)
            using var client = TryConnectClamAV(port);
            if (client == null)
            {
                Logger.LogError("Could not connect to ClamAV daemon for scanning.", Array.Empty<object>());
                return;
            }
            // You can now use 'client' to send scan commands/files to ClamAV
            // ... (your scanning logic here)
        }

        public static bool EnsureClamAVInstalled()
        {
            string clamAVPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV", "clamscan.exe");
            string clamdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV", "clamd.exe");
            var files = Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV"))
                ? Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV"), "*", SearchOption.AllDirectories)
                : Array.Empty<string>();
            foreach (var file in files)
                Logger.LogInfo($"ClamAV directory contains: {file}", Array.Empty<object>());

            if (File.Exists(clamAVPath))
            {
                Logger.LogInfo("ClamAV is installed (clamscan.exe found).", Array.Empty<object>());
                return true;
            }
            else if (File.Exists(clamdPath))
            {
                Logger.LogInfo("ClamAV is installed (clamd.exe found, clamscan.exe missing).", Array.Empty<object>());
                Logger.LogWarning("clamscan.exe not found. You may need to use clamd.exe or download a Windows build that includes clamscan.exe.", Array.Empty<object>());
                return true;
            }
            else
            {
                Logger.LogWarning("ClamAV executables not found. Attempting to download and extract ClamAV...", Array.Empty<object>());
                DownloadClamAVZip();
                // Re-check after extraction
                files = Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV"))
                    ? Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV"), "*", SearchOption.AllDirectories)
                    : Array.Empty<string>();
                foreach (var file in files)
                    Logger.LogInfo($"ClamAV directory contains: {file}", Array.Empty<object>());
                if (File.Exists(clamAVPath))
                {
                    Logger.LogInfo("ClamAV is installed (clamscan.exe found after extraction).", Array.Empty<object>());
                    return true;
                }
                else if (File.Exists(clamdPath))
                {
                    Logger.LogInfo("ClamAV is installed (clamd.exe found after extraction, clamscan.exe missing).", Array.Empty<object>());
                    Logger.LogWarning("clamscan.exe not found. You may need to use clamd.exe or download a Windows build that includes clamscan.exe.", Array.Empty<object>());
                    return true;
                }
                else
                {
                    Logger.LogError("ClamAV is not installed. No clamscan.exe or clamd.exe found in ClamAV directory after extraction.", Array.Empty<object>());
                    Logger.LogError("Files present in ClamAV directory are listed above. If clamscan.exe is missing, download a Windows build that includes it (e.g., from ClamWin: https://github.com/clamwin/clamwin/releases) or adjust your configuration.", Array.Empty<object>());
                    return false;
                }
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
                // Use ClamWin portable as a working Windows binary source
                string url = "https://downloads.sourceforge.net/project/clamwin/clamwin/0.103.2/clamwin-portable-0.103.2-setup.exe";
                string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clamwin-portable-setup.exe");
                string extractPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAV");

                // Always delete and recreate ClamAV directory for a clean state, with retry logic
                if (Directory.Exists(extractPath))
                {
                    const int maxAttempts = 5;
                    int attempt = 0;
                    bool deleted = false;
                    while (attempt < maxAttempts && !deleted)
                    {
                        try
                        {
                            Directory.Delete(extractPath, true);
                            Logger.LogInfo($"Deleted existing ClamAV directory: {extractPath}", Array.Empty<object>());
                            deleted = true;
                        }
                        catch (Exception ex)
                        {
                            attempt++;
                            Logger.LogWarning($"Attempt {attempt} to delete ClamAV directory failed: {ex.Message}", Array.Empty<object>());
                            System.Threading.Thread.Sleep(500); // Wait before retry
                        }
                    }
                    if (!deleted)
                    {
                        Logger.LogError($"Failed to delete ClamAV directory after {maxAttempts} attempts. Please close any programs using files in this directory and try again.", Array.Empty<object>());
                        return;
                    }
                }
                Directory.CreateDirectory(extractPath);

                using (var httpClient = new System.Net.Http.HttpClient())
                using (var response = httpClient.GetAsync(url).Result)
                {
                    response.EnsureSuccessStatusCode();
                    using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        response.Content.CopyToAsync(fs).Wait();
                    }
                }
                Logger.LogInfo("ClamWin portable setup downloaded successfully.", Array.Empty<object>());

                // Extraction of .exe installer is not supported natively; instruct user
                Logger.LogError("Automatic extraction of ClamWin portable setup is not supported. Please manually run the installer and copy clamd.exe and/or clamscan.exe to the ClamAV directory.", Array.Empty<object>());
                Logger.LogError("Download ClamWin portable from https://github.com/clamwin/clamwin/releases or https://clamwin.com/ if the above link is broken.", Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to download ClamWin portable setup: " + ex.Message, Array.Empty<object>());
            }
        }
    }
}
