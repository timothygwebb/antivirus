using System;
using System.IO;
using System.Net;
using antivirus;
using System.Runtime.InteropServices;

namespace antivirus
{
    public class Program
    {
        static void Main(string[] _)
        {
            Console.WriteLine("Program execution started.");

            // Check the operating system version
            if (IsOldOperatingSystem())
            {
                Console.WriteLine("Running on an older operating system. Using compatibility mode.");
                RunCompatibilityMode();
            }
            else
            {
                Console.WriteLine("Running on a modern operating system.");
                RunModernMode();
            }
        }

        private static bool IsOldOperatingSystem()
        {
            // Check if the OS is Windows Me or older
            OperatingSystem os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32Windows && os.Version.Major == 4 && os.Version.Minor <= 90;
        }

        private static void RunCompatibilityMode()
        {
            // Use the backup files for compatibility
            MBRChecker_Backup.IsMBRSuspicious();
            Scanner_Backup.Scan("C:\\");
            BrowserRepair_Backup.RepairBrowsers();
            Quarantine_Backup.QuarantineFile("C:\\example.txt");
            ClamAVDefinitionsManager_Backup.EnsureDefinitionsUpToDate();
        }

        private static void RunModernMode()
        {
            // Use the default files for modern systems
            MBRChecker.IsMBRSuspicious();
            Scanner.Scan("C:\\");
            BrowserRepair.RepairBrowsers();
            Quarantine.QuarantineFile("C:\\example.txt");
            ClamAVDefinitionsManager.EnsureDefinitionsUpToDate();
        }
    }
}