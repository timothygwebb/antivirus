using System.IO;

namespace antivirus
{
    public class Scanner
    {
        public static void Scan(string input)
        {
            Logger.LogInfo("Scanning started", Array.Empty<object>());
            if (Directory.Exists(input))
            {
                Logger.LogInfo("Scanning directory: " + input, Array.Empty<object>());
                string[] files = Directory.GetFiles(input);
                if (files.Length == 0)
                {
                    string msg = "Directory is empty: " + input;
                    Logger.LogWarning(msg, Array.Empty<object>());
                    System.Console.WriteLine(msg);
                }
                else
                {
                    foreach (string file in files)
                    {
                        ScanFile(file);
                    }
                }
            }
            else if (File.Exists(input))
            {
                Logger.LogInfo("Scanning file: " + input, Array.Empty<object>());
                ScanFile(input);
            }
            else
            {
                Logger.LogError("Path not found: " + input, Array.Empty<object>());
            }
            Logger.LogInfo("Scanning finished", Array.Empty<object>());
        }

        private static void ScanFile(string filePath)
        {
            // Simulate detection: mark .exe files as infected
            if (filePath.EndsWith(".exe"))
            {
                Logger.LogError("Infected file detected: " + filePath, Array.Empty<object>());
                Quarantine.QuarantineFile(filePath);
            }
            else
            {
                Logger.LogResult("File clean: " + filePath, Array.Empty<object>());
            }
        }
    }
}
