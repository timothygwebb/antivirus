using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace antivirus
{
    public static class MBRChecker
    {
        // Reads the first 512 bytes of PhysicalDrive0 (MBR)
        [SupportedOSPlatform("windows")]
        public static bool IsMBRSuspicious()
        {
            try
            {
                using var fs = new FileStream(@"\\.\\PhysicalDrive0", FileMode.Open, FileAccess.Read);
                byte[] mbr = new byte[512];
                int bytesRead = 0;
                while (bytesRead < 512)
                {
                    int read = fs.Read(mbr, bytesRead, 512 - bytesRead);
                    if (read == 0) break;
                    bytesRead += read;
                }

                if (bytesRead < 512)
                {
                    Logger.LogWarning("Incomplete MBR read.", Array.Empty<object>());
                    return false;
                }

                string mbrText = System.Text.Encoding.ASCII.GetString(mbr);
                if (mbrText.Contains("CIH", StringComparison.OrdinalIgnoreCase) ||
                    mbrText.Contains("Chernobyl", StringComparison.OrdinalIgnoreCase) ||
                    mbrText.Contains("Mona", StringComparison.OrdinalIgnoreCase) ||
                    mbrText.Contains("Surprise", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not read MBR: {ex.Message}", Array.Empty<object>());
            }
            return false;
        }

        // Overwrites the MBR with zeros (dangerous!)
        [SupportedOSPlatform("windows")]
        public static bool CleanseMBR()
        {
            try
            {
                using var fs = new FileStream(@"\\.\\PhysicalDrive0", FileMode.Open, FileAccess.Write);
                byte[] zeros = new byte[512];
                fs.Write(zeros, 0, 512);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to cleanse MBR: {ex.Message}", Array.Empty<object>());
                return false;
            }
        }

        // Checks if the program is running with administrative privileges
        [SupportedOSPlatform("windows")]
        public static bool IsRunningAsAdministrator()
        {
            if (!OperatingSystem.IsWindows())
            {
                Logger.LogWarning("IsRunningAsAdministrator is only supported on Windows.", Array.Empty<object>());
                return false;
            }

            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
    }
}
