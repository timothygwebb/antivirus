using System;
using System.IO;
using System.Diagnostics;

namespace antivirus.Legacy
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine("Legacy Antivirus Recovery Tool");

            string installersDir = Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers");
            if (!Directory.Exists(installersDir))
            {
                Directory.CreateDirectory(installersDir);
            }

            // Step 1: Download and install ClamAV
            string clamavInstallerPath = Path.Combine(installersDir, "clamav-1.0.9.win.win32.msi");
            string clamavDownloadUrl = "https://clamav-site.s3.amazonaws.com/production/release_files/files/000/002/065/original/clamav-1.0.9.win.win32.msi?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAU7AK5ITMMOVIJYX4%2F20260223%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20260223T005519Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&X-Amz-Signature=3d59ea5619c1d8fb3120c16f6d6dbb17b2a62e982fb06cdeaaf508463856cda2"; // Updated URL
            DownloadWithCurl(clamavDownloadUrl, clamavInstallerPath);

            if (File.Exists(clamavInstallerPath))
            {
                InstallClamAV(clamavInstallerPath);
            }
            else
            {
                Console.WriteLine("ClamAV installer not found. Skipping installation.");
            }

            // Step 2: Configure ClamAV
            ConfigureClamAV();

            // Step 3: Scan files
            string scanDir = Path.Combine(Directory.GetCurrentDirectory(), "ScanDirectory");
            if (!Directory.Exists(scanDir))
            {
                Directory.CreateDirectory(scanDir);
            }
            ScanFiles(scanDir);

            // Step 4: Quarantine infected files
            string quarantineDir = Path.Combine(Directory.GetCurrentDirectory(), "Quarantine");
            if (!Directory.Exists(quarantineDir))
            {
                Directory.CreateDirectory(quarantineDir);
            }
            QuarantineInfectedFiles(scanDir, quarantineDir);
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
                string arguments = $"-o \"{destinationPath}\" \"{url}\"";

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
                Console.WriteLine($"Win32Exception: {win32Ex.Message}");
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
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking executable availability ({executableName}): {ex.Message}");
                return false;
            }
        }

        private static void InstallClamAV(string installerPath)
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
                    }
                    else
                    {
                        Console.WriteLine("ClamAV installation failed.");
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                Console.WriteLine($"Win32Exception: {win32Ex.Message}");
                Console.WriteLine("Ensure the installer path is correct and you have the necessary permissions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to install ClamAV: {ex.Message}");
            }
        }

        private static void ConfigureClamAV()
        {
            try
            {
                Console.WriteLine("Configuring ClamAV...");
                string configPath = Path.Combine(Directory.GetCurrentDirectory(), "clamd.conf");
                File.WriteAllText(configPath, "# Example ClamAV configuration\nLogFile C:\\clamav\\clamd.log\nDatabaseDirectory C:\\clamav\\db\nLocalSocket C:\\clamav\\clamd.sock\n");
                Console.WriteLine("ClamAV configured successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure ClamAV: {ex.Message}");
            }
        }

        private static void ScanFiles(string directory)
        {
            try
            {
                if (!IsExecutableAvailable("clamdscan"))
                {
                    Console.WriteLine("clamdscan is not available on this system. Please ensure ClamAV is installed and the executable is in the system's PATH.");
                    return;
                }

                Console.WriteLine("Scanning files...");
                string clamdscanPath = "clamdscan";
                string arguments = $"--infected --remove {directory}";

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = clamdscanPath,
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

                    Console.WriteLine($"clamdscan standard output: {standardOutput}");
                    Console.WriteLine($"clamdscan error output: {errorOutput}");

                    if (process.ExitCode != 0)
                    {
                        Console.WriteLine($"clamdscan failed with error: {errorOutput}");
                    }
                    else
                    {
                        Console.WriteLine("Scan completed successfully.");
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                Console.WriteLine($"Win32Exception: {win32Ex.Message}");
                Console.WriteLine("Ensure clamdscan is installed and available in the system's PATH.");
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
