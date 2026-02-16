using System;
using System.IO;
using System.Runtime.InteropServices;

namespace antivirus
{
    class Program
    {
        static void Main(string[] _)
        {
            Logger.LogInfo("Program started", Array.Empty<object>());

            // MBR check before scanning
            if (OperatingSystem.IsWindows())
            {
                if (!MBRChecker.IsRunningAsAdministrator())
                {
                    Console.WriteLine("This program must be run as an administrator to access the MBR.");
                    Logger.LogWarning("Program not running as administrator.", Array.Empty<object>());
                    return;
                }

                if (MBRChecker.IsMBRSuspicious())
                {
                    Console.WriteLine("WARNING: Suspicious Master Boot Record detected!");
                    Logger.LogWarning("Suspicious Master Boot Record detected!", Array.Empty<object>());
                    Console.WriteLine("Do you want to attempt to cleanse the MBR? (y/N): ");
                    var resp = Console.ReadLine();
                    if (resp != null && resp.Trim().ToLower() == "y")
                    {
                        if (MBRChecker.CleanseMBR())
                        {
                            Console.WriteLine("MBR cleanse attempted. Please reboot your system.");
                            Logger.LogInfo("MBR cleanse attempted.", Array.Empty<object>());
                        }
                        else
                        {
                            Console.WriteLine("Failed to cleanse MBR. Run as administrator.");
                            Logger.LogError("Failed to cleanse MBR.", Array.Empty<object>());
                        }
                    }
                }
            }

            string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Console.WriteLine($"Enter a file or directory path to scan (default: {defaultPath}). Press Enter to use the default:");
            string? input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                input = defaultPath;
                Logger.LogInfo("Using default path: " + defaultPath, Array.Empty<object>());
            }

            Scanner.Scan(input);

            Logger.LogInfo("Program finished", Array.Empty<object>());
        }
    }
}