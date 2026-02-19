// Provides browser installer download logic for legacy Windows (ME/98/95)
using System;
using System.IO;
using System.Net;

namespace antivirus.Legacy
{
    public static class BrowserInstallers_Legacy
    {
        public static void EnsureLegacyBrowserInstallers()
        {
            string installersDir = Path.Combine(Directory.GetCurrentDirectory(), "BrowserInstallers");
            if (!Directory.Exists(installersDir))
                Directory.CreateDirectory(installersDir);

            // Only legacy browsers for Windows ME/98/95
            var browsers = new[] {
                new { Name = "K-Meleon", Url = "https://downloads.sourceforge.net/project/kmeleon/k-meleon/1.5.4/K-Meleon1.5.4.exe", File = Path.Combine(installersDir, "K-Meleon1.5.4.exe") },
                new { Name = "RetroZilla", Url = "https://o.rthost.win/gpc/files1.rt/retrozilla-2.2.exe", File = Path.Combine(installersDir, "RetroZilla-2.2.exe") }
            };

            foreach (var browser in browsers)
            {
                if (!File.Exists(browser.File))
                {
                    try
                    {
                        Console.WriteLine("Downloading " + browser.Name + " installer...");
                        using (var client = new WebClient())
                        {
                            client.DownloadFile(browser.Url, browser.File);
                        }
                        Console.WriteLine(browser.Name + " installer downloaded to " + browser.File);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to download " + browser.Name + ": " + ex.Message);
                    }
                }
            }
        }
    }
}
