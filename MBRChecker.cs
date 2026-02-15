using System;
using System.IO;
using System.Runtime.InteropServices;

namespace antivirus
{
    public static class MBRChecker
    {
        // Reads the first 512 bytes of PhysicalDrive0 (MBR)
        public static bool IsMBRSuspicious()
        {
            try
            {
                using (var fs = new FileStream(@"\\.\\PhysicalDrive0", FileMode.Open, FileAccess.Read))
                {
                    byte[] mbr = new byte[512];
                    fs.Read(mbr, 0, 512);
                    // Simple heuristic: check for known MBR virus signatures or non-standard boot code
                    // (For demo: flag if 'CIH', 'Chernobyl', 'Mona', 'Surprise' appear in ASCII)
                    string mbrText = System.Text.Encoding.ASCII.GetString(mbr);
                    if (mbrText.IndexOf("CIH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mbrText.IndexOf("Chernobyl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mbrText.IndexOf("Mona", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mbrText.IndexOf("Surprise", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Could not read MBR: {ex.Message}", Array.Empty<object>());
            }
            return false;
        }

        // Overwrites the MBR with zeros (dangerous!)
        public static bool CleanseMBR()
        {
            try
            {
                using (var fs = new FileStream(@"\\.\\PhysicalDrive0", FileMode.Open, FileAccess.Write))
                {
                    byte[] zeros = new byte[512];
                    fs.Write(zeros, 0, 512);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to cleanse MBR: {ex.Message}", Array.Empty<object>());
                return false;
            }
        }
    }
}
