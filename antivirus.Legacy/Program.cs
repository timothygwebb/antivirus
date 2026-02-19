using System;
using antivirus.Legacy;

namespace antivirus.Legacy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Legacy Antivirus Recovery Tool");
            // Ensure legacy browser installers are present
            BrowserInstallers_Legacy.EnsureLegacyBrowserInstallers();
            // TODO: Add legacy scanning and recovery logic here
            Console.WriteLine("[Legacy] Scanning and recovery would run here.");
            // ...existing legacy scan/quarantine logic...
        }
    }
}
