using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace antivirus
{
    /// <summary>
    /// Provides scanning and ClamAV integration for the antivirus application.
    /// </summary>
    public static class Scanner
    {
        private const string V = "clamd.log\").Replace(\\";

        // Simplified collection initializations
        private static readonly HashSet<string> ExcludedExtensions = new() { ".cs", ".csproj", ".sln", ".md", ".db", ".log", ".json", ".xml" };
        private static readonly string[] ExcludedFolders = { "bin", "obj", ".git" };
        private static readonly HashSet<string> ExcludedFiles = new() { "NTUSER.DAT", "NTUSER.DAT.LOG", "NTUSER.DAT.LOG1", "NTUSER.DAT.LOG2", "pagefile.sys", "hiberfil.sys" };

        // Move constant arrays to static readonly fields for CA1861
        private static readonly string[] AllowedExtensions = { ".cvd", ".cld", ".exe", ".dll", ".conf", ".log" };
        private static readonly string[] AllowedFiles = { "clamd.exe", "freshclam.exe", "clamd.conf", "freshclam.conf", "clamd.log" };


    
        public static class DownloadUrlResolver
        {
            public static string GetClamAVUrl()
            {
                string arch = RuntimeInformation.OSArchitecture.ToString().ToLower();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return arch switch
                    {
                        "x64" => "https://www.clamav.net/downloads/production/clamav-latest.win.x64.zip",
                        "arm64" => "https://www.clamav.net/downloads/production/clamav-latest.win.arm64.zip",
                        _ => "https://www.clamav.net/downloads"
                    };
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return "https://www.clamav.net/downloads#linux-packages";
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "https://www.clamav.net/downloads#macos-packages";
                }

                return "https://www.clamav.net/downloads";
            }

            public static string GetKmeleonUrl()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "https://sourceforge.net/projects/kmeleon/files/k-meleon-dev/K-Meleon76RC2.7z/download";
                }

                return "https://sourceforge.net/projects/kmeleon";
            }

            public static string GetOperaUrl()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "https://www.opera.com/computer/thanks?ni=stable_portable&os=windows";
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return "https://www.opera.com/computer/thanks?os=linux";
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "https://www.opera.com/computer/thanks?os=mac";
                }

                return "https://www.opera.com";
            }
        }
        private static readonly string ClamAVDir =
        Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");

        private static readonly string ClamdExe =
            Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "ClamAV"), "clamd.exe");

        private static readonly string ClamAVZipUrl =
            DownloadUrlResolver.GetClamAVUrl();

        private static readonly string KmeleonUrl =
            DownloadUrlResolver.GetKmeleonUrl();

        private static readonly string OperaUrl =
            DownloadUrlResolver.GetOperaUrl();

        // CA1861: Move constant arrays to static readonly fields
        private static readonly string[] FreshclamConfDefault =
        {
            "# Example config file for freshclam",
            "# Comment lines start with #",
            "#",
            "# Path to the database directory.",
            "DatabaseDirectory ./database",
            "# Path to the log file (make sure it has proper permissions)",
            "UpdateLogFile ./freshclam.log",
            "# Database owner user", 
            "DatabaseOwner clamav",
            "# Uncomment the following line and replace XY with your country code",
            "# to force a specific mirror.",
            "#DatabaseMirror database.clamav.net"
        };

        private static readonly string[] ClamdConfDefault =
        {
            "# Example config file for clamd",
            "# Comment lines start with #",
            "#",
            "# Path to the database directory.",
            "DatabaseDirectory ./database",
            "# Path to the log file (make sure it has proper permissions)",
            "LogFile ./clamd.log",
            "# Log time with each message.",
            "LogTime yes",
            "# Enable logging to syslog.",
            "LogSyslog yes",
            "# Uncomment the following line to enable logging in verbose mode.",
            "#LogVerbose yes"
        };

        /// <summary>
        /// Downloads a file asynchronously using HttpClient.
        /// </summary>
        private static async Task DownloadFileAsync(string url, string filePath)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);
        }

        /// <summary>
        /// Updates the ClamAV virus database using freshclam.
        /// </summary>
        private static void UpdateClamAVDatabase()
        {
            string freshclamExe = Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "ClamAV"), "freshclam.exe");
            if (File.Exists(freshclamExe))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = freshclamExe,
                        WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV"),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        Logger.LogInfo($"freshclam output: {output}", Array.Empty<object>());
                        if (!string.IsNullOrEmpty(error))
                            Logger.LogWarning($"freshclam error: {error}", Array.Empty<object>());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to run freshclam: {ex.Message}", Array.Empty<object>());
                }
            }
            else
            {
                Logger.LogWarning("freshclam.exe not found. ClamAV definitions will not be updated automatically.", Array.Empty<object>());
            }
        }

        /// <summary>
        /// Removes non-database files from the ClamAV directory.
        /// </summary>
        private static void CleanClamAVDirectory()
        {
            var allowedExtensions = new HashSet<string>(AllowedExtensions, StringComparer.OrdinalIgnoreCase);
            var allowedFiles = new HashSet<string>(AllowedFiles, StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(ClamAVDir))
            {
                string ext = Path.GetExtension(file);
                string name = Path.GetFileName(file);
                if (!allowedExtensions.Contains(ext) && !allowedFiles.Contains(name))
                {
                    try { File.Delete(file); Logger.LogInfo($"Deleted non-database file from ClamAV directory: {name}", Array.Empty<object>()); } catch { }
                }
                if (ext.Equals(".pdb", StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(file); Logger.LogInfo($"Deleted .pdb file from ClamAV directory: {name}", Array.Empty<object>()); } catch { }
                }
            }
        }

        /// <summary>
        /// Ensures ClamAV is installed, configured, and up to date.
        /// </summary>
        private static void EnsureClamAVInstalled()
        {
            if (!File.Exists(ClamdExe))
            {
                Logger.LogWarning($"ClamAV not found: {ClamdExe}. Attempting to download...", Array.Empty<object>());
                Console.WriteLine($"ClamAV not found: {ClamdExe}. Attempting to download...");
                try
                {
                    DownloadAndExtractClamAV().GetAwaiter().GetResult();
                }
                catch (HttpRequestException ex)
                {
                    Logger.LogError($"HTTP request failed: {ex.Message}", Array.Empty<object>());
                    Console.WriteLine("Failed to download ClamAV. Please check your internet connection and try again.");
                    return;
                }
                catch (IOException ex)
                {
                    Logger.LogError($"File operation failed: {ex.Message}", Array.Empty<object>());
                    Console.WriteLine("Failed to extract ClamAV files. Please check file permissions and try again.");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Unexpected error: {ex.Message}", Array.Empty<object>());
                    Console.WriteLine("An unexpected error occurred. Please download and extract ClamAV manually from https://www.clamav.net/downloads.");
                    return;
                }
            }

            Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
            EnsureConfigurationFile("freshclam.conf", FreshclamConfDefault);
            UpdateClamAVDatabase();
            EnsureConfigurationFile("clamd.conf", ClamdConfDefault);
            StartClamDaemon();
        }

        private static async Task DownloadAndExtractClamAV()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            using var httpClient = new HttpClient(handler);
            string tempZip = Path.Combine(Path.GetTempPath(), "clamav.zip");

            Logger.LogInfo("Downloading ClamAV...", Array.Empty<object>());
            await DownloadFileAsync(ClamAVZipUrl, tempZip);
            Logger.LogInfo("Extracting ClamAV...", Array.Empty<object>());
            ZipFile.ExtractToDirectory(tempZip, ClamAVDir, true);

            var dirs = Directory.GetDirectories(ClamAVDir);
            if (dirs.Length == 1)
            {
                string subDir = dirs[0];
                MoveFilesAndDirectories(subDir, ClamAVDir);
                Directory.Delete(subDir, true);
                Logger.LogInfo($"Moved ClamAV files from subdirectory '{Path.GetFileName(subDir)}' to '{ClamAVDir}'.", Array.Empty<object>());
            }
            Logger.LogInfo("ClamAV downloaded and extracted.", Array.Empty<object>());
            Console.WriteLine("ClamAV downloaded and extracted.");
        }

        private static void MoveFilesAndDirectories(string sourceDir, string targetDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relPath = Path.GetRelativePath(sourceDir, file);
                string destPath = Path.Combine(targetDir, relPath);
                var destDir = Path.GetDirectoryName(destPath);
                if (destDir != null)
                {
                    Directory.CreateDirectory(destDir);
                    File.Move(file, destPath, true);
                }
            }

            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relPath = Path.GetRelativePath(sourceDir, dir);
                string destPath = Path.Combine(targetDir, relPath);
                Directory.CreateDirectory(destPath);
            }
        }

        private static void EnsureConfigurationFile(string fileName, string[] defaultContent)
        {
            string filePath = Path.Combine(ClamAVDir, fileName);
            if (!File.Exists(filePath))
            {
                try
                {
                    File.WriteAllLines(filePath, defaultContent);
                    Logger.LogInfo($"Created default {fileName} for ClamAV.", Array.Empty<object>());
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to create {fileName}: {ex.Message}", Array.Empty<object>());
                }
            }
        }

        private static void StartClamDaemon()
        {
            try
            {
                var clamdProcesses = Process.GetProcessesByName("clamd");
                if (clamdProcesses.Length == 0 && File.Exists(ClamdExe))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = ClamdExe,
                        WorkingDirectory = ClamAVDir,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Logger.LogInfo($"clamd.exe: {e.Data}", Array.Empty<object>()); };
                        process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) Logger.LogError($"clamd.exe error: {e.Data}", Array.Empty<object>()); };
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }
                }
                else
                {
                    Logger.LogWarning("ClamAV daemon is not running or executable not found.", Array.Empty<object>());
                }
            }
            catch (Exception ex)
            {
                if (ex?.Message != null)
                {
                    Logger.LogError($"Failed to start ClamAV daemon: {ex.Message}", Array.Empty<object>());
                }
                else
                {
                    Logger.LogError("Failed to start ClamAV daemon: Unknown error", Array.Empty<object>());
                }
            }
        }

        private static ILogger Logger = new DefaultLogger(); // Default logger implementation

        public static void SetLogger(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Checks if the application is running from a removable drive.
        /// </summary>
        public static bool IsRunningFromRemovable()
        {
            string rootPath = Path.GetPathRoot(Directory.GetCurrentDirectory());
            if (string.IsNullOrEmpty(rootPath)) return false;

            DriveInfo driveInfo = new(rootPath);
            return driveInfo.DriveType == DriveType.Removable;
        }

        /// <summary>
        /// Scans a file or directory for potential threats.
        /// </summary>
        public static void Scan(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger.LogError("Path is null or empty.", Array.Empty<object>());
                return;
            }

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                Logger.LogError($"Path not found: {path}", Array.Empty<object>());
                return;
            }

            if (File.Exists(path))
            {
                Logger.LogInfo($"Scanning file: {path}", Array.Empty<object>());
                // Add file scanning logic here
            }
            else if (Directory.Exists(path))
            {
                Logger.LogInfo($"Scanning directory: {path}", Array.Empty<object>());
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    Logger.LogInfo($"Scanning file: {file}", Array.Empty<object>());
                    // Add file scanning logic here
                }
            }
        }
    }
}
