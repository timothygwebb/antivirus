using System;
using System.IO;

namespace antivirus
{
    public static class MBRChecker
    {
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
                var msg = "Could not read MBR: " + ex.Message;
                if (ex is UnauthorizedAccessException)
                    msg += " (Try running as administrator)";
                Logger.LogWarning(msg, new object[0]);
            }
            return false;
        }

        public static bool CleanseMBR()
        {
            try
            {
                using (FileStream fs = new FileStream(@"\\.\\PhysicalDrive0", FileMode.Open, FileAccess.Write))
                {
                    byte[] zeros = new byte[512];
                    fs.Write(zeros, 0, 512);
                }
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
