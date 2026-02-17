using System;
using System.IO;
using System.Net;
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
                Logger.LogInfo("Program started", new object[0]);

                // Ensure ClamAV is installed and configured
                if (!Scanner.EnsureClamAVInstalled())
                {
                    Logger.LogError("ClamAV is not fully configured. Program cannot proceed.", new object[0]);
                    Console.WriteLine("ClamAV is not fully configured. Program cannot proceed.");
                    Logger.LogInfo("Program finished", new object[0]);
                    Console.WriteLine("Scan complete. Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }

                // Simplified execution flow for testing
                Console.WriteLine("Enter a file or directory path to scan. Press Enter to use the default user profile directory:");
                string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // Changed to Personal for compatibility
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    input = defaultPath;
                    Logger.LogInfo("Using default path: " + defaultPath, new object[0]);
                }

                Logger.LogInfo("Starting scan for path: " + input, new object[0]);
                Console.WriteLine("Scanning path: " + input);

                // Simplify 'new' expressions and collection initializations
                string[] urls = new string[] { "https://database.clamav.net/main.cvd", "https://database.clamav.net/daily.cvd", "https://database.clamav.net/bytecode.cvd" };

                // Ensure ClamAV definitions are present
                Scanner.EnsureDefinitionsDatabase();

                // Download ClamAV zip if necessary
                Scanner.DownloadClamAVZip();

                // Advanced scan (ClamAV, heuristics, exclusions, browser recovery, quarantine)
                Scanner.Scan(input);

                // Browser repair/recovery
                BrowserRepair.RepairBrowsers();

                Logger.LogInfo("Program finished", new object[0]);
                Console.WriteLine("Scan complete. Press Enter to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Logger.LogError("An error occurred: " + ex.Message, new object[0]);
            }
        }
    }
}