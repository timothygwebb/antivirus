using System;
using System.Diagnostics;
using System.IO;
using antivirus;

namespace antivirus
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Program execution started.");
            Logger.LogInfo("Program started", Array.Empty<object>());

            // Check if the program is launched with the --browser-repair argument
            if (args.Length > 0 && args[0] == "--browser-repair")
            {
                Logger.LogInfo("Executing browser repair process.", Array.Empty<object>());
                BrowserRepair.RepairBrowsers();
                Logger.LogInfo("Browser repair process completed.", Array.Empty<object>());
                return;
            }

            // Dual OS compatibility: use legacy code if on Windows Me or similar
            if (IsLegacyWindows())
            {
                Logger.LogInfo("Running in legacy compatibility mode.", Array.Empty<object>());
                // Call legacy entry point if available
                try
                {
                    var legacyType = Type.GetType("antivirus.Program_Backup");
                    var legacyMain = legacyType?.GetMethod("Main");
                    legacyMain?.Invoke(null, new object[] { new string[0] });
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to invoke legacy entry point: " + ex.Message, Array.Empty<object>());
                }
                return;
            }

            try
            {
                // 1. MBR check and cleanse (prompt user)
                Logger.LogInfo("Checking for MBR infections...", Array.Empty<object>());
                if (MBRChecker.IsMBRSuspicious())
                {
                    Logger.LogWarning("Suspicious MBR detected!", Array.Empty<object>());
                    Console.WriteLine("Suspicious MBR detected! Attempt to cleanse? (y/n): ");
                    var resp = Console.ReadLine();
                    if (resp != null && resp.Trim().ToLower().StartsWith("y"))
                    {
                        if (MBRChecker.CleanseMBR())
                        {
                            Logger.LogInfo("MBR cleansed successfully.", Array.Empty<object>());
                            Console.WriteLine("MBR cleansed successfully.");
                        }
                        else
                        {
                            Logger.LogError("Failed to cleanse MBR.", Array.Empty<object>());
                            Console.WriteLine("Failed to cleanse MBR.");
                        }
                    }
                }
                else
                {
                    Logger.LogInfo("No suspicious MBR detected.", Array.Empty<object>());
                }

                // 2. Ensure ClamAV is installed
                if (!Scanner.EnsureClamAVInstalled())
                {
                    Logger.LogError("ClamAV is not fully configured. Program cannot proceed.", Array.Empty<object>());
                    Console.WriteLine("ClamAV is not fully configured. Program cannot proceed.");
                    Logger.LogInfo("Program finished", Array.Empty<object>());
                    Console.WriteLine("Scan complete. Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }

                // 3. Verify ClamAV definitions exist (EnsureClamAVInstalled already attempts updates)
                if (!Scanner.EnsureClamAVDefinitionsExist())
                {
                    Logger.LogError("ClamAV definitions are missing. Program cannot proceed.", Array.Empty<object>());
                    Console.WriteLine("ClamAV definitions are missing. Program cannot proceed.");
                    Logger.LogInfo("Program finished", Array.Empty<object>());
                    Console.WriteLine("Scan complete. Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }

                // 4. Get scan path
                Console.WriteLine("Enter a file or directory path to scan. Press Enter to use the default user profile directory:");
                string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    input = defaultPath;
                    Logger.LogInfo("Using default path: " + defaultPath, Array.Empty<object>());
                }
                Logger.LogInfo("Starting scan for path: " + input, Array.Empty<object>());
                Console.WriteLine("Scanning path: " + input);

                // 5. Scan for malware
                bool scanCompleted = Scanner.Scan(input);

                // 6. Launch browser repair as a separate process if scan completed
                if (scanCompleted)
                {
                    try
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = Environment.ProcessPath,
                            Arguments = "--browser-repair",
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                        Logger.LogInfo("Launched browser repair process.", Array.Empty<object>());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to launch browser repair process: " + ex.Message, Array.Empty<object>());
                    }
                }
                else
                {
                    Logger.LogInfo("Scan did not complete; skipping browser repair.", Array.Empty<object>());
                    Console.WriteLine("Scan did not complete; skipping browser repair.");
                }

                Logger.LogInfo("Program finished", Array.Empty<object>());
                Console.WriteLine("Scan complete. Press Enter to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Logger.LogError("An error occurred: " + ex.Message, Array.Empty<object>());
            }
        }

        private static bool IsLegacyWindows()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32Windows && (os.Version.Major < 5); // Windows Me/98/95
        }
    }
}