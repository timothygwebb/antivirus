using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace antivirus
{
    public static class BrowserRepair
    {
        private static readonly string InstallerDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BrowserInstallers");

        public static void RepairBrowsers()
        {
            Console.WriteLine("Checking browser access...");
            Logger.LogInfo("Checking browser access...", new object[0]);

            var installers = GetInstallersForPlatform();
            foreach (var entry in installers)
            {
                string browserId = entry.browserId;
                string installerPath = entry.installerPath;
                bool localInstaller = entry.isLocalInstaller;

                if (IsBrowserInstalled(browserId))
                {
                    continue;
                }

                Console.WriteLine("Browser missing or broken. Reinstalling: " + browserId);
                Logger.LogInfo("Browser missing or broken. Reinstalling: " + browserId, new object[0]);

                if (localInstaller)
                {
                    if (!File.Exists(installerPath))
                    {
                        Logger.LogWarning("Installer not found: " + installerPath, new object[0]);
                        continue;
                    }

                    try
                    {
                        Process process = new Process();
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = installerPath,
                            UseShellExecute = true
                        };
                        process.Start();
                        Logger.LogInfo("Started installer: " + installerPath, new object[0]);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to start installer: " + ex.Message, new object[0]);
                    }
                }
                else
                {
                    // Non-Windows: try executing installer command(s) if available; try all methods and stop after success
                    var commands = ParseCommands(installerPath);
                    bool installed = false;
                    foreach (var cmd in commands)
                    {
                        Logger.LogInfo($"Attempting install for {browserId} using: {cmd}", new object[0]);
                        Console.WriteLine($"Attempting install for {browserId} using: {cmd}");
                        if (ExecuteShellCommand(cmd))
                        {
                            // Give the system a moment and re-check
                            System.Threading.Thread.Sleep(1000);
                            if (IsBrowserInstalled(browserId))
                            {
                                Logger.LogInfo($"Installation succeeded for {browserId} using: {cmd}", new object[0]);
                                installed = true;
                                break;
                            }
                        }
                    }

                    if (!installed)
                    {
                        Logger.LogInfo($"Automatic installer not available or failed for {browserId} on this platform. Try the following commands manually: {installerPath}", new object[0]);
                        Console.WriteLine($"Automatic installer not available or failed for {browserId} on this platform. Try the following commands manually: {installerPath}");
                    }
                }
            }
        }

        private static bool IsBrowserInstalled(string browserId)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                    switch (browserId.ToLowerInvariant())
                    {
                        case "chrome":
                        case "chromesetup.exe":
                            return File.Exists(Path.Combine(localApp, "Google", "Chrome", "Application", "chrome.exe"))
                                   || File.Exists(Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"))
                                   || File.Exists(Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"));
                        case "firefox":
                        case "firefoxsetup.exe":
                            return File.Exists(Path.Combine(programFiles, "Mozilla Firefox", "firefox.exe"))
                                   || File.Exists(Path.Combine(programFilesX86, "Mozilla Firefox", "firefox.exe"));
                        case "opera":
                        case "operasetup.exe":
                            return File.Exists(Path.Combine(programFiles, "Opera", "launcher.exe"))
                                   || File.Exists(Path.Combine(programFilesX86, "Opera", "launcher.exe"));
                        case "k-meleon":
                        case "k-meleonsetup.exe":
                            return File.Exists(Path.Combine(programFiles, "K-Meleon", "k-meleon.exe"));
                        default:
                            return false;
                    }
                }

                // macOS / Linux: check PATH for known executables
                string exe = browserId.ToLowerInvariant() switch
                {
                    "chrome" or "chromesetup.exe" => "google-chrome",
                    "firefox" or "firefoxsetup.exe" => "firefox",
                    "opera" or "operasetup.exe" => "opera",
                    _ => browserId.ToLowerInvariant()
                };

                return IsExecutableOnPath(exe);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsExecutableOnPath(string exeName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which",
                    Arguments = exeName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return false;
                p.WaitForExit(3000);
                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> ParseCommands(string installerPath)
        {
            if (string.IsNullOrWhiteSpace(installerPath)) return Enumerable.Empty<string>();
            // If multiple commands are provided separated by '|' allow trying them in sequence
            if (installerPath.Contains('|'))
            {
                return installerPath.Split('|').Select(s => s.Trim()).Where(s => s.Length > 0);
            }

            return new[] { installerPath };
        }

        private static bool ExecuteShellCommand(string command)
        {
            try
            {
                ProcessStartInfo psi;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // On Windows try PowerShell -Command
                    psi = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -NonInteractive -Command \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else
                {
                    // Use /bin/bash -lc to allow complex commands
                    psi = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-lc \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }

                using var p = Process.Start(psi);
                if (p == null) return false;
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(600000); // up to 10 minutes for package managers
                Logger.LogInfo($"Command output: {stdout}", new object[0]);
                if (!string.IsNullOrWhiteSpace(stderr)) Logger.LogWarning($"Command error output: {stderr}", new object[0]);
                return p.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to execute command: " + ex.Message, new object[0]);
                return false;
            }
        }

        private static (string browserId, string installerPath, bool isLocalInstaller)[] GetInstallersForPlatform()
        {
            // Try to load configuration first
            var configured = LoadInstallersFromConfig();
            if (configured != null && configured.Length > 0)
            {
                return configured;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new[] {
                    ("Chrome", Path.Combine(InstallerDir, "ChromeSetup.exe"), true),
                    ("Firefox", Path.Combine(InstallerDir, "FirefoxSetup.exe"), true),
                    ("Opera", Path.Combine(InstallerDir, "OperaSetup.exe"), true),
                    ("K-Meleon", Path.Combine(InstallerDir, "K-MeleonSetup.exe"), true)
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new[] {
                    ("Firefox", "sudo apt-get install -y firefox|sudo yum install -y firefox|sudo pacman -S --noconfirm firefox", false),
                    ("Chrome", "sudo apt-get install -y google-chrome-stable|sudo yum install -y google-chrome-stable", false),
                    ("Opera", "sudo apt-get install -y opera|sudo yum install -y opera", false)
                };
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new[] {
                    ("Firefox", "brew install --cask firefox", false),
                    ("Chrome", "brew install --cask google-chrome", false),
                    ("Opera", "brew install --cask opera", false)
                };
            }

            return Array.Empty<(string, string, bool)>();
        }

        private static (string browserId, string installerPath, bool isLocalInstaller)[] LoadInstallersFromConfig()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string configPath = Path.Combine(baseDir, "appsettings.json");
                if (!File.Exists(configPath)) return null;

                using var fs = File.OpenRead(configPath);
                using var doc = JsonDocument.Parse(fs);
                if (!doc.RootElement.TryGetProperty("BrowserInstallers", out var installersElem)) return null;

                var list = new List<(string, string, bool)>();
                foreach (var item in installersElem.EnumerateArray())
                {
                    string id = item.GetProperty("BrowserId").GetString();
                    string installer = item.GetProperty("Installer").GetString();
                    bool local = false;
                    if (item.TryGetProperty("IsLocal", out var isLocalElem)) local = isLocalElem.GetBoolean();
                    // If installer is relative, resolve against base dir
                    if (local && !Path.IsPathRooted(installer)) installer = Path.Combine(baseDir, installer);
                    list.Add((id, installer, local));
                }

                return list.ToArray();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to read installer configuration: " + ex.Message, new object[0]);
                return null;
            }
        }
    }
}
