using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using antivirus;

namespace antivirus
{
    public class Program
    {
        static void Main(string[] args)
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

            // Dual OS compatibility: use legacy code if on Windows Me or similar
            if (IsLegacyWindows())
            {
                Logger.LogInfo("Running in legacy compatibility mode.", new object[0]);
                try
                {
                    // Launch legacy process if available (antivirus.Legacy.exe)
                    string legacyExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "antivirus.Legacy.exe");
                    if (File.Exists(legacyExe))
                    {
                        var proc = new Process();
                        proc.StartInfo = new ProcessStartInfo
                        {
                            FileName = legacyExe,
                            UseShellExecute = true
                        };
                        proc.Start();
                        Logger.LogInfo("Launched legacy antivirus process.", new object[0]);
                    }
                    else
                    {
                        // Fallback: call legacy entry point if running in same process
                        var legacyType = Type.GetType("antivirus.Program_Backup");
                        var legacyMain = legacyType?.GetMethod("Main");
                        if (legacyMain != null)
                        {
                            // Ensure legacy browser installers are present
                            var browserInstallersType = Type.GetType("antivirus.Legacy.BrowserInstallers_Legacy");
                            var ensureMethod = browserInstallersType?.GetMethod("EnsureLegacyBrowserInstallers");
                            ensureMethod?.Invoke(null, null);
                            legacyMain.Invoke(null, new object[] { new string[0] });
                        }
                        else
                        {
                            Logger.LogError("No legacy entry point found.", new object[0]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to launch legacy process: " + ex.Message, new object[0]);
                }
                return;
            }

            try
            {
                // 1. Check for MBR infections
                Scanner.ReadMBR();

                // 2. Ensure ClamAV is installed
                if (!Scanner.EnsureClamAVInstalled())
                {
                    Logger.LogError("ClamAV is not fully configured. Program cannot proceed.", new object[0]);
                    Console.WriteLine("ClamAV is not fully configured. Program cannot proceed.");
                    Logger.LogInfo("Program finished", new object[0]);
                    Console.WriteLine("Scan complete. Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }

                // 3. Verify ClamAV definitions exist (EnsureClamAVInstalled already attempts updates)
                if (!Scanner.EnsureClamAVDefinitionsExist())
                {
                    Logger.LogError("ClamAV definitions are missing. Program cannot proceed.", new object[0]);
                    Console.WriteLine("ClamAV definitions are missing. Program cannot proceed.");
                    Logger.LogInfo("Program finished", new object[0]);
                    Console.WriteLine("Scan complete. Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }

                // 4. Ensure browser installers for current OS
                Scanner.EnsureBrowserInstallers();

                // 5. Get scan path
                Console.WriteLine("Enter a file or directory path to scan. Press Enter to use the default user profile directory:");
                string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    input = defaultPath;
                    Logger.LogInfo("Using default path: " + defaultPath, new object[0]);
                }
                Logger.LogInfo("Starting scan for path: " + input, new object[0]);
                Console.WriteLine("Scanning path: " + input);

                // 6. Scan for malware
                bool scanCompleted = Scanner.Scan(input);

                // 7. Launch browser repair as a separate process if scan completed
                if (scanCompleted)
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = Assembly.GetExecutingAssembly().Location,
                            Arguments = "--browser-repair",
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                        Logger.LogInfo("Launched browser repair process.", new object[0]);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to launch browser repair process: " + ex.Message, new object[0]);
                    }
                }
                else
                {
                    Logger.LogInfo("Scan did not complete; skipping browser repair.", new object[0]);
                    Console.WriteLine("Scan did not complete; skipping browser repair.");
                }

                Logger.LogInfo("Program finished", new object[0]);
                Console.WriteLine("Scan complete. Press Enter to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Logger.LogError("An error occurred: " + ex.Message, new object[0]);
            }
        }

        private static bool IsLegacyWindows()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32Windows && (os.Version.Major < 5); // Windows Me/98/95
        }

        public static void DownloadWithCurl(string url, string destinationPath)
        {
            // TODO: Implement the method to download a file using curl.
            throw new NotImplementedException();
        }

        public static void InstallClamAV(string installerPath)
        {
            // TODO: Implement the method to install ClamAV.
            throw new NotImplementedException();
        }

        public static void ConfigureClamAV()
        {
            // TODO: Implement the method to configure ClamAV.
            throw new NotImplementedException();
        }

        public static void ScanFiles(string directoryPath)
        {
            // TODO: Implement the method to scan files in a directory.
            throw new NotImplementedException();
        }

        public static void QuarantineInfectedFiles(string sourceDirectory, string quarantineDirectory)
        {
            // TODO: Implement the method to quarantine infected files.
            throw new NotImplementedException();
        }
    }
}