using System;
using System.IO;
using System.Net;

namespace antivirus
{
    public static class ClamAVDefinitionsManager
    {
        private static readonly string[] DefinitionFiles = new string[] { "main.cvd", "daily.cvd", "bytecode.cvd" };
    // Use the same ClamAVDir as Scanner: store ClamAV under LocalApplicationData\ClamAV
    public static readonly string ClamAVDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClamAV");

        // Checks for updates every 24 hours
        private static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(24);
        private static DateTime _lastUpdate = DateTime.MinValue;
        // Track attempts to avoid running freshclam repeatedly in a short time window
        private static DateTime _lastUpdateAttempt = DateTime.MinValue;
        private static readonly TimeSpan UpdateAttemptThrottle = TimeSpan.FromSeconds(60);

        public static void DownloadDefinitions()
        {
            Directory.CreateDirectory(ClamAVDir);
            // Prefer using freshclam if it's available in the ClamAV directory (it handles mirrors/auth transparently).
            string freshclamExe = Path.Combine(ClamAVDir, "freshclam.exe");
            if (File.Exists(freshclamExe))
            {
                try
                {
                    Logger.LogInfo("Attempting to update definitions with freshclam.exe", Array.Empty<object>());
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = freshclamExe,
                        WorkingDirectory = ClamAVDir,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var proc = System.Diagnostics.Process.Start(startInfo);
                    if (proc != null)
                    {
                        string outp = proc.StandardOutput.ReadToEnd();
                        string err = proc.StandardError.ReadToEnd();
                        proc.WaitForExit(60_000);
                        Logger.LogInfo($"freshclam (auto) output: {outp}", Array.Empty<object>());
                        if (!string.IsNullOrEmpty(err)) Logger.LogWarning($"freshclam (auto) error: {err}", Array.Empty<object>());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"freshclam auto-update failed: {ex.Message}", Array.Empty<object>());
                }
                // If definitions now exist, we're done
                if (DefinitionsExist())
                {
                    Logger.LogInfo("Definitions present after freshclam run.", Array.Empty<object>());
                    return;
                }
            }

            // freshclam didn't work or isn't available: try HTTP download with a browser-like user agent and mirror fallback
            string[] baseFiles = new string[] { "main", "daily", "bytecode" };
            string[] mirrors = new[] { "https://database.clamav.net/", "https://clamav.cis.uab.edu/clamav/", "https://updates.clamav.net/" };
            bool anyFailed = false;
            using (var client = new System.Net.Http.HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                foreach (var name in baseFiles)
                {
                    string[] extensions = new[] { ".cvd", ".cld" };
                    bool downloaded = false;
                    foreach (var mirror in mirrors)
                    {
                        foreach (var ext in extensions)
                        {
                            string url = mirror + name + ext;
                            string dest = Path.Combine(ClamAVDir, name + ext);
                            try
                            {
                                Logger.LogInfo($"Attempting to download ClamAV definition: {url}", Array.Empty<object>());
                                using var resp = client.GetAsync(url).GetAwaiter().GetResult();
                                if (!resp.IsSuccessStatusCode)
                                {
                                    Logger.LogWarning($"Failed to download {url}: HTTP {(int)resp.StatusCode}", Array.Empty<object>());
                                    continue;
                                }
                                using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    resp.Content.CopyToAsync(fs).GetAwaiter().GetResult();
                                }
                                Logger.LogInfo($"Downloaded ClamAV definition to {dest}", Array.Empty<object>());
                                downloaded = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Error downloading {url}: {ex.Message}", Array.Empty<object>());
                            }
                        }
                        if (downloaded) break;
                    }
                    if (!downloaded)
                    {
                        anyFailed = true;
                        Logger.LogError($"Failed to download definition {name}. Please manually download from https://www.clamav.net/downloads.", Array.Empty<object>());
                    }
                }
            }
            if (anyFailed)
            {
                Console.WriteLine("ClamAV definitions download failed. Please manually download main.cvd/.cld, daily.cvd/.cld, and bytecode.cvd/.cld from https://www.clamav.net/downloads and place them in the ClamAV directory.");
                Logger.LogError("One or more ClamAV definitions failed to download automatically.", Array.Empty<object>());
            }
        }

        public static bool DefinitionsExist()
        {
            foreach (var file in DefinitionFiles)
            {
                var localPath = Path.Combine(ClamAVDir, file);
                if (!File.Exists(localPath))
                    return false;
            }
            return true;
        }

        public static void EnsureDefinitionsUpToDate()
        {
            if (!DefinitionsExist() || DateTime.Now - _lastUpdate > UpdateInterval)
            {
                Logger.LogInfo("Updating ClamAV definitions...", Array.Empty<object>());
                DownloadDefinitions();
                _lastUpdate = DateTime.Now;
            }
        }

        // Notify the manager that definitions were updated externally (e.g., freshclam ran)
        public static void NotifyUpdated()
        {
            _lastUpdate = DateTime.Now;
        }

        // Return true if enough time has passed since last update attempt
        public static bool ShouldAttemptUpdate()
        {
            if (DateTime.Now - _lastUpdateAttempt > UpdateAttemptThrottle)
            {
                _lastUpdateAttempt = DateTime.Now;
                return true;
            }
            return false;
        }
    }
}
