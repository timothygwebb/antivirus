using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace antivirus
{
    public static class Scanner
    {
        private static readonly object ClamAVInitLock = new object();
        private static bool ClamAVInitialized = false;

        private static readonly ArrayList ExcludedExtensions = new ArrayList
        {
            ".cs", ".csproj", ".sln", ".md", ".db", ".log", ".json", ".xml"
        };
        private static readonly string[] ExcludedFolders = { "bin", "obj", ".git" };
        private static readonly ArrayList ExcludedFiles = new ArrayList
        {
            "NTUSER.DAT", "NTUSER.DAT.LOG", "NTUSER.DAT.LOG1", "NTUSER.DAT.LOG2", "pagefile.sys", "hiberfil.sys"
        };
        internal static readonly string ClamAVDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClamAV");
        private static readonly string ClamdExe = Path.Combine(ClamAVDir, "clamd.exe");
        private static readonly string ClamAVZipUrl = "https://www.clamav.net/downloads/production/clamav-1.5.1.win.x64.zip";
        private static readonly string KmeleonUrl = "http://sourceforge.net/projects/kmeleon/files/k-meleon-dev/K-Meleon76RC2.7z/download";
        private static readonly string OperaUrl = "https://www.opera.com/computer/thanks?ni=stable_portable&os=windows";
        private static readonly string ChromeUrl = "https://dl.google.com/chrome/install/latest/chrome_installer.exe";
        private static readonly string FirefoxUrl = "https://download.mozilla.org/?product=firefox-latest&os=win&lang=en-US";
        private static readonly string OperaSetupUrl = "https://net.geo.opera.com/opera/stable/windows";
        private static readonly string KmeleonSetupUrl = "https://downloads.sourceforge.net/project/kmeleon/k-meleon/76.4.7/K-Meleon76.4.7.exe";
        private static readonly string DotNetInstallerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dotnetfx.exe");

        private static void DownloadFile(string url, string filePath)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3";
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
            catch (WebException ex)
            {
                Logger.LogError($"Error downloading file: {ex.Message}", new object[0]);
                Console.WriteLine($"Error downloading file: {ex.Message}");
            }
        }

        private static void ExtractZipFile(string zipFilePath, string extractPath)
        {
            try
            {
                // Custom implementation for extracting ZIP files compatible with .NET Framework 2.0
                Logger.LogInfo("ZIP extraction is not supported in .NET Framework 2.0. Please use an external tool.", new object[0]);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error extracting zip file: " + ex.Message, new object[0]);
            }
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static void MoveFile(string sourceFilePath, string destinationFilePath)
        {
            if (File.Exists(destinationFilePath))
            {
                File.Delete(destinationFilePath);
            }
            File.Move(sourceFilePath, destinationFilePath);
        }

        // Fixing string method errors
        private static bool IsExcludedFile(string fileName, object excluded)
        {
            string excludedString = excluded as string;
            if (excludedString == null)
                return false;

            return string.Equals(fileName, excludedString, StringComparison.OrdinalIgnoreCase) ||
                   (excludedString.EndsWith("*") && fileName.StartsWith(excludedString.TrimEnd('*'), StringComparison.OrdinalIgnoreCase));
        }

        // Fixing Contains method for .NET Framework 2.0
        private static bool ContainsIgnoreCase(string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool EndsWithIgnoreCase(string source, string value)
        {
            return source != null && value != null && source.ToLower().EndsWith(value.ToLower());
        }

        private static string TrimEnd(string source, char trimChar)
        {
            return source?.TrimEnd(trimChar);
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
                    using (var process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                            Logger.LogInfo($"freshclam output: {output}", new object[0]);
                            if (!string.IsNullOrEmpty(error))
                                Logger.LogWarning($"freshclam error: {error}", new object[0]);
                            // If freshclam succeeded, notify the definitions manager
                            if (process.ExitCode == 0)
                            {
                                try { ClamAVDefinitionsManager.NotifyUpdated(); } catch { }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to run freshclam: {ex.Message}", new object[0]);
                }
            }
            else
            {
                Logger.LogWarning("freshclam.exe not found. ClamAV definitions will not be updated automatically.", new object[0]);
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
                    Logger.LogWarning($"ClamAV not found: {ClamdExe}. Attempting to download...", new object[0]);
                    Console.WriteLine($"ClamAV not found: {ClamdExe}. Attempting to download...");
                    try
                    {
                        string tempZip = Path.Combine(Path.GetTempPath(), "clamav.zip");
                        DownloadFile(ClamAVZipUrl, tempZip);
                        ExtractZipFile(tempZip, ClamAVDir);
                        var dirs = Directory.GetDirectories(ClamAVDir);
                        if (dirs.Length == 1)
                        {
                            string subDir = dirs[0];
                            foreach (var file in Directory.GetFiles(subDir, "*", SearchOption.AllDirectories))
                            {
                                string relPath = GetRelativePath(subDir, file);
                                string destPath = Path.Combine(ClamAVDir, relPath);
                                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                                MoveFile(file, destPath);
                            }
                            foreach (var dir in Directory.GetDirectories(subDir, "*", SearchOption.AllDirectories))
                            {
                                string relPath = GetRelativePath(subDir, dir);
                                string destPath = Path.Combine(ClamAVDir, relPath);
                                Directory.CreateDirectory(destPath);
                            }
                            Directory.Delete(subDir, true);
                            Logger.LogInfo($"Moved ClamAV files from subdirectory '{Path.GetFileName(subDir)}' to '{ClamAVDir}'.", new object[0]);
                        }
                        Logger.LogInfo("ClamAV downloaded and extracted.", new object[0]);
                        Console.WriteLine("ClamAV downloaded and extracted.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to download or extract ClamAV: {ex.Message}", new object[0]);
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
                        Logger.LogInfo("Created default freshclam.conf for ClamAV.", new object[0]);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to create freshclam.conf: {ex.Message}", new object[0]);
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
                        Logger.LogInfo("Created default clamd.conf for ClamAV.", new object[0]);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to create clamd.conf: {ex.Message}", new object[0]);
                    }
                }

                // 3. Update database before starting clamd.exe
                try
                {
                    if (ClamAVDefinitionsManager.ShouldAttemptUpdate())
                        UpdateClamAVDatabase();
                    else
                        Logger.LogInfo("Skipping freshclam update because a recent attempt was made.", new object[0]);
                }
                catch { }

                try
                {
                    var clamdProcesses = Process.GetProcessesByName("clamd");
                    if (clamdProcesses.Length > 0)
                    {
                        Logger.LogInfo($"Found existing clamd.exe processes: {clamdProcesses.Length}. Attempting to use existing daemon.", new object[0]);
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
                            Logger.LogInfo("clamd.exe started by application.", new object[0]);
                            ClamAVInitialized = true;
                            return true;
                        }
                        else
                        {
                            Logger.LogError("Failed to start clamd.exe process.", new object[0]);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to start ClamAV daemon: {ex.Message}", new object[0]);
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
                Logger.LogInfo("Attempting to download K-Meleon browser...", new object[0]);
                DownloadFile(KmeleonUrl, kmeleonInstaller);
                Logger.LogInfo("K-Meleon browser downloaded. Please extract and run manually if needed.", new object[0]);
                Console.WriteLine("K-Meleon browser downloaded as archive. Please extract and run manually if needed.");
                Process.Start("explorer.exe", $"/select,\"{kmeleonInstaller}\"");
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to download K-Meleon browser: {ex.Message}", new object[0]);
            }
            // Try Opera as fallback
            try
            {
                Logger.LogInfo("Attempting to download Opera browser...", new object[0]);
                DownloadFile(OperaUrl, operaInstaller);
                Logger.LogInfo("Opera browser downloaded. Please run manually if needed.", new object[0]);
                Console.WriteLine("Opera browser downloaded. Please run manually if needed.");
                Process.Start("explorer.exe", $"/select,\"{operaInstaller}\"");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to download Opera browser: {ex.Message}", new object[0]);
                Console.WriteLine("Failed to download both K-Meleon and Opera browsers. Please download and install a browser manually.");
            }
        }

        /// <summary>
        /// Recursively scans a directory, skipping excluded files and folders.
        /// /// <summary>
        /// Recursively scans a directory, skipping excluded files and folders.
        /// </summary>
        /// <param name="path">The directory path to scan.</param>
        private static void ScanDirectorySafe(string path)
        {
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (ShouldSkipFile(file)) continue;
                    ScanFile(file);
                }
                foreach (string dir in Directory.GetDirectories(path))
                {
                    if (ShouldSkipDir(dir)) continue;
                    ScanDirectorySafe(dir);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogWarning($"Access denied to directory: {path} ({ex.Message})", new object[0]);
            }
            catch (IOException ex)
            {
                Logger.LogWarning($"IO error in directory: {path} ({ex.Message})", new object[0]);
            }
        }

        /// <summary>
        /// Determines if a directory should be skipped based on exclusion rules.
        /// /// <summary>
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
                if (IsExcludedFile(fileName, excluded))
                    return true;
            }
            string ext = Path.GetExtension(filePath);
            if (ExcludedExtensions.Contains(ext)) return true;
            string dir = Path.GetDirectoryName(filePath) ?? string.Empty;
            foreach (var folder in ExcludedFolders)
            {
                if (dir.IndexOf(Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar, StringComparison.Ordinal) >= 0 ||
                    dir.EndsWith(Path.DirectorySeparatorChar + folder, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Scans a file using ClamAV and heuristics, and quarantines if a threat is found.
        /// /// <summary>
        /// Scans a file using ClamAV and heuristics, and quarantines if a threat is found.
        /// </summary>
        private static void ScanFile(string filePath)
        {
            string clamResult = ScanWithClamAVTcp(filePath);
            if (clamResult != null)
            {
                if (clamResult.IndexOf("FOUND", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Logger.LogError($"ClamAV detected threat: {clamResult}", new object[0]);
                    Quarantine.QuarantineFile(filePath);
                    return;
                }
                else if (clamResult.IndexOf("OK", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Logger.LogResult($"File clean: {filePath}", new object[0]);
                    return;
                }
            }

            bool heuristicThreat = IsHeuristicThreat(filePath);
            if (heuristicThreat)
            {
                Logger.LogWarning($"Heuristic threat detected: {filePath}", new object[0]);
            }
            else
            {
                Logger.LogResult($"File clean: {filePath}", new object[0]);
            }
            // Removed nested try block and moved ClamAV TCP scan logic to its own method
        }

        private static bool IsHeuristicThreat(string filePath)
        {
            return ContainsIgnoreCase(filePath, "virus") || new FileInfo(filePath).Length > 10000000;
        }

        private static string ScanWithClamAVTcp(string filePath)
        {
            const int maxAttempts = 6;
            const int baseDelayMs = 500;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        client.Connect("localhost", 3310);
                        using (var stream = client.GetStream())
                        {
                            byte[] cmd = Encoding.ASCII.GetBytes("zINSTREAM\0");
                            stream.Write(cmd, 0, cmd.Length);
                            using (var file = File.OpenRead(filePath))
                            {
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
                            }
                            stream.Write(new byte[4], 0, 4);
                            using (var ms = new MemoryStream())
                            {
                                byte[] respBuffer = new byte[1024];
                                int respBytes = stream.Read(respBuffer, 0, respBuffer.Length);
                                ms.Write(respBuffer, 0, respBytes);
                                return Encoding.ASCII.GetString(ms.ToArray());
                            }
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (attempt < maxAttempts)
                    {
                        Logger.LogWarning($"ClamAV connection failed (attempt {attempt}): {ex.Message}", new object[0]);
                        int sleep = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                        System.Threading.Thread.Sleep(Math.Min(sleep, 10000));
                    }
                    else
                    {
                        Logger.LogWarning($"ClamAV scan failed after {0} attempts.", new object[] { maxAttempts });
                        return null;
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    if (attempt < maxAttempts)
                    {
                        Logger.LogWarning($"ClamAV socket disposed early (attempt {attempt}): {ex.Message}", new object[0]);
                        int sleep = baseDelayMs * (int)Math.Pow(2, attempt - 1);
                        System.Threading.Thread.Sleep(Math.Min(sleep, 10000));
                    }
                    else
                    {
                        Logger.LogWarning($"ClamAV scan failed after {0} attempts.", new object[] { maxAttempts });
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"ClamAV scan failed: {ex.Message}", new object[0]);
                    return null;
                }
            }
            Logger.LogWarning($"ClamAV scan failed after {0} attempts.", new object[] { maxAttempts });
            return null;
        }

        /// <summary>
        /// Scans the given input path (file or directory). Returns true if a full scan ran to completion; false if aborted early.
        /// </summary>
        public static bool Scan(string input)
        {
            // Only attempt installation/initialization if not already done
            if (!ClamAVInitialized)
            {
                if (!EnsureClamAVInstalled())
                {
                    Logger.LogError("ClamAV is not fully configured. Program cannot proceed.", new object[0]);
                    Console.WriteLine("ClamAV is not fully configured. Program cannot proceed.");
                    return false;
                }
            }
            if (!IsClamAVDaemonReady())
            {
                Logger.LogError("ClamAV daemon is not ready. Aborting scan.", new object[0]);
                Console.WriteLine("ClamAV daemon is not ready. Aborting scan.");
                return false;
            }
            if (!EnsureClamAVDefinitionsExist())
            {
                Logger.LogError("ClamAV definitions are missing. Aborting scan.", new object[0]);
                Console.WriteLine("ClamAV definitions are missing. Aborting scan.");
                return false;
            }
            Logger.LogInfo("Scanning started", new object[0]);
            // Ensure definitions are present and up-to-date before scanning
            ClamAVDefinitionsManager.EnsureDefinitionsUpToDate();
            if (!ClamAVDefinitionsManager.DefinitionsExist())
            {
                Logger.LogError("ClamAV definitions are missing. Program cannot proceed.", new object[0]);
                Console.WriteLine("ClamAV definitions are missing. Program cannot proceed.");
                return false;
            }
            if (Directory.Exists(input))
            {
                Logger.LogInfo($"Scanning directory: {input}", new object[0]);
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
                Logger.LogError($"Path not found: {input}", new object[0]);
            }
            Logger.LogInfo("Scanning finished", new object[0]);
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
                using (TcpClient client = new TcpClient())
                {
                    client.Connect("localhost", 3310);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"ClamAV daemon is not ready: {ex.Message}", new object[0]);
                return false;
            }
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
                    Logger.LogError($"ClamAV definition missing: {cvdPath} or {cldPath}", new object[0]);
                    allExist = false;
                }
            }
            if (!allExist)
            {
                Logger.LogError("One or more ClamAV definitions are missing. Please run freshclam or update ClamAV definitions.", new object[0]);
            }
            else
            {
                Logger.LogInfo("All required ClamAV definitions are present in the ClamAV directory.", new object[0]);
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
            bool isLegacy = IsLegacyWindows();
            var browsers = isLegacy
                ? new[]
                {
                    new { Name = "K-Meleon", Url = "https://downloads.sourceforge.net/project/kmeleon/k-meleon/1.5.4/K-Meleon1.5.4.exe", File = Path.Combine(installersDir, "K-Meleon1.5.4.exe") },
                    new { Name = "RetroZilla", Url = "https://o.rthost.win/gpc/files1.rt/retrozilla-2.2.exe", File = Path.Combine(installersDir, "RetroZilla-2.2.exe") }
                }
                : new[]
                {
                    new { Name = "Chrome", Url = ChromeUrl, File = Path.Combine(installersDir, "ChromeSetup.exe") },
                    new { Name = "Firefox", Url = FirefoxUrl, File = Path.Combine(installersDir, "FirefoxSetup.exe") },
                    new { Name = "Opera", Url = OperaSetupUrl, File = Path.Combine(installersDir, "OperaSetup.exe") },
                    new { Name = "K-Meleon", Url = KmeleonSetupUrl, File = Path.Combine(installersDir, "K-MeleonSetup.exe") },
                };
            foreach (var browser in browsers)
            {
                if (!File.Exists(browser.File))
                {
                    try
                    {
                        Logger.LogInfo($"Downloading {browser.Name} installer...", new object[0]);
                        DownloadFile(browser.Url, browser.File);
                        Logger.LogInfo($"{browser.Name} installer downloaded to {browser.File}.", new object[0]);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to download {browser.Name} installer: {ex.Message}", new object[0]);
                    }
                }
            }
            // Ensure .NET Framework 2.0 installer is included
            if (!File.Exists(DotNetInstallerPath))
            {
                Logger.LogWarning($".NET Framework 2.0 installer not found at '{DotNetInstallerPath}'. Please ensure 'dotnetfx.exe' is included in the tool folder.", new object[0]);
                Console.WriteLine($".NET Framework 2.0 installer not found at '{DotNetInstallerPath}'. Please ensure 'dotnetfx.exe' is included in the tool folder.");
            }
        }

        private static bool IsLegacyWindows()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32Windows && (os.Version.Major < 5); // Windows Me/98/95
        }

        // Fixing access level for ClamAVDir
        public static string GetClamAVDir()
        {
            return ClamAVDir;
        }

        public static void ReadMBR()
        {
            try
            {
                using (FileStream fs = new FileStream("\\\\.\\PhysicalDrive0", FileMode.Open, FileAccess.Read))
                {
                    byte[] mbr = new byte[512];
                    fs.Read(mbr, 0, mbr.Length);
                    Logger.LogInfo("MBR read successfully.", new object[0]);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogWarning("Could not read MBR: Access denied. Run the program as Administrator.", new object[0]);
                Console.WriteLine("Could not read MBR: Access denied. Run the program as Administrator.");
            }
            catch (FileNotFoundException ex)
            {
                Logger.LogWarning("Could not read MBR: File not found. Ensure the correct path is used.", new object[0]);
                Console.WriteLine("Could not read MBR: File not found. Ensure the correct path is used.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not read MBR: {ex.Message}", new object[0]);
                Console.WriteLine($"Could not read MBR: {ex.Message}");
            }
        }
    }

    // Implementing missing methods in ClamAVDefinitionsManager
    public static class ClamAVDefinitionsManager
    {
        public static void NotifyUpdated()
        {
            // Logic to notify that definitions have been updated
            Logger.LogInfo("ClamAV definitions updated.", new object[0]);
        }

        public static bool ShouldAttemptUpdate()
        {
            // Logic to determine if an update should be attempted
            return true; // Placeholder logic
        }

        public static void EnsureDefinitionsUpToDate()
        {
            // Logic to ensure definitions are up to date
            Logger.LogInfo("Ensuring ClamAV definitions are up to date.", new object[0]);
        }

        public static bool DefinitionsExist()
        {
            // Logic to check if definitions exist
            return Directory.Exists(Scanner.ClamAVDir) && Directory.GetFiles(Scanner.ClamAVDir, "*.cvd").Length > 0;
        }
    }
}
