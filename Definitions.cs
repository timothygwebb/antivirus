using System;
using System.Collections.Generic;
using System.IO;

namespace antivirus
{
    public class Definitions
    {
        private static readonly string DbPath = Path.Combine(Directory.GetCurrentDirectory(), "definitions.db");
        private static HashSet<string> signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool loaded = false;

        public static void LoadDefinitions()
        {
            signatures.Clear();
            if (File.Exists(DbPath))
            {
                foreach (var line in File.ReadAllLines(DbPath))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                        signatures.Add(trimmed);
                }
                Logger.LogInfo($"Loaded {signatures.Count} virus definitions from {DbPath}", Array.Empty<object>());
            }
            else
            {
                Logger.LogWarning($"Definitions database not found: {DbPath}", Array.Empty<object>());
            }
            loaded = true;
        }

        public static bool IsKnownThreat(string fileName)
        {
            if (!loaded) LoadDefinitions();
            return signatures.Contains(Path.GetFileName(fileName));
        }
    }
}
