using System;
using System.IO;

namespace antivirus
{
    class Program
    {
        static void Main(string[] _)
        {
            Logger.LogInfo("Program started", Array.Empty<object>());

            string defaultPath = "C:\\Users\\timot\\source\\repos\\antivirus";
            Console.WriteLine("Enter a file or directory path to scan (default: " + defaultPath + "). Press Enter to use the default:");
            string? input = Console.ReadLine();

            if (string.IsNullOrEmpty(input))
            {
                input = defaultPath;
                Logger.LogInfo("Using default path: " + defaultPath, Array.Empty<object>());
            }

            Scanner.Scan(input);

            Logger.LogInfo("Program finished", Array.Empty<object>());
        }
    }
}
