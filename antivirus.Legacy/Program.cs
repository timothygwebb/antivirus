using System;
using System.IO;
using System.Diagnostics;

namespace antivirus.Legacy
{
    public class Program
    {
        public static void Main(string[] _)
        {
            try
            {
                Console.WriteLine("Legacy Antivirus Recovery Tool");

                string installersDir = Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers");
                if (!Directory.Exists(installersDir))
                {
                    Directory.CreateDirectory(installersDir);
                }

                // Step 1: Download and install ClamAV
                string clamavInstallerPath = Path.Combine(installersDir, "clamav-1.0.9.win.win32.msi");
                string clamavDownloadUrl = "https://github.com/Cisco-Talos/clamav/releases/download/clamav-1.0.9/clamav-1.0.9.win.win32.msi";
                DownloadWithCurl(clamavDownloadUrl, clamavInstallerPath);

                bool installSuccess = false;
                if (File.Exists(clamavInstallerPath))
                {
                    installSuccess = InstallClamAV(clamavInstallerPath);
                }
                else
                {
                    Console.WriteLine("ClamAV installer not found. Skipping installation.");
                }

                // Step 2: Configure ClamAV
                ConfigureClamAV();

                // Step 2b: Download virus definitions using freshclam
                RunFreshclam();

                // Step 3: Scan files
                string scanDir = Path.Combine(Directory.GetCurrentDirectory(), "ScanDirectory");
                if (!Directory.Exists(scanDir))
                {
                    Directory.CreateDirectory(scanDir);
                }
                string clamscanPath = FindClamscanExecutable();
                string dbDir = FindClamAVDatabaseDirectory();
                ScanFiles(scanDir, clamscanPath, dbDir);

                // Step 4: Quarantine infected files
                string quarantineDir = Path.Combine(Directory.GetCurrentDirectory(), "Quarantine");
                if (!Directory.Exists(quarantineDir))
                {
                    Directory.CreateDirectory(quarantineDir);
                }
                QuarantineInfectedFiles(scanDir, quarantineDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }

        private static void DownloadWithCurl(string url, string destinationPath)
        {
            try
            {
                if (!IsExecutableAvailable("curl"))
                {
                    Console.WriteLine("curl is not available on this system. Please install curl or download the file manually.");
                    return;
                }

                string curlPath = "curl";
                string arguments = $"-L -o \"{destinationPath}\" \"{url}\"";

                Console.WriteLine($"Executing curl command: {curlPath} {arguments}");

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = curlPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();

                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string errorOutput = process.StandardError.ReadToEnd();

                    Console.WriteLine($"curl standard output: {standardOutput}");
                    Console.WriteLine($"curl error output: {errorOutput}");

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"curl failed with error: {errorOutput}");
                        Console.WriteLine("Please download the file manually and place it in the appropriate directory.");
                    }
                    else
                    {
                        Console.WriteLine("Download completed successfully using curl.");
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                Console.WriteLine(win32Ex.ToString());
                Console.WriteLine("Ensure curl is installed and available in the system's PATH.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to use curl for downloading: {ex.Message}");
                Console.WriteLine("Please download the file manually and place it in the appropriate directory.");
            }
        }

        private static bool IsExecutableAvailable(string executableName)
        {
            try
            {
                string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                string[] paths = pathVariable.Split(Path.PathSeparator);

                // Only consider extensions that can be launched with UseShellExecute = false
                string[] allowedExtensions = { ".exe", ".com" };
                string pathextValue = Environment.GetEnvironmentVariable("PATHEXT") ?? string.Join(";", allowedExtensions);
                string[] allExtensions = pathextValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                string[] extensions = Array.FindAll(
                    allExtensions,
                    ext => Array.Exists(allowedExtensions, a => a.Equals(ext, StringComparison.OrdinalIgnoreCase)));
                if (extensions.Length == 0)
                    extensions = allowedExtensions;

                bool hasExtension = !string.IsNullOrEmpty(Path.GetExtension(executableName));

                foreach (string pathEntry in paths)
                {
                    if (string.IsNullOrEmpty(pathEntry))
                        continue;

                    // Normalize: remove surrounding quotes and expand embedded environment variables
                    string normalizedPath = pathEntry.Trim().Trim('"');
                    normalizedPath = Environment.ExpandEnvironmentVariables(normalizedPath);

                    // Check exact name first (handles cases where extension is already included)
                    string fullPath = Path.Combine(normalizedPath, executableName);
                    if (File.Exists(fullPath))
                        return true;

                    // Check with each real executable extension only if name has no extension
                    if (!hasExtension)
                    {
                        foreach (string ext in extensions)
                        {
                            string fullPathWithExt = Path.Combine(normalizedPath, executableName + ext);
                            if (File.Exists(fullPathWithExt))
                                return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking executable availability ({executableName}): {ex.Message}");
                return false;
            }
        }

        // Removed duplicate void InstallClamAV
        private static bool InstallClamAV(string installerPath)
        {
            try
            {
                Console.WriteLine("Installing ClamAV...");
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{installerPath}\" /quiet /norestart", // Silent installation for MSI
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("ClamAV installed successfully.");
                        // Check for clamdscan.exe existence
                        string clamdscanPath = FindClamdscanExecutable();
                        if (string.IsNullOrEmpty(clamdscanPath))
                        {
                            Console.WriteLine("ClamAV installation succeeded, but clamdscan.exe was not found. Please check installation location or PATH.");
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("ClamAV installation failed.");
                        return false;
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                Console.WriteLine(win32Ex.ToString());
                Console.WriteLine("Ensure the installer path is correct and you have the necessary permissions.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to install ClamAV: {ex.Message}");
                return false;
            }
            return false;
        }

        private static string FindClamdscanExecutable()
        {
            // Try PATH
            if (IsExecutableAvailable("clamdscan"))
                return "clamdscan";

            // Try common install locations
            string[] commonPaths = new string[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ClamAV\\clamdscan.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "clamdscan.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers\\clamdscan.exe")
            };
            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        private static string FindClamscanExecutable()
        {
            // Prefer standalone clamscan.exe (does not require running daemon)
            if (IsExecutableAvailable("clamscan"))
                return "clamscan";

            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? string.Empty;
            string[] commonPaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ClamAV\\clamscan.exe"),
                !string.IsNullOrEmpty(programFilesX86) ? Path.Combine(programFilesX86, "ClamAV\\clamscan.exe") : string.Empty,
                Path.Combine(Directory.GetCurrentDirectory(), "clamscan.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers\\clamscan.exe")
            };
            foreach (string path in commonPaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }
            // Fall back to clamdscan if clamscan is not found
            return FindClamdscanExecutable();
        }

        private static string FindFreshclamExecutable()
        {
            if (IsExecutableAvailable("freshclam"))
                return "freshclam";

            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? string.Empty;
            string[] commonPaths = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ClamAV\\freshclam.exe"),
                !string.IsNullOrEmpty(programFilesX86) ? Path.Combine(programFilesX86, "ClamAV\\freshclam.exe") : string.Empty,
                Path.Combine(Directory.GetCurrentDirectory(), "freshclam.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers\\freshclam.exe")
            };
            foreach (string path in commonPaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    return path;
            }
            return null;
        }

        private static void RunFreshclam()
        {
            try
            {
                string freshclamPath = FindFreshclamExecutable();
                if (string.IsNullOrEmpty(freshclamPath))
                {
                    Console.WriteLine("freshclam.exe not found. Virus definition databases may not be up to date.");
                    return;
                }

                Console.WriteLine("Downloading ClamAV virus definitions with freshclam...");
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = freshclamPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine($"freshclam output: {output}");
                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine($"freshclam messages: {error}");

                    if (process.ExitCode == 0)
                        Console.WriteLine("ClamAV virus definitions downloaded successfully.");
                    else
                        Console.WriteLine("freshclam completed with warnings. Check the output above.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run freshclam: {ex.Message}");
            }
        }
        private static void ConfigureClamAV()
        {
            try
            {
                Console.WriteLine("Configuring ClamAV...");
                string clamavInstallDir = FindClamAVInstallDirectory();
                string dbDir = !string.IsNullOrEmpty(clamavInstallDir)
                    ? Path.Combine(clamavInstallDir, "database")
                    : Path.Combine(Directory.GetCurrentDirectory(), "clamav-db");

                if (!Directory.Exists(dbDir))
                    Directory.CreateDirectory(dbDir);

                // Write clamd.conf to the ClamAV install directory (or current dir as fallback)
                string confDir = !string.IsNullOrEmpty(clamavInstallDir) ? clamavInstallDir : Directory.GetCurrentDirectory();
                string configPath = Path.Combine(confDir, "clamd.conf");
                File.WriteAllText(configPath,
                    "# ClamAV daemon configuration\n" +
                    $"DatabaseDirectory {dbDir}\n" +
                    "TCPSocket 3310\n" +
                    "TCPAddr 127.0.0.1\n" +
                    "ScanPE true\n");

                // Write freshclam.conf so freshclam knows where to save databases
                string freshclamConfPath = Path.Combine(confDir, "freshclam.conf");
                File.WriteAllText(freshclamConfPath,
                    "# ClamAV freshclam configuration\n" +
                    $"DatabaseDirectory {dbDir}\n" +
                    "DatabaseMirror database.clamav.net\n");

                Console.WriteLine($"ClamAV configured successfully. Database directory: {dbDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure ClamAV: {ex.Message}");
            }
        }

        private static string FindClamAVInstallDirectory()
        {
            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? string.Empty;
            string[] commonDirs = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ClamAV"),
                !string.IsNullOrEmpty(programFilesX86) ? Path.Combine(programFilesX86, "ClamAV") : string.Empty,
                @"C:\ClamAV",
                Path.Combine(Directory.GetCurrentDirectory(), "ClamAV")
            };
            foreach (string dir in commonDirs)
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return dir;
            }
            return null;
        }

        private static string FindClamAVDatabaseDirectory()
        {
            string installDir = FindClamAVInstallDirectory();
            if (!string.IsNullOrEmpty(installDir))
            {
                string dbDir = Path.Combine(installDir, "database");
                if (Directory.Exists(dbDir))
                    return dbDir;
            }
            string fallback = Path.Combine(Directory.GetCurrentDirectory(), "clamav-db");
            if (Directory.Exists(fallback))
                return fallback;
            return null;
        }

        // Removed duplicate void ScanFiles
        private static void ScanFiles(string directory, string clamscanPath, string databaseDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(clamscanPath) ||
                    (!clamscanPath.Equals("clamscan", StringComparison.OrdinalIgnoreCase) &&
                     !clamscanPath.Equals("clamdscan", StringComparison.OrdinalIgnoreCase) &&
                     !File.Exists(clamscanPath)))
                {
                    Console.WriteLine("clamscan.exe is not available on this system. Please ensure ClamAV is installed and the executable is in the system's PATH or specify its location.");
                    return;
                }

                Console.WriteLine($"Scanning files with: {clamscanPath}");

                // Build arguments: pass --database when using standalone clamscan with a known db directory
                string arguments;
                bool isClamscan = Path.GetFileNameWithoutExtension(clamscanPath).Equals("clamscan", StringComparison.OrdinalIgnoreCase);
                if (isClamscan && !string.IsNullOrEmpty(databaseDirectory) && Directory.Exists(databaseDirectory))
                    arguments = $"--database=\"{databaseDirectory}\" --infected --remove \"{directory}\"";
                else
                    arguments = $"--infected --remove \"{directory}\"";

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = clamscanPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Console.WriteLine($"clamscan standard output: {standardOutput}");
                    if (!string.IsNullOrEmpty(errorOutput))
                        Console.WriteLine($"clamscan error output: {errorOutput}");

                    if (process.ExitCode != 0 && process.ExitCode != 1)
                    {
                        // Exit code 0 = no virus found, 1 = virus found, other = error
                        Console.WriteLine($"clamscan failed with exit code {process.ExitCode}: {errorOutput}");
                    }
                    else
                    {
                        Console.WriteLine("Scan completed successfully.");
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                Console.WriteLine(win32Ex.ToString());
                Console.WriteLine("Ensure clamscan is installed and available in the system's PATH.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to scan files: {ex.Message}");
            }
        }

        private static void QuarantineInfectedFiles(string sourceDir, string quarantineDir)
        {
            try
            {
                Console.WriteLine("Quarantining infected files...");
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(quarantineDir, fileName);
                    File.Move(file, destFile);
                    Console.WriteLine($"Quarantined: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to quarantine files: {ex.Message}");
            }
        }
    }
}
