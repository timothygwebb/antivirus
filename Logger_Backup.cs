using System.IO;

namespace antivirus
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Result
    }

    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "antivirus.log");
        private static bool logPathPrinted = false;

        public static void LogInfo(string format, object[] args)
        {
            LogMessage(LogLevel.Info, format, args);
        }

        public static void LogWarning(string format, object[] args)
        {
            LogMessage(LogLevel.Warning, format, args);
        }

        public static void LogError(string format, object[] args)
        {
            LogMessage(LogLevel.Error, format, args);
        }

        public static void LogResult(string format, object[] args)
        {
            LogMessage(LogLevel.Result, format, args);
        }

        private static void LogMessage(LogLevel level, string format, object[] args)
        {
            string message;
            if (args != null && args.Length > 0)
            {
                try
                {
                    message = string.Format(format, args);
                }
                catch (System.FormatException)
                {
                    message = format + " [Logger: FormatException]";
                }
            }
            else
            {
                message = format;
            }

            string logEntry = $"[{level}] {message}";
            File.AppendAllText(LogFilePath, logEntry + "\n");

            if (!logPathPrinted)
            {
                Console.WriteLine("Log file: " + LogFilePath);
                logPathPrinted = true;
            }
        }
    }
}