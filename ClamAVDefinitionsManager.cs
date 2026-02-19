using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace antivirus
{
    public static class ClamAVDefinitionsManager
    {
        public static void UpdateDefinitions()
        {
            Logger.LogInfo("Updating ClamAV definitions...", new object[0]);

            try
            {
                Logger.LogInfo("Attempting to update definitions with freshclam.exe", new object[0]);

                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "freshclam.exe"),
                    Arguments = "--quiet",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (System.Diagnostics.Process proc = System.Diagnostics.Process.Start(startInfo))
                {
                    string outp = proc.StandardOutput.ReadToEnd();
                    string err = proc.StandardError.ReadToEnd();

                    Logger.LogInfo("freshclam (auto) output: " + outp, new object[0]);
                    if (!string.IsNullOrEmpty(err))
                    {
                        Logger.LogWarning("freshclam (auto) error: " + err, new object[0]);
                    }
                }

                Logger.LogInfo("Definitions present after freshclam run.", new object[0]);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("freshclam auto-update failed: " + ex.Message, new object[0]);
            }
        }

        public static void DownloadDefinitions()
        {
            try
            {
                Logger.LogInfo("Downloading ClamAV definitions...", new object[0]);

                string[] urls = { "https://www.clamav.net/downloads" };
                foreach (string url in urls)
                {
                    try
                    {
                        Logger.LogInfo("Attempting to download ClamAV definition: " + url, new object[0]);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream responseStream = response.GetResponseStream())
                        using (FileStream fileStream = new FileStream("definition.cvd", FileMode.Create, FileAccess.Write))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, bytesRead);
                            }
                        }

                        Logger.LogInfo("Downloaded ClamAV definition to definition.cvd", new object[0]);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Error downloading " + url + ": " + ex.Message, new object[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to download definitions: " + ex.Message, new object[0]);
            }
        }
    }
}
