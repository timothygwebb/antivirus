using System;
using System.Collections;
using System.IO;

namespace antivirus
{
    public class Definitions
    {
        private static readonly string DbPath = Path.Combine(Directory.GetCurrentDirectory(), "definitions.db");
        private static ArrayList signatures = new ArrayList();
        private static bool loaded = false;

        public static void LoadDefinitions()
        {
            signatures.Clear();
            if (File.Exists(DbPath))
            {
                foreach (var line in File.ReadAllLines(DbPath))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !signatures.Contains(trimmed))
                        signatures.Add(trimmed);
                }
                Logger.LogInfo("Loaded " + signatures.Count + " virus definitions from " + DbPath, new object[0]);
            }
            else
            {
                Logger.LogWarning("Definitions database not found: " + DbPath, new object[0]);
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
