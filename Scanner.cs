#nullable enable
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
using System.Threading.Tasks;

namespace antivirus
{
    /// <summary>
    /// Provides scanning and ClamAV integration for the antivirus application.
    /// </summary>
    public static class Scanner
    {
        // Guard to prevent repeated ClamAV initialization and duplicate actions/logging
        private static readonly object ClamAVInitLock = new object();
        private static bool ClamAVInitialized = false;
        private static readonly object BrowserInstallersLock = new object();
        private static bool BrowserInstallersInitialized = false;

        private static readonly HashSet<string> ExcludedExtensions = new()
        {
            ".cs", ".csproj", ".sln", ".md", ".db", ".log", ".json", ".xml"
        };
        private static readonly string[] ExcludedFolders = { "bin", "obj", ".git" };
        private static readonly HashSet<string> ExcludedFiles = new()
        {
            "NTUSER.DAT", "NTUSER.DAT.LOG", "NTUSER.DAT.LOG1", "NTUSER.DAT.LOG2", "pagefile.sys", "hiberfil.sys"
        };
        // Removed unused field clamAvWarned
		private static readonly string ClamAVDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClamAV");
        private static readonly string ClamdExe = Path.Combine(ClamAVDir, "clamd.exe");
        private static readonly string ClamAVZipUrl = "https://www.clamav.net/downloads/production/clamav-1.5.1.win.x64.zip";
        private static readonly string KmeleonUrl = "http://sourceforge.net/projects/kmeleon/files/k-meleon-dev/K-Meleon76RC2.7z/download";
        private static readonly string OperaUrl = "https://www.opera.com/computer/thanks?ni=stable_portable&os=windows";
        // URLs for browser installers
        private static readonly string ChromeUrl = "https://dl.google.com/chrome/install/latest/chrome_installer.exe";
        private static readonly string FirefoxUrl = "https://download.mozilla.org/?product=firefox-latest&os=win&lang=en-US";
        private static readonly string OperaSetupUrl = "https://net.geo.opera.com/opera/stable/windows";
        private static readonly string KmeleonSetupUrl = "https://downloads.sourceforge.net/project/kmeleon/k-meleon/76.4.7/K-Meleon76.4.7.exe";


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
                        // If freshclam succeeded, notify the definitions manager
                        if (process.ExitCode == 0)
                        {
                            try { ClamAVDefinitionsManager.NotifyUpdated(); } catch { }
                        }
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
            var allowedExtensions = new HashSet<string>(new[] { ".cvd", ".cld", ".exe", ".dll", ".conf", ".log" }, StringComparer.OrdinalIgnoreCase);
            var allowedFiles = new HashSet<string>(new[] { "clamd.exe", "freshclam.exe", "clamd.conf", "freshclam.conf", "clamd.log" }, StringComparer.OrdinalIgnoreCase);
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
        public static bool EnsureClamAVInstalled()
        {
            // If daemon already responding or we've initialized, nothing to do
            if (ClamAVInitialized || IsClamAVDaemonReady())
                return true;

            lock (ClamAVInitLock)
            {
                if (ClamAVInitialized)
                    return true;

                // 1. Download and extract ClamAV if needed
                if (!File.Exists(ClamdExe))
                {
                    Logger.LogWarning($"ClamAV not found: {ClamdExe}. Attempting to download...", Array.Empty<object>());
                    Console.WriteLine($"ClamAV not found: {ClamdExe}. Attempting to download...");
                    try
                    {
                        var handler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                        };
                        using var httpClient = new HttpClient(handler);
                        string tempZip = Path.Combine(Path.GetTempPath(), "clamav.zip");
                        DownloadFileAsync(ClamAVZipUrl, tempZip).GetAwaiter().GetResult();
                        ZipFile.ExtractToDirectory(tempZip, ClamAVDir, true);
                        var dirs = Directory.GetDirectories(ClamAVDir);
                        if (dirs.Length == 1)
                        {
                            string subDir = dirs[0];
                            foreach (var file in Directory.GetFiles(subDir, "*", SearchOption.AllDirectories))
                            {
                                string relPath = Path.GetRelativePath(subDir, file);
                                string destPath = Path.Combine(ClamAVDir, relPath);
                                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                                File.Move(file, destPath, true);
                            }
                            foreach (var dir in Directory.GetDirectories(subDir, "*", SearchOption.AllDirectories))
                            {
                                string relPath = Path.GetRelativePath(subDir, dir);
                                string destPath = Path.Combine(ClamAVDir, relPath);
                                Directory.CreateDirectory(destPath);
                            }
                            Directory.Delete(subDir, true);
                            Logger.LogInfo($"Moved ClamAV files from subdirectory '{Path.GetFileName(subDir)}' to '{ClamAVDir}'.", Array.Empty<object>());
                        }
                        Logger.LogInfo("ClamAV downloaded and extracted.", Array.Empty<object>());
                        Console.WriteLine("ClamAV downloaded and extracted.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to download or extract ClamAV: {ex.Message}", Array.Empty<object>());
                        Console.WriteLine("Failed to download or extract ClamAV. Please download and extract it manually from https://www.clamav.net/downloads.");
                        return false;
                    }
                }

                // 2. Create config files if needed
                string freshclamConf = Path.Combine(ClamAVDir, "freshclam.conf");
                if (!File.Exists(freshclamConf))
                {
                    try
                    {
                        var conf = new[]
                        {
                            $"DatabaseDirectory {ClamAVDir.Replace("\\", "/")}",
                            "DatabaseMirror database.clamav.net"
                        };
                        File.WriteAllLines(freshclamConf, conf);
                        Logger.LogInfo("Created default freshclam.conf for ClamAV.", Array.Empty<object>());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to create freshclam.conf: {ex.Message}", Array.Empty<object>());
                    }
                }
                string clamdConf = Path.Combine(ClamAVDir, "clamd.conf");
                if (!File.Exists(clamdConf))
                {
                    try
                    {
                        var conf = new[]
                        {
                            $"LogFile {Path.Combine(ClamAVDir, "clamd.log").Replace("\\", "/")}",
                            $"DatabaseDirectory {ClamAVDir.Replace("\\", "/")}",
                            "TCPSocket 3310",
                            "TCPAddr 127.0.0.1",
                            "Foreground true",
                            "ScanPE true",
                            "ScanELF false",
                            "ScanMail false"
                        };
                        File.WriteAllLines(clamdConf, conf);
                        Logger.LogInfo("Created default clamd.conf for ClamAV.", Array.Empty<object>());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to create clamd.conf: {ex.Message}", Array.Empty<object>());
                    }
                }

                // 3. Clean ClamAV directory and update database before starting clamd.exe
                CleanClamAVDirectory();
                try
                {
                    if (ClamAVDefinitionsManager.ShouldAttemptUpdate())
                        UpdateClamAVDatabase();
                    else
                        Logger.LogInfo("Skipping freshclam update because a recent attempt was made.", Array.Empty<object>());
                }
                catch { }

                try
                {
                    var clamdProcesses = Process.GetProcessesByName("clamd");
                    if (clamdProcesses.Length > 0)
                    {
                        Logger.LogInfo($"Found existing clamd.exe processes: {clamdProcesses.Length}. Attempting to use existing daemon.", Array.Empty<object>());
                        // If existing clamd processes exist, assume daemon is ready
                        ClamAVInitialized = true;
                        return true;
                    }

                    if (File.Exists(ClamdExe))
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
                        var clamdProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                        if (clamdProcess.Start())
                        {
                            Logger.LogInfo("clamd.exe started by application.", Array.Empty<object>());
                            ClamAVInitialized = true;
                            return true;
                        }
                        else
                        {
                            Logger.LogError("Failed to start clamd.exe process.", Array.Empty<object>());
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to start ClamAV daemon: {ex.Message}", Array.Empty<object>());
                    return false;
                }

                // Mark initialized even if we don't wait for clamd to be ready
                ClamAVInitialized = true;
                return true;
            }
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
            const int maxAttempts = 6;
            const int baseDelayMs = 500;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        var connectTask = client.ConnectAsync("localhost", 3310);
                        int timeout = 2000 + attempt * 1000; // increase timeout each attempt
                        if (!connectTask.Wait(timeout) || !client.Connected)
                            throw new SocketException();
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
                }
                catch (SocketException ex) when (attempt < maxAttempts)
                {
                    Logger.LogWarning($"ClamAV connection failed (attempt {attempt}): {ex.Message}", Array.Empty<object>());
                    int sleep = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    System.Threading.Thread.Sleep(Math.Min(sleep, 10000));
                }
                catch (ObjectDisposedException ex) when (attempt < maxAttempts)
                {
                    Logger.LogWarning($"ClamAV socket disposed early (attempt {attempt}): {ex.Message}", Array.Empty<object>());
                    int sleep = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                    System.Threading.Thread.Sleep(Math.Min(sleep, 10000));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"ClamAV scan failed: {ex.Message}", Array.Empty<object>());
                    return null;
                }
            }
            Logger.LogWarning($"ClamAV scan failed after {maxAttempts} attempts.", Array.Empty<object>());
            return null;
        }

        /// <summary>
        /// Scans the given input path (file or directory). Returns true if a full scan ran to completion; false if aborted early.
        /// </summary>
        public static bool Scan(string input)
        {
            // Ensure browser installers are downloaded only once
            lock (BrowserInstallersLock)
            {
                if (!BrowserInstallersInitialized)
                {
                    EnsureBrowserInstallers();
                    BrowserInstallersInitialized = true;
                }
            }
            // Only attempt installation/initialization if not already done
            if (!ClamAVInitialized)
            {
                if (!EnsureClamAVInstalled())
                {
                Logger.LogError("ClamAV is not fully configured. Program cannot proceed.", Array.Empty<object>());
                Console.WriteLine("ClamAV is not fully configured. Program cannot proceed.");
                return false;
                }
            }
            if (!IsClamAVDaemonReady())
            {
                Logger.LogError("ClamAV daemon is not ready. Aborting scan.", Array.Empty<object>());
                Console.WriteLine("ClamAV daemon is not ready. Aborting scan.");
                return false;
            }
            if (!EnsureClamAVDefinitionsExist())
            {
                Logger.LogError("ClamAV definitions are missing. Aborting scan.", Array.Empty<object>());
                Console.WriteLine("ClamAV definitions are missing. Aborting scan.");
                return false;
            }
            Logger.LogInfo("Scanning started", Array.Empty<object>());
            // Ensure definitions are present and up-to-date before scanning
            ClamAVDefinitionsManager.EnsureDefinitionsUpToDate();
            if (!ClamAVDefinitionsManager.DefinitionsExist())
            {
                Logger.LogError("ClamAV definitions are missing. Program cannot proceed.", Array.Empty<object>());
                Console.WriteLine("ClamAV definitions are missing. Program cannot proceed.");
                return false;
            }
            if (Directory.Exists(input))
            {
                Logger.LogInfo($"Scanning directory: {input}", Array.Empty<object>());
                ScanDirectorySafe(input);
            }
            else if (File.Exists(input))
            {
                if (!ShouldSkipFile(input))
                {
                    ScanFile(input);
                }
            }
            else
            {
                Logger.LogError($"Path not found: {input}", Array.Empty<object>());
            }
            Logger.LogInfo("Scanning finished", Array.Empty<object>());
            return true;
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
        /// Checks if the ClamAV daemon is listening on port 3310.
        /// </summary>
        public static bool IsClamAVDaemonReady()
        {
            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync("localhost", 3310);
                if (task.Wait(1000) && client.Connected)
                    return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Ensures the ClamAV virus definitions exist in the ClamAV directory.
        /// </summary>
        public static bool EnsureClamAVDefinitionsExist()
        {
            string[] requiredDefs = { "main", "daily", "bytecode" };
            bool allExist = true;
            foreach (var def in requiredDefs)
            {
                string cvdPath = Path.Combine(ClamAVDir, def + ".cvd");
                string cldPath = Path.Combine(ClamAVDir, def + ".cld");
                if (!File.Exists(cvdPath) && !File.Exists(cldPath))
                {
                    Logger.LogError($"ClamAV definition missing: {cvdPath} or {cldPath}", Array.Empty<object>());
                    allExist = false;
                }
            }
            if (!allExist)
            {
                Logger.LogError("One or more ClamAV definitions are missing. Please run freshclam or update ClamAV definitions.", Array.Empty<object>());
            }
            else
            {
                Logger.LogInfo("All required ClamAV definitions are present in the ClamAV directory.", Array.Empty<object>());
            }
            return allExist;
        }

        /// <summary>
        /// Ensures browser installers are present, downloads them if missing.
        /// </summary>
        public static void EnsureBrowserInstallers()
        {
            string installersDir = Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers");
            Directory.CreateDirectory(installersDir);
            var browsers = new (string Name, string Url, string File)[]
            {
                ("Chrome", ChromeUrl, Path.Combine(installersDir, "ChromeSetup.exe")),
                ("Firefox", FirefoxUrl, Path.Combine(installersDir, "FirefoxSetup.exe")),
                ("Opera", OperaSetupUrl, Path.Combine(installersDir, "OperaSetup.exe")),
                ("K-Meleon", KmeleonSetupUrl, Path.Combine(installersDir, "K-MeleonSetup.exe")),
            };
            foreach (var (name, url, file) in browsers)
            {
                if (!File.Exists(file))
                {
                    try
                    {
                        Logger.LogInfo($"Downloading {name} installer...", Array.Empty<object>());
                        DownloadFileAsync(url, file).GetAwaiter().GetResult();
                        Logger.LogInfo($"{name} installer downloaded to {file}.", Array.Empty<object>());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to download {name} installer: {ex.Message}", Array.Empty<object>());
                    }
                }
            }
        }
    }
}
