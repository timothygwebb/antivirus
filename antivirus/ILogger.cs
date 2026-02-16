namespace antivirus
{
    public interface ILogger
    {
        void LogError(string message, object[] args);
        void LogWarning(string message, object[] args);
        void LogInfo(string message, object[] args);
        void LogResult(string message, object[] args);
    }
}