using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using antivirus;

namespace antivirus
{
    public class Program
    {
        static async Task Main(string[] _)
        {
            Console.WriteLine("Program execution started.");

            try
            {
                Logger.LogInfo("Program started", Array.Empty<object>());

                // Ensure ClamAV is installed and configured
                if (!Scanner.EnsureClamAVInstalled())
                {
                    Logger.LogError("ClamAV is not fully configured. Program cannot proceed.", Array.Empty<object>());
                    Console.WriteLine("ClamAV is not fully configured. Program cannot proceed.");
                    Logger.LogInfo("Program finished", Array.Empty<object>());
                    Console.WriteLine("Scan complete. Press Enter to exit...");
                    Console.ReadLine();
                    return;
                }

                // Simplified execution flow for testing
                Console.WriteLine("Enter a file or directory path to scan. Press Enter to use the default user profile directory:");
                string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string? input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    input = defaultPath;
                    Logger.LogInfo("Using default path: " + defaultPath, Array.Empty<object>());
                }

                Logger.LogInfo("Starting scan for path: " + input, Array.Empty<object>());
                Console.WriteLine($"Scanning path: {input}");

                // Simplify 'new' expressions and collection initializations
                string[] urls = { "https://database.clamav.net/main.cvd", "https://database.clamav.net/daily.cvd", "https://database.clamav.net/bytecode.cvd" };

                // Ensure ClamAV definitions are present
                Scanner.EnsureDefinitionsDatabase();

                // Download ClamAV zip if necessary
                Scanner.DownloadClamAVZip();

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

            Console.WriteLine("Scan complete. Press Enter to exit...");
            Console.ReadLine();
        }

        private static async Task<bool> DownloadClamAVDefinitionsWithRetryAsync()
        {
            int retryCount = 3;
            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    // Simplify collection initialization
                    string[] urls =
                    {
                        "https://database.clamav.net/main.cvd",
                        "https://database.clamav.net/daily.cvd",
                        "https://database.clamav.net/bytecode.cvd"
                    };

                    string definitionsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClamAVDefs");
                    Directory.CreateDirectory(definitionsPath);

                    using HttpClient client = new();
                    foreach (var url in urls)
                    {
                        string fileName = Path.GetFileName(url);
                        string destinationPath = Path.Combine(definitionsPath, fileName);
                        var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fileStream);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt} failed to download ClamAV definitions: {ex.Message}");
                    if (attempt == retryCount)
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        private static async Task<bool> DownloadClamdWithRetryAsync()
        {
            int retryCount = 3;
            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    string clamdUrl = "https://www.clamav.net/downloads/production/clamd.exe";
                    string destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clamd.exe");

                    using HttpClient client = new HttpClient();
                    var response = await client.GetAsync(clamdUrl);
                    response.EnsureSuccessStatusCode();
                    await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fileStream);

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Attempt {attempt} failed to download clamd.exe: {ex.Message}");
                    if (attempt == retryCount)
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }
}