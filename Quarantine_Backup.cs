namespace antivirus
{
    public class Quarantine
    {
        public static void QuarantineFile(string filePath)
        {
            Logger.LogWarning("File quarantined: " + filePath, new object[0]);
        }
    }
}