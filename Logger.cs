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

    public class Logger : ILogger
    {
        private static readonly string LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "antivirus.log");
        private static bool logPathPrinted = false;

        public static void LogMessage(LogLevel level, string format, object[] args)
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
            string logEntry = "[" + level.ToString() + "] " + message;
            System.Console.WriteLine(logEntry);
            try
            {
                if (!logPathPrinted)
                {
                    System.Console.WriteLine($"Log file path: {LogFilePath}");
                    logPathPrinted = true;
                }
                File.AppendAllText(LogFilePath, logEntry + System.Environment.NewLine);
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        public void LogInfo(string format, object[] args)
        {
            LogMessage(LogLevel.Info, format, args);
        }

        public void LogWarning(string format, object[] args)
        {
            LogMessage(LogLevel.Warning, format, args);
        }

        public void LogError(string format, object[] args)
        {
            LogMessage(LogLevel.Error, format, args);
        }

        public void LogResult(string format, object[] args)
        {
            LogMessage(LogLevel.Result, format, args);
        }
    }
}
