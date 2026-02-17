using System;
using System.Diagnostics;
using System.IO;
#if WINDOWS
using Microsoft.Win32;
#endif

namespace antivirus
{
    public static class BrowserRepair
    {
        // Paths to bundled installers
        private static readonly string[] BrowserInstallers = {
            "ChromeSetup.exe",
            "FirefoxSetup.exe",
            "OperaSetup.exe",
            "K-MeleonSetup.exe"
        };
        private static readonly string InstallerDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserInstallers");

        public static void RepairBrowsers()
        {
            Console.WriteLine("Checking browser access...");
            Logger.LogInfo("Checking browser access...", Array.Empty<object>());
            foreach (var installer in BrowserInstallers)
            {
                string installerPath = Path.Combine(InstallerDir, installer);
                if (!File.Exists(installerPath))
                {
                    Logger.LogWarning($"Installer not found: {installerPath}", Array.Empty<object>());
                    continue;
                }
                if (!IsBrowserInstalled(installer))
                {
                    Console.WriteLine($"Browser missing or broken. Reinstalling: {installer}");
                    Logger.LogInfo($"Browser missing or broken. Reinstalling: {installer}", Array.Empty<object>());
                    try
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = installerPath,
                                UseShellExecute = true
                            }
                        };
                        process.Start();
                        Logger.LogInfo($"Started installer: {installerPath}", Array.Empty<object>());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to start installer {installer}: {ex.Message}", Array.Empty<object>());
                    }
                }
                else
                {
                    Logger.LogInfo($"Browser already installed: {installer}", Array.Empty<object>());
                }
            }
        }

        private static bool IsBrowserInstalled(string installer)
        {
            // Simple checks for browser executables
            if (installer.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"));
            if (installer.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe"));
            if (installer.Contains("Opera", StringComparison.OrdinalIgnoreCase))
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Opera", "launcher.exe"));
            if (installer.Contains("K-Meleon", StringComparison.OrdinalIgnoreCase))
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "K-Meleon", "k-meleon.exe"));
            return false;
        }

        public static void FixBrowserRegistry()
        {
#if WINDOWS
            // Chrome
            try
            {
                using var chromeKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
                if (chromeKey != null)
                {
                    chromeKey.SetValue("", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"));
                    Logger.LogInfo("Restored Chrome registry key.", Array.Empty<object>());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to fix Chrome registry: {ex.Message}", Array.Empty<object>());
            }
            // Firefox
            try
            {
                using var firefoxKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe");
                if (firefoxKey != null)
                {
                    firefoxKey.SetValue("", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe"));
                    Logger.LogInfo("Restored Firefox registry key.", Array.Empty<object>());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to fix Firefox registry: {ex.Message}", Array.Empty<object>());
            }
            // Opera
            try
            {
                using var operaKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\launcher.exe");
                if (operaKey != null)
                {
                    operaKey.SetValue("", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Opera", "launcher.exe"));
                    Logger.LogInfo("Restored Opera registry key.", Array.Empty<object>());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to fix Opera registry: {ex.Message}", Array.Empty<object>());
            }
#else
            Logger.LogWarning("Registry operations are not supported on this platform.", Array.Empty<object>());
#endif
        }
    }
}
