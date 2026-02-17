using System;
using System.IO;
using System.Runtime.InteropServices;
using antivirus;

namespace antivirus
{
    public class Program
    {
        static void Main(string[] _)
        {
            Console.WriteLine("Program execution started.");

            try
            {
                Logger.LogInfo("Program started", Array.Empty<object>());

                // Simplified execution flow for testing
                Console.WriteLine("Enter a file or directory path to scan. Press Enter to use the default user profile directory:");
                string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string? input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    input = defaultPath;
                    Logger.LogInfo("Using default path: " + defaultPath, Array.Empty<object>());
                }

                // Check for ClamAV definitions and engine
                bool hasDefinitions = ClamAVDefinitionsManager.DefinitionsExist();
                string clamdPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clamd.exe");
                bool hasClamd = File.Exists(clamdPath);
                if (!hasDefinitions)
                {
                    Console.WriteLine("ClamAV definitions not found. Please copy main.cvd, daily.cvd, and bytecode.cvd to the ClamAVDefs directory.");
                    Logger.LogError("ClamAV definitions not found. Please copy main.cvd, daily.cvd, and bytecode.cvd to the ClamAVDefs directory.", Array.Empty<object>());
                }
                if (!hasClamd)
                {
                    Console.WriteLine("clamd.exe not found. Please copy clamd.exe to your project directory.");
                    Logger.LogError("clamd.exe not found. Please copy clamd.exe to your project directory.", Array.Empty<object>());
                }
                // If network is available, attempt update (optional, does not fail if offline)
                if (hasDefinitions && hasClamd && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    Logger.LogInfo("Network available. You may update ClamAV definitions manually.", Array.Empty<object>());
                }
                if (!hasDefinitions || !hasClamd)
                {
                    Console.WriteLine("Cannot scan. Required files are missing.");
                    Logger.LogError("Cannot scan. Required files are missing.", Array.Empty<object>());
                    Logger.LogInfo("Program finished", Array.Empty<object>());
                    Console.WriteLine("Scan complete. Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }

                Logger.LogInfo("Starting scan for path: " + input, Array.Empty<object>());
                Console.WriteLine($"Scanning path: {input}");

                // Advanced scan (ClamAV, heuristics, exclusions, browser recovery, quarantine)
                Scanner.Scan(input);

                // Browser repair/recovery
                BrowserRepair.RepairBrowsers();

                Logger.LogInfo("Program finished", Array.Empty<object>());
                Console.WriteLine("Scan complete. Press Enter to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Logger.LogError("Unhandled exception: " + ex.Message, Array.Empty<object>());
            }
        }
    }
}