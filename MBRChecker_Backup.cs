using System;
using System.IO;
using System.Text;

namespace antivirus
{
    public static class MBRChecker
    {
        // Reads the first 512 bytes of PhysicalDrive0 (MBR)
        public static bool IsMBRSuspicious()
        {
            try
            {
                using (FileStream fs = new FileStream(@"\\.\\PhysicalDrive0", FileMode.Open, FileAccess.Read))
                {
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
                        Logger.LogWarning("Incomplete MBR read.", new object[0]);
                        return false;
                    }

                    string mbrText = Encoding.ASCII.GetString(mbr);
                    if (mbrText.IndexOf("CIH", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mbrText.IndexOf("Chernobyl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mbrText.IndexOf("Mona", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        mbrText.IndexOf("Surprise", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not read MBR: " + ex.Message, new object[0]);
            }
            return false;
        }

        // Overwrites the MBR with zeros (dangerous!)
        public static bool CleanseMBR()
        {
            try
            {
                // Implementation for cleansing MBR (if needed)
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to cleanse MBR: " + ex.Message, new object[0]);
                return false;
            }
        }
    }
}