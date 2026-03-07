using System;

namespace antivirus.Legacy
{
    public static class Logger
    {
        public static void LogInfo(string message, object[] _args) => Console.WriteLine($"INFO: {message}");
        public static void LogError(string message, object[] _args) => Console.WriteLine($"ERROR: {message}");
        public static void LogError(string message, Exception ex, object[] _args) => Console.WriteLine($"ERROR: {message} Exception: {ex.Message}");
        public static void LogWarning(string message, object[] _args) => Console.WriteLine($"WARN: {message}");
    }
}
