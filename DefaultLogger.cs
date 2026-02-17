namespace antivirus
{
    public class DefaultLogger : ILogger
    {
        public void LogError(string message, object[] args)
        {
            Console.Error.WriteLine(message, args);
        }

        public void LogWarning(string message, object[] args)
        {
            Console.WriteLine("WARNING: " + string.Format(message, args));
        }

        public void LogInfo(string message, object[] args)
        {
            Console.WriteLine("INFO: " + string.Format(message, args));
        }

        public void LogResult(string message, object[] args)
        {
            Console.WriteLine("RESULT: " + string.Format(message, args));
        }
    }
}