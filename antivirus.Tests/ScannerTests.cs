using System;
using System.IO;
using Xunit;
using antivirus;

namespace antivirus.antivirus.Tests
{
    public class ScannerTests
    {
        [Fact]
        public static void IsRunningFromRemovable_ReturnsFalse_OnFixedDrive()
        {
            // Arrange: Current directory is almost always on a fixed drive in test env
            // Act
            bool result = Scanner.IsRunningFromRemovable();
            // Assert
            Assert.False(result);
        }

        [Fact]
        public static void Scan_NonExistentPath_LogsError()
        {
            // Arrange
            string nonExistent = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            LocalTestLogger logger = new LocalTestLogger();
            Scanner.SetLogger(logger); // Inject the test logger

            // Act
            Scanner.Scan(nonExistent);

            // Assert
            Assert.NotNull(logger.LastError);
            Assert.Contains($"Path not found: {nonExistent}", logger.LastError!);
        }

        // Additional tests for EnsureLegacyBrowserInstalled and Scan can be added with more advanced mocking
    }

    // Convert LocalTestLogger to a non-static class for dependency injection
    public class LocalTestLogger : ILogger
    {
        private static readonly object _lock = new();
        private static string? _lastError;

        public string? LastError
        {
            get
            {
                lock (_lock)
                {
                    return _lastError;
                }
            }
            private set
            {
                lock (_lock)
                {
                    _lastError = value;
                }
            }
        }

        public void Reset() => LastError = null;
        public void LogError(string message, object[] _) => LastError = message;
        public void LogWarning(string message, object[] _) { }
        public void LogInfo(string message, object[] _) { }
        public void LogResult(string message, object[] _) { }
    }
}
