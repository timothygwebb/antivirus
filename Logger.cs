namespace antivirus
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Result
    }

    public class Logger
    {
        public static void LogMessage(LogLevel level, string format, object[] args)
        {
            string message = string.Format(format, args);
            System.Console.WriteLine("[" + level.ToString() + "] " + message);
        }

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
    }
}
