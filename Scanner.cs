using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.IO.Compression;
using System.Net.Http;

namespace antivirus
{
    /// <summary>
    /// Provides scanning and ClamAV integration for the antivirus application.
    /// </summary>
    public static class Scanner
    {
        private static readonly HashSet<string> ExcludedExtensions = new()
        {
            ".cs", ".csproj", ".sln", ".md", ".db", ".log", ".json", ".xml"
        };
        private static readonly string[] ExcludedFolders = { "bin", "obj", ".git" };
        private static readonly HashSet<string> ExcludedFiles = new()
        {
            "NTUSER.DAT", "NTUSER.DAT.LOG", "NTUSER.DAT.LOG1", "NTUSER.DAT.LOG2", "pagefile.sys", "hiberfil.sys"
        };
        private static bool clamAvWarned = false;
        private static readonly string ClamAVDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
        private static readonly string ClamdExe = Path.Combine(ClamAVDir, "clamd.exe");
        private static readonly string ClamAVZipUrl = "https://www.clamav.net/downloads/production/clamav-1.5.1.win.x64.zip";
        private static readonly string KmeleonUrl = "http://sourceforge.net/projects/kmeleon/files/k-meleon-dev/K-Meleon76RC2.7z/download";
        private static readonly string OperaUrl = "https://www.opera.com/computer/thanks?ni=stable_portable&os=windows";

        /// <summary>
        /// Downloads a file asynchronously using HttpClient.
        /// </summary>
        private static async System.Threading.Tasks.Task DownloadFileAsync(string url, string destination)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            using var fs = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
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
        private static void EnsureClamAVInstalled()
        {
            if (!File.Exists(ClamdExe))
            {
                Logger.LogWarning($"ClamAV not found: {ClamdExe}. Attempting to download...", Array.Empty<object>());
                Console.WriteLine($"ClamAV not found: {ClamdExe}. Attempting to download...");
                try
                {
                    System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
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
                }
            }
            CleanClamAVDirectory();
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
            UpdateClamAVDatabase();
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
                        Logger.LogInfo("clamd.exe started automatically.", Array.Empty<object>());
                        Console.WriteLine("clamd.exe started automatically.");
                    }
                    else
                    {
                        Logger.LogWarning("Failed to start clamd.exe: Process.Start returned null.", Array.Empty<object>());
                        Console.WriteLine("Failed to start clamd.exe: Process.Start returned null.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to start clamd.exe: {ex.Message}", Array.Empty<object>());
                Console.WriteLine($"Failed to start clamd.exe: {ex.Message}");
                Console.WriteLine("If you see errors, try running clamd.exe manually in the ClamAV directory. If it fails, check for missing dependencies (like Visual C++ Redistributable) or configuration issues. Also check Windows Defender or firewall settings.");
            }
        }

        /// <summary>
        /// Ensures a legacy browser is downloaded for environments lacking a modern browser.
        /// </summary>
        public static void EnsureLegacyBrowserInstalled()
        {
            string tempDir = Path.GetTempPath();
            string kmeleonInstaller = Path.Combine(tempDir, "K-Meleon76RC2.7z");
            string operaInstaller = Path.Combine(tempDir, "Opera_Portable.exe");
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            // Try K-Meleon first
            try
            {
                using var client = new WebClient();
                Logger.LogInfo("Attempting to download K-Meleon browser...", Array.Empty<object>());
                client.DownloadFile(KmeleonUrl, kmeleonInstaller);
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
                using var client = new WebClient();
                Logger.LogInfo("Attempting to download Opera browser...", Array.Empty<object>());
                client.DownloadFile(OperaUrl, operaInstaller);
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
            string? clamResult = ScanWithClamAV(filePath);
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
                    Logger.LogResult($"ClamAV: File clean: {filePath}", Array.Empty<object>());
                    return;
                }
                else
                {
                    Logger.LogWarning($"ClamAV scan inconclusive: {clamResult}", Array.Empty<object>());
                }
            }

            bool heuristicThreat = filePath.Contains("virus", StringComparison.OrdinalIgnoreCase) || new FileInfo(filePath).Length > 10_000_000;
            bool dbThreat = Definitions.IsKnownThreat(filePath);

            if (dbThreat)
            {
                Logger.LogError($"Known virus detected: {filePath}", Array.Empty<object>());
                Quarantine.QuarantineFile(filePath);
            }
            else if (heuristicThreat)
            {
                Logger.LogWarning($"Heuristic threat detected: {filePath}", Array.Empty<object>());
                Quarantine.QuarantineFile(filePath);
            }
            else if (filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogResult($"File clean (exe): {filePath}", Array.Empty<object>());
            }
            else
            {
                Logger.LogResult($"File clean: {filePath}", Array.Empty<object>());
            }
        }

        /// <summary>
        /// Scans a file with ClamAV via TCP and returns the result string, or null if failed.
        /// </summary>
        private static string? ScanWithClamAV(string filePath)
        {
            try
            {
                using var client = new TcpClient("127.0.0.1", 3310);
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
                    stream.Write(size, 0, 4);
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
                if (!clamAvWarned)
                {
                    Logger.LogWarning($"ClamAV scan failed: {ex.Message}", Array.Empty<object>());
                    clamAvWarned = true;
                }
                return null;
            }
        }

        /// <summary>
        /// Entry point for scanning a file or directory.
        /// </summary>
        public static void Scan(string input)
        {
            EnsureClamAVInstalled();
            Logger.LogInfo("Scanning started", Array.Empty<object>());
            Definitions.LoadDefinitions();
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
    }
}
