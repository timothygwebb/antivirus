using antivirus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
#if WINDOWS
            try
            {
                var directoryInfo = new DirectoryInfo(ClamAVDir);
                var accessControl = directoryInfo.GetAccessControl();
                accessControl.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                    new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinUsersSid, null),
                    System.Security.AccessControl.FileSystemRights.FullControl,
                    System.Security.AccessControl.AccessControlType.Allow));
                directoryInfo.SetAccessControl(accessControl);
                Logger.LogInfo("Set write permissions for ClamAV directory.", Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to set write permissions for ClamAV directory: {ex.Message}", Array.Empty<object>());
            }
#else
            Logger.LogWarning("Setting write permissions is not supported on this platform.", Array.Empty<object>());
#endif
        }

        // Simplify 'new' expressions and collection initializations
        private static readonly HashSet<string> ExcludedExtensions = new() { ".cs", ".csproj", ".sln", ".md", ".db", ".log", ".json", ".xml" };

        private static readonly string[] ExcludedFolders = { "bin", "obj", ".git" };

        private static readonly HashSet<string> ExcludedFiles = new() { "NTUSER.DAT", "NTUSER.DAT.LOG", "NTUSER.DAT.LOG1", "NTUSER.DAT.LOG2", "pagefile.sys", "hiberfil.sys" };
        // Removed unused field clamAvWarned
        private static readonly string ClamAVDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
        private static readonly string ClamdExe = Path.Combine(ClamAVDir, "clamd.exe");
        private static readonly string ClamAVZipUrl = "https://www.clamav.net/downloads/production/clamav-1.5.1.win.x64.zip";
        private static readonly string KmeleonUrl = "http://sourceforge.net/projects/kmeleon/files/k-meleon-dev/K-Meleon76RC2.7z/download";
        private static readonly string OperaUrl = "https://www.opera.com/computer/thanks?ni=stable_portable&os=windows";
        private static bool IsFirstRun = true; // Flag to track if this is the first run


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
            string freshclamExe = Path.Combine(ClamAVDir, "freshclam.exe");
            if (File.Exists(freshclamExe))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = freshclamExe,
                        WorkingDirectory = ClamAVDir,
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

        private static void PersistFirstRunFlag()
        {
            string flagFilePath = Path.Combine(ClamAVDir, "first_run.flag");
            if (!File.Exists(flagFilePath))
            {
                File.Create(flagFilePath).Dispose();
                IsFirstRun = true;
            }
            else
            {
                IsFirstRun = false;
            }
        }

        /// <summary>
        /// Removes non-database files from the ClamAV directory if it's the first run.
        /// </summary>
        private static void CleanClamAVDirectory()
        {
            PersistFirstRunFlag(); // Check and set the first run flag

            if (!IsFirstRun) return; // Skip cleanup if not the first run

            Logger.LogInfo("Skipping deletion of non-database files from the ClamAV directory.", Array.Empty<object>());

            IsFirstRun = false; // Set the flag to false after the first cleanup
        }

        /// <summary>
        /// Ensures ClamAV is installed, configured, and up to date.
        /// </summary>
        public static bool EnsureClamAVInstalled()
        {
            // Verify ClamAV installation
            if (!File.Exists(ClamdExe) || !File.Exists(Path.Combine(ClamAVDir, "freshclam.exe")))
            {
                Logger.LogError("ClamAV daemon or FreshClam utility not found. Please ensure ClamAV is installed.", Array.Empty<object>());
                Console.WriteLine("ClamAV is not fully configured. Please ensure ClamAV is installed and configured correctly.");
                return false;
            }

#if WINDOWS
            SetWritePermissions();
#else
            Logger.LogWarning("Setting write permissions is not supported on this platform.", Array.Empty<object>());
#endif

            // Validate configuration files
            string freshclamConf = Path.Combine(ClamAVDir, "freshclam.conf");
            string clamdConf = Path.Combine(ClamAVDir, "clamd.conf");
            if (!File.Exists(freshclamConf))
            {
                Logger.LogError("Missing freshclam.conf. Please ensure the configuration file is present.", Array.Empty<object>());
                Console.WriteLine("ClamAV is not fully configured. Please ensure freshclam.conf is present in the ClamAV directory.");
                return false;
            }
            if (!File.Exists(clamdConf))
            {
                Logger.LogError("Missing clamd.conf. Please ensure the configuration file is present.", Array.Empty<object>());
                Console.WriteLine("ClamAV is not fully configured. Please ensure clamd.conf is present in the ClamAV directory.");
                return false;
            }

            // Update virus definitions
            UpdateClamAVDatabase();

            // Check if ClamAV daemon is running
            try
            {
                var clamdProcesses = Process.GetProcessesByName("clamd");
                if (clamdProcesses.Length == 0)
                {
                    Logger.LogInfo($"Attempting to start ClamAV daemon from: {ClamdExe}", Array.Empty<object>());
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
                        Logger.LogInfo("ClamAV daemon started successfully.", Array.Empty<object>());
                    }
                    else
                    {
                        Logger.LogError("Failed to start ClamAV daemon. Process could not be started.", Array.Empty<object>());
                        Console.WriteLine("Failed to start ClamAV daemon. Please check the configuration and try again.");
                        return false;
                    }
                }
                else
                {
                    Logger.LogInfo("ClamAV daemon is already running.", Array.Empty<object>());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to start ClamAV daemon: {ex.Message}", Array.Empty<object>());
                Console.WriteLine("An error occurred while starting ClamAV daemon. Please check the logs for more details.");
                return false;
            }

            // Restart ClamAV service if needed
            Logger.LogInfo("ClamAV installation and configuration verified successfully.", Array.Empty<object>());
            return true;
        }

        // Move browser download logic into a method
        private static void DownloadBrowsersIfNeeded()
        {
            string tempDir = Path.GetTempPath();
            string kmeleonInstaller = Path.Combine(tempDir, "K-Meleon76RC2.7z");
            string operaInstaller = Path.Combine(tempDir, "Opera_Portable.exe");
            // Try K-Meleon first
            try
            {
                using var httpClient = new HttpClient();
                Logger.LogInfo("Attempting to download K-Meleon browser...", Array.Empty<object>());
                DownloadFileAsync(KmeleonUrl, kmeleonInstaller).GetAwaiter().GetResult();
                Logger.LogInfo("K-Meleon browser downloaded. Please extract and run manually if needed.", Array.Empty<object>());
                Console.WriteLine("K-Meleon browser downloaded as archive. Please extract and run manually if needed.");
                Process.Start("explorer.exe", $"/select,\"{kmeleonInstaller}\"");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to download K-Meleon browser: {ex.Message}", Array.Empty<object>());
            }
            // Try Opera as fallback
            try
            {
                using var httpClient = new HttpClient();
                Logger.LogInfo("Attempting to download Opera browser...", Array.Empty<object>());
                DownloadFileAsync(OperaUrl, operaInstaller).GetAwaiter().GetResult();
                Logger.LogInfo("Opera browser downloaded. Please run manually if needed.", Array.Empty<object>());
                Console.WriteLine("Opera browser downloaded. Please run manually if needed.");
                Process.Start("explorer.exe", $"/select,\"{operaInstaller}\"");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to download Opera browser: {ex.Message}", Array.Empty<object>());
                Console.WriteLine("Failed to download both K-Meleon and Opera browsers. Please download and install a browser manually.");
            }
        }

        /// <summary>
        /// Recursively scans a directory, skipping excluded files and folders.
        /// </summary>
        /// <param name="path">The directory path to scan.</param>
        private static void ScanDirectorySafe(string path)
        {
            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    if (ShouldSkipFile(file)) continue;
                    ScanFile(file);
                }
                foreach (var dir in Directory.GetDirectories(path))
                {
                    if (ShouldSkipDir(dir)) continue;
                    ScanDirectorySafe(dir);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogWarning($"Access denied to directory: {path} ({ex.Message})", Array.Empty<object>());
            }
            catch (IOException ex)
            {
                Logger.LogWarning($"IO error in directory: {path} ({ex.Message})", Array.Empty<object>());
            }
        }

        /// <summary>
        /// Determines if a directory should be skipped based on exclusion rules.
        /// </summary>
        private static bool ShouldSkipDir(string dirPath)
        {
            string dirName = Path.GetFileName(dirPath);
            foreach (var folder in ExcludedFolders)
            {
                if (dirName.Equals(folder, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if a file should be skipped based on exclusion rules.
        /// </summary>
        private static bool ShouldSkipFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            foreach (var excluded in ExcludedFiles)
            {
                if (fileName.Equals(excluded, StringComparison.OrdinalIgnoreCase) ||
                    (excluded.EndsWith('*') && fileName.StartsWith(excluded.TrimEnd('*'), StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            string ext = Path.GetExtension(filePath);
            if (ExcludedExtensions.Contains(ext)) return true;
            string dir = Path.GetDirectoryName(filePath) ?? string.Empty;
            foreach (var folder in ExcludedFolders)
            {
                if (dir.Contains(Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    dir.EndsWith(Path.DirectorySeparatorChar + folder, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Scans a file using ClamAV and heuristics, and quarantines if a threat is found.
        /// </summary>
        private static void ScanFile(string filePath)
        {
            string? clamResult = ScanWithClamAVTcp(filePath);
            if (clamResult != null)
            {
                if (clamResult.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogError($"ClamAV detected threat: {clamResult}", Array.Empty<object>());
                    Quarantine.QuarantineFile(filePath);
                    return;
                }
                else if (clamResult.Contains("OK", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogResult($"File clean: {filePath}", Array.Empty<object>());
                    return;
                }
            }

            bool heuristicThreat = filePath.Contains("virus", StringComparison.OrdinalIgnoreCase) || new FileInfo(filePath).Length > 10_000_000;
            if (heuristicThreat)
            {
                Logger.LogWarning($"Heuristic threat detected: {filePath}", Array.Empty<object>());
            }
            else
            {
                Logger.LogResult($"File clean: {filePath}", Array.Empty<object>());
            }
            // Removed nested try block and moved ClamAV TCP scan logic to its own method
        }

        private static string? ScanWithClamAVTcp(string filePath)
        {
            try
            {
                using var client = new TcpClient("localhost", 3310);
                using var stream = client.GetStream();
                byte[] cmd = Encoding.ASCII.GetBytes("zINSTREAM\0");
                stream.Write(cmd, 0, cmd.Length);
                using var file = File.OpenRead(filePath);
                byte[] buffer = new byte[2048];
                int bytesRead;
                while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] size = new byte[4];
                    size[0] = (byte)((bytesRead >> 24) & 0xFF);
                    size[1] = (byte)((bytesRead >> 16) & 0xFF);
                    size[2] = (byte)((bytesRead >> 8) & 0xFF);
                    size[3] = (byte)(bytesRead & 0xFF);
                    stream.Write(size, 0, size.Length);
                    stream.Write(buffer, 0, bytesRead);
                }
                stream.Write(new byte[4], 0, 4);
                using var ms = new MemoryStream();
                byte[] respBuffer = new byte[1024];
                int respBytes = stream.Read(respBuffer, 0, respBuffer.Length);
                ms.Write(respBuffer, 0, respBytes);
                return Encoding.ASCII.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"ClamAV scan failed: {ex.Message}", Array.Empty<object>());
                return null;
            }
        }

        /// <summary>
        /// Scans the given input path (file or directory).
        /// </summary>
        // Add detailed logging to trace the flow of execution in the Scan method
        public static void Scan(string input)
        {
            Logger.LogInfo("Starting ClamAV installation and configuration verification.", Array.Empty<object>());
            if (!EnsureClamAVInstalled())
            {
                Logger.LogError("ClamAV is not fully configured. Program cannot proceed.", Array.Empty<object>());
                Console.WriteLine("ClamAV is not fully configured. Program cannot proceed.");
                return;
            }

            Logger.LogInfo("Verifying ClamAV definitions.", Array.Empty<object>());
            EnsureDefinitionsDatabase();

            Logger.LogInfo("Scanning started", Array.Empty<object>());
            if (Directory.Exists(input))
            {
                Logger.LogInfo($"Scanning directory: {input}", Array.Empty<object>());
                ScanDirectorySafe(input);
            }
            else if (File.Exists(input))
            {
                if (!ShouldSkipFile(input))
                {
                    Logger.LogInfo($"Scanning file: {input}", Array.Empty<object>());
                    ScanFile(input);
                }
            }
            else
            {
                Logger.LogError($"Path not found: {input}", Array.Empty<object>());
            }
            Logger.LogInfo("Scanning finished", Array.Empty<object>());
        }

        /// <summary>
        /// Determines if the application is running from a removable or CD-ROM drive.
        /// </summary>
        public static bool IsRunningFromRemovable()
        {
            var drive = new DriveInfo(Directory.GetCurrentDirectory());
            return drive.DriveType == DriveType.CDRom || drive.DriveType == DriveType.Removable;
        }

        /// <summary>
        /// Ensures the ClamAV definitions database exists.
        /// </summary>
        public static void EnsureDefinitionsDatabase()
        {
            string[] requiredDefinitions = { "main.cvd", "daily.cvd", "bytecode.cvd" };
            foreach (var definition in requiredDefinitions)
            {
                string definitionPath = Path.Combine(ClamAVDir, definition);
                if (!File.Exists(definitionPath))
                {
                    Logger.LogWarning($"ClamAV definition not found: {definitionPath}", Array.Empty<object>());
                    Console.WriteLine($"Missing ClamAV definition: {definition}. Please ensure all required definitions are present.");
                }
                else
                {
                    Logger.LogInfo($"ClamAV definition found: {definitionPath}", Array.Empty<object>());
                }
            }
        }

        /// <summary>
        /// Downloads the ClamAV zip archive.
        /// </summary>
        public static void DownloadClamAVZip()
        {
            try
            {
                string zipPath = Path.Combine(ClamAVDir, "clamav.zip");
                using var client = new HttpClient();
                var response = client.GetAsync(ClamAVZipUrl).Result;
                response.EnsureSuccessStatusCode();
                using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                response.Content.CopyToAsync(fileStream).Wait();
                Logger.LogInfo("ClamAV zip downloaded successfully.", Array.Empty<object>());
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to download ClamAV zip: {ex.Message}", Array.Empty<object>());
            }
        }
    }
}
