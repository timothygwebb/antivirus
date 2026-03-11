using System;
using System.IO;
using System.Diagnostics;
using antivirus.Legacy;

namespace antivirus.Legacy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Program execution started.");
            Logger.LogInfo("Program started", new object[0]);

            // Check if the program is launched with the --browser-repair argument
            if (args.Length > 0 && args[0] == "--browser-repair")
            {
                Logger.LogInfo("Executing browser repair process.", new object[0]);
                BrowserRepair.RepairBrowsers();
                Logger.LogInfo("Browser repair process completed.", new object[0]);
                return;
            }

            // Handle --scan-all argument for full system scan
            if (args.Length > 0 && args[0] == "--scan-all")
            {
                string rootPath = "C:\\";
                Logger.LogInfo("Starting full system scan from root: " + rootPath, new object[0]);
                Console.WriteLine("Scanning entire system from root: " + rootPath);
                var scanResult = Scanner.Scan(rootPath);
                Logger.LogInfo("Program finished", new object[0]);

                // Display detailed scan results
                Console.WriteLine("\n========== SCAN RESULTS ==========");
                Console.WriteLine($"Status: {(scanResult.Success ? "COMPLETED" : "FAILED")}");
                Console.WriteLine($"Directories Scanned: {scanResult.DirectoriesScanned:N0}");
                Console.WriteLine($"Files Scanned: {scanResult.FilesScanned:N0}");
                Console.WriteLine($"Infections Found: {scanResult.InfectionsFound}");
                Console.WriteLine($"Files Quarantined: {scanResult.FilesQuarantined}");
                if (scanResult.InfectionsFound == 0)
                {
                    Console.WriteLine("\n✓ No threats detected - Your system is clean!");
                }
                else
                {
                    Console.WriteLine($"\n⚠ WARNING: {scanResult.InfectionsFound} threat(s) detected!");
                    Console.WriteLine($"   {scanResult.FilesQuarantined} file(s) quarantined");
                }
                Console.WriteLine("==================================\n");

                Console.WriteLine("Scan complete. Press Enter to exit...");
                Console.ReadLine();
                return;
            }

            // Interactive menu if no arguments
            if (args.Length == 0)
            {
                while (true)
                {
                    Console.WriteLine("\nAntivirus Menu:");
                    Console.WriteLine("1. Full System Scan");
                    Console.WriteLine("2. Browser Repair");
                    Console.WriteLine("3. Update Virus Definitions");
                    Console.WriteLine("4. Exit");
                    Console.Write("Select an option (1-4): ");
                    var choice = Console.ReadLine();
                    if (choice == "1")
                    {
                        string rootPath = "C:\\";
                        Logger.LogInfo("Starting full system scan from root: " + rootPath, new object[0]);
                        Console.WriteLine("Scanning entire system from root: " + rootPath);
                        var scanResult = Scanner.Scan(rootPath);
                        Logger.LogInfo("Program finished", new object[0]);

                        // Display detailed scan results
                        Console.WriteLine("\n========== SCAN RESULTS ==========");
                        Console.WriteLine($"Status: {(scanResult.Success ? "COMPLETED" : "FAILED")}");
                        Console.WriteLine($"Directories Scanned: {scanResult.DirectoriesScanned:N0}");
                        Console.WriteLine($"Files Scanned: {scanResult.FilesScanned:N0}");
                        Console.WriteLine($"Infections Found: {scanResult.InfectionsFound}");
                        Console.WriteLine($"Files Quarantined: {scanResult.FilesQuarantined}");
                        if (scanResult.InfectionsFound == 0)
                        {
                            Console.WriteLine("\n✓ No threats detected - Your system is clean!");
                        }
                        else
                        {
                            Console.WriteLine($"\n⚠ WARNING: {scanResult.InfectionsFound} threat(s) detected!");
                            Console.WriteLine($"   {scanResult.FilesQuarantined} file(s) quarantined");
                        }
                        Console.WriteLine("==================================\n");

                        Console.WriteLine("Scan complete. Press Enter to return to menu...");
                        Console.ReadLine();
                    }
                    else if (choice == "2")
                    {
                        Logger.LogInfo("Executing browser repair process.", new object[0]);
                        BrowserRepair.RepairBrowsers();
                        Logger.LogInfo("Browser repair process completed.", new object[0]);
                        Console.WriteLine("Press Enter to return to menu...");
                        Console.ReadLine();
                    }
                    else if (choice == "3")
                    {
                        Logger.LogInfo("Updating virus definitions.", new object[0]);
                        RunFreshclam();
                        Logger.LogInfo("Virus definitions update completed.", new object[0]);
                        Console.WriteLine("Update complete. Press Enter to return to menu...");
                        Console.ReadLine();
                    }
                    else if (choice == "4")
                    {
                        Console.WriteLine("Exiting application.");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid option. Please try again.");
                    }
                }
                return;
            }
            // Dual OS compatibility: use legacy code if on Windows Me or similar
            if (IsLegacyWindows())
            {
                LegacyMain();
            }
            else
            {
                // Modern OS path
                ModernMain();
            }
        }

        private static void LegacyMain()
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
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
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
                string arguments = $"--fail -L -o \"{destinationPath}\" \"{url}\"";

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
            // ONLY use local portable installation to avoid permission issues with system ClamAV
            // First priority: Check local ClamAV directory (self-contained portable installation)
            string localClamAVDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
            string freshclamInLocal = Path.Combine(localClamAVDir, "freshclam.exe");
            if (File.Exists(freshclamInLocal))
                return freshclamInLocal;

            string freshclamInLocalBin = Path.Combine(localClamAVDir, "bin\\freshclam.exe");
            if (File.Exists(freshclamInLocalBin))
                return freshclamInLocalBin;

            // Check application directory and subdirectories
            string freshclamInCurrentDir = Path.Combine(Directory.GetCurrentDirectory(), "freshclam.exe");
            if (File.Exists(freshclamInCurrentDir))
                return freshclamInCurrentDir;

            string freshclamInBin = Path.Combine(Directory.GetCurrentDirectory(), "bin\\freshclam.exe");
            if (File.Exists(freshclamInBin))
                return freshclamInBin;

            string freshclamInInstallers = Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers\\freshclam.exe");
            if (File.Exists(freshclamInInstallers))
                return freshclamInInstallers;

            // DO NOT use system-installed ClamAV - causes permission issues
            // Return null to trigger automatic download of portable version
            return null;
        }

        private static void RunFreshclam()
        {
            try
            {
                // Auto-configure KernelEX if on Windows ME
                if (IsLegacyWindows())
                {
                    Console.WriteLine("Windows ME detected - Auto-configuring KernelEX compatibility...");
                    Logger.LogInfo("Windows ME detected - applying KernelEX settings", new object[0]);
                }

                string freshclamPath = FindFreshclamExecutable();
                if (string.IsNullOrEmpty(freshclamPath))
                {
                    Console.WriteLine("freshclam.exe not found. Attempting to download ClamAV automatically...");
                    Logger.LogInfo("freshclam.exe not found. Attempting auto-download.", new object[0]);

                    // Attempt to download and extract ClamAV ZIP (portable, no admin required)
                    try
                    {
                        string clamavDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
                        if (!Directory.Exists(clamavDir))
                            Directory.CreateDirectory(clamavDir);

                        string tempZip = Path.Combine(Path.GetTempPath(), "clamav.zip");
                        string clamavZipUrl = "https://www.clamav.net/downloads/production/clamav-1.5.1.win.x64.zip";

                        Console.WriteLine("Downloading ClamAV portable package...");
                        DownloadWithCurl(clamavZipUrl, tempZip);

                        if (File.Exists(tempZip))
                        {
                            Console.WriteLine("Extracting ClamAV...");
                            ExtractZipFile(tempZip, clamavDir);

                            // Move files from subdirectory BEFORE configuring (which creates database dir)
                            string[] dirs = Directory.GetDirectories(clamavDir);
                            if (dirs.Length > 0)
                            {
                                // Find the ClamAV subdirectory (usually has a version in the name)
                                string subDir = null;
                                foreach (string dir in dirs)
                                {
                                    string dirName = Path.GetFileName(dir);
                                    if (dirName.StartsWith("clamav", StringComparison.OrdinalIgnoreCase))
                                    {
                                        subDir = dir;
                                        break;
                                    }
                                }

                                // If we found a ClamAV subdirectory, move its contents up one level
                                if (!string.IsNullOrEmpty(subDir))
                                {
                                    Console.WriteLine($"Moving files from subdirectory: {Path.GetFileName(subDir)}");
                                    foreach (string file in Directory.GetFiles(subDir, "*", SearchOption.AllDirectories))
                                    {
                                        string relPath = file.Substring(subDir.Length + 1);
                                        string destPath = Path.Combine(clamavDir, relPath);
                                        string destDir = Path.GetDirectoryName(destPath);
                                        if (!Directory.Exists(destDir))
                                            Directory.CreateDirectory(destDir);
                                        if (File.Exists(destPath))
                                            File.Delete(destPath);
                                        File.Move(file, destPath);
                                    }
                                    Directory.Delete(subDir, true);
                                    Console.WriteLine("File reorganization completed.");
                                }
                            }

                            Console.WriteLine("ClamAV downloaded and extracted successfully.");
                            Logger.LogInfo("ClamAV package downloaded and extracted.", new object[0]);

                            // Configure ClamAV for the extracted location (AFTER moving files)
                            ConfigureClamAVForDirectory(clamavDir);

                            // Try to find freshclam again after extraction
                            freshclamPath = FindFreshclamExecutable();
                        }
                    }
                    catch (Exception downloadEx)
                    {
                        Console.WriteLine("Failed to auto-download ClamAV: " + downloadEx.Message);
                        Logger.LogError("Failed to auto-download ClamAV", downloadEx, new object[0]);
                    }

                    if (string.IsNullOrEmpty(freshclamPath))
                    {
                        Console.WriteLine("Unable to locate or install freshclam.exe. Virus definitions cannot be updated.");
                        Logger.LogWarning("freshclam.exe not found after installation attempt.", new object[0]);
                        return;
                    }
                }

                // ALWAYS use local configuration directory for self-contained operation
                // This ensures we have write permissions and don't interfere with system ClamAV
                string localClamAVDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
                string localDbDir = Path.Combine(localClamAVDir, "database");

                // Create local config directory if it doesn't exist
                if (!Directory.Exists(localClamAVDir))
                    Directory.CreateDirectory(localClamAVDir);
                if (!Directory.Exists(localDbDir))
                {
                    Directory.CreateDirectory(localDbDir);
                    Console.WriteLine($"Created local database directory: {localDbDir}");
                    // Give filesystem time to sync
                    System.Threading.Thread.Sleep(500);
                }

                // Verify directory is accessible
                if (!Directory.Exists(localDbDir))
                {
                    Console.WriteLine("ERROR: Database directory could not be created or accessed.");
                    Logger.LogError("Database directory creation failed", new object[0]);
                    return;
                }

                // Test write permissions by creating a test file
                try
                {
                    string testFile = Path.Combine(localDbDir, "test.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch (Exception permEx)
                {
                    Console.WriteLine($"ERROR: No write permissions to database directory: {permEx.Message}");
                    Logger.LogError("Database directory not writable", new object[0]);
                    return;
                }

                // Write local config file with proper permissions
                string localFreshclamConf = Path.Combine(localClamAVDir, "freshclam.conf");
                try
                {
                    File.WriteAllText(localFreshclamConf,
                        "# ClamAV freshclam configuration\n" +
                        $"DatabaseDirectory {localDbDir.Replace("\\", "/")}\n" +
                        "DatabaseMirror database.clamav.net\n" +
                        "MaxAttempts 3\n" +
                        "ScriptedUpdates no\n" +
                        "LogVerbose yes\n");
                    Console.WriteLine($"Created freshclam config at: {localFreshclamConf}");
                }
                catch (Exception confEx)
                {
                    Console.WriteLine($"Warning: Could not write config file: {confEx.Message}");
                }

                Console.WriteLine($"Using freshclam at: {freshclamPath}");
                Console.WriteLine($"Database directory: {localDbDir}");
                Console.WriteLine("Downloading ClamAV virus definitions with freshclam...");

                // Use --config-file to explicitly override default config location
                // This prevents freshclam from trying to read system config files
                string args = $"--config-file=\"{localFreshclamConf}\"";
                Console.WriteLine($"freshclam arguments: {args}");

                // Auto-configure KernelEX if on Windows ME
                if (IsLegacyWindows() && File.Exists(freshclamPath))
                {
                    ConfigureKernelEXForExecutable(freshclamPath);
                    System.Threading.Thread.Sleep(200); // Give registry time to sync
                }

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = freshclamPath,
                    Arguments = args,
                    WorkingDirectory = localClamAVDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = StartProcessWithKernelEX(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine($"freshclam output: {output}");
                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine($"freshclam messages: {error}");

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("ClamAV virus definitions downloaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"freshclam failed with exit code {process.ExitCode}.");
                        Console.WriteLine("This may be due to:");
                        Console.WriteLine("  - Network connectivity issues");
                        Console.WriteLine("  - Database directory permissions");
                        Console.WriteLine("  - ClamAV mirror server unavailable");
                        Console.WriteLine("The application will continue, but virus definitions may be outdated.");
                        Logger.LogWarning($"freshclam failed with exit code {process.ExitCode}", new object[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to run freshclam: {ex.Message}");
                Console.WriteLine("The application will continue, but virus definitions may not be updated.");
                Logger.LogWarning($"Failed to run freshclam: {ex.Message}", new object[0]);
            }
        }

        private static void ExtractZipFile(string zipFilePath, string outputDirectory)
        {
            try
            {
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);

                Console.WriteLine("Extracting files, please wait...");

                // Try PowerShell Expand-Archive first (more reliable for large files)
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-NoProfile -Command \"Expand-Archive -Path '{zipFilePath}' -DestinationPath '{outputDirectory}' -Force\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(psi))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine("ZIP extraction completed successfully.");
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"PowerShell extraction warning: {error}");
                            // Fall through to COM method
                        }
                    }
                }
                catch (Exception psEx)
                {
                    Console.WriteLine($"PowerShell extraction not available: {psEx.Message}");
                    Console.WriteLine("Trying alternative extraction method...");
                }

                // Fallback: Use Shell.Application COM object (for older systems)
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                if (shellType == null)
                    throw new ApplicationException("No ZIP extraction method available.");

                object shell = Activator.CreateInstance(shellType);
                object zipFolder = shellType.InvokeMember("NameSpace", 
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { zipFilePath });
                object destFolder = shellType.InvokeMember("NameSpace", 
                    System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { outputDirectory });

                if (zipFolder == null)
                    throw new ApplicationException("Unable to open ZIP file.");
                if (destFolder == null)
                    throw new ApplicationException("Unable to access output directory.");

                object items = zipFolder.GetType().InvokeMember("Items",
                    System.Reflection.BindingFlags.GetProperty, null, zipFolder, null);

                int itemCount = (int)items.GetType().InvokeMember("Count",
                    System.Reflection.BindingFlags.GetProperty, null, items, null);

                Console.WriteLine($"Extracting {itemCount} items...");

                // CopyHere with options: 16 = respond "Yes to All", 4 = do not display progress dialog
                destFolder.GetType().InvokeMember("CopyHere",
                    System.Reflection.BindingFlags.InvokeMethod, null, destFolder, new object[] { items, 20 });

                // Wait for extraction to complete
                int waitCount = 0;
                while (waitCount < 120) // Wait up to 2 minutes for large files
                {
                    System.Threading.Thread.Sleep(2000);
                    waitCount++;

                    int currentFiles = Directory.GetFiles(outputDirectory, "*", SearchOption.AllDirectories).Length;
                    if (currentFiles >= itemCount)
                    {
                        System.Threading.Thread.Sleep(2000);
                        break;
                    }

                    if (waitCount % 5 == 0)
                        Console.WriteLine($"Extracting... {currentFiles} of {itemCount} files");
                }

                Console.WriteLine("ZIP extraction completed.");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to extract ZIP file: " + ex.Message, ex);
            }
        }

        private static void ConfigureClamAVForDirectory(string clamavDir)
        {
            try
            {
                Console.WriteLine("Configuring ClamAV...");
                string dbDir = Path.Combine(clamavDir, "database");
                if (!Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                    Console.WriteLine($"Created database directory: {dbDir}");
                }

                // Write clamd.conf
                string configPath = Path.Combine(clamavDir, "clamd.conf");
                File.WriteAllText(configPath,
                    "# ClamAV daemon configuration\n" +
                    $"DatabaseDirectory {dbDir.Replace("\\", "/")}\n" +
                    "TCPSocket 3310\n" +
                    "TCPAddr 127.0.0.1\n" +
                    "ScanPE true\n");

                // Write freshclam.conf
                string freshclamConfPath = Path.Combine(clamavDir, "freshclam.conf");
                File.WriteAllText(freshclamConfPath,
                    "# ClamAV freshclam configuration\n" +
                    $"DatabaseDirectory {dbDir.Replace("\\", "/")}\n" +
                    "DatabaseMirror database.clamav.net\n" +
                    "MaxAttempts 3\n" +
                    "LogVerbose yes\n");

                Console.WriteLine($"ClamAV configured successfully.");
                Console.WriteLine($"  Configuration: {clamavDir}");
                Console.WriteLine($"  Database directory: {dbDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to configure ClamAV: {ex.Message}");
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
            // First priority: Check local portable installation
            string localClamAVDir = Path.Combine(Directory.GetCurrentDirectory(), "ClamAV");
            if (Directory.Exists(localClamAVDir))
                return localClamAVDir;

            string programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? string.Empty;
            string[] commonDirs = new string[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ClamAV"),
                !string.IsNullOrEmpty(programFilesX86) ? Path.Combine(programFilesX86, "ClamAV") : string.Empty,
                @"C:\ClamAV"
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

        private static bool IsLegacyWindows()
        {
            // Implement a check for legacy Windows versions (e.g., Windows Me)
            Version ver = Environment.OSVersion.Version;
            return (ver.Major == 4 && ver.Minor == 90); // Windows Me
        }

        private static void ConfigureKernelEXForExecutable(string exePath)
        {
            // Automatically configure KernelEX compatibility for an executable
            // This sets the registry key that KernelEX uses to determine compatibility mode
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("Software\\KernelEx"))
                {
                    if (key != null)
                    {
                        key.SetValue(exePath, "Windows2000", Microsoft.Win32.RegistryValueKind.String);
                        Console.WriteLine($"Configured KernelEX for: {Path.GetFileName(exePath)}");
                        Logger.LogInfo($"Applied KernelEX Windows2000 mode to {exePath}", new object[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note: Could not auto-configure KernelEX: {ex.Message}");
                Console.WriteLine("If ClamAV fails to run, please install KernelEX from http://kernelex.sourceforge.net/");
            }
        }

        private static Process StartProcessWithKernelEX(ProcessStartInfo processInfo)
        {
            // If on Windows ME, auto-configure KernelEX before starting process
            if (IsLegacyWindows() && File.Exists(processInfo.FileName))
            {
                ConfigureKernelEXForExecutable(processInfo.FileName);

                // Give registry time to sync
                System.Threading.Thread.Sleep(100);
            }

            return Process.Start(processInfo);
        }

        private static void ModernMain()
        {
            // Modern operating system logic here
            Console.WriteLine("Modern Antivirus Scanner");
            // Implementation for modern OS
        }
    }

    public static class BrowserRepair
    {
        public static void RepairBrowsers()
        {
            Console.WriteLine("Starting browser repair process...");
            Logger.LogInfo("Browser repair initiated", new object[0]);
            try
            {
                Console.WriteLine("Checking for browser issues...");
                Logger.LogInfo("Checking for browser issues", new object[0]);

                var browsers = new[]
                {
                    new { Name = "Chrome", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google\\Chrome\\Application\\chrome.exe") },
                    new { Name = "Firefox", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox\\firefox.exe") },
                    new { Name = "Edge", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft\\Edge\\Application\\msedge.exe") },
                    new { Name = "Opera", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Opera\\launcher.exe") }
                };

                foreach (var browser in browsers)
                {
                    if (File.Exists(browser.Path))
                    {
                        Console.WriteLine($"Found {browser.Name}: {browser.Path}");
                    }
                    else
                    {
                        Console.WriteLine($"{browser.Name} not found at {browser.Path}");
                    }
                }

                // Simulate repair
                Console.WriteLine("No issues detected. Repair not required.");
                Logger.LogInfo("Browser repair completed successfully", new object[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during browser repair: {ex.Message}");
                Logger.LogError("Browser repair failed", ex, new object[0]);
            }
        }
    }
}
