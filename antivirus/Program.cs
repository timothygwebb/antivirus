using System;
using System.IO;
using System.Runtime.InteropServices;

namespace antivirus
{
    class Program
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

                Logger.LogInfo("Starting scan for path: " + input, Array.Empty<object>());
                Console.WriteLine($"Scanning path: {input}");

                // Simulate scanning
                Console.WriteLine("Scanning...");
                System.Threading.Thread.Sleep(2000); // Simulate delay

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