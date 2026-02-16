using System;
using System.IO;
using Xunit;
using antivirus;

namespace antivirus.Tests
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
            LoggerStub.Reset();
            // Act
            Scanner.Scan(nonExistent);
            // Assert
            Assert.NotNull(LoggerStub.LastError);
            Assert.Contains($"Path not found: {nonExistent}", LoggerStub.LastError!);
        }

        // Additional tests for EnsureLegacyBrowserInstalled and Scan can be added with more advanced mocking
    }

    // Stub for Logger to capture logs
    public static class LoggerStub
    {
        private static readonly object _lock = new();
        private static string? _lastError;

        public static string? LastError
        {
            get
            {
                lock (_lock)
                {
                    return _lastError;
                }
            }
            set
            {
                lock (_lock)
                {
                    _lastError = value;
                }
            }
        }

        public static void Reset() => LastError = null;
        public static void LogError(string msg, object[] _1) => LastError = msg;
        public static void LogWarning(string msg, object[] _2) { }
        public static void LogInfo(string msg, object[] _3) { }
        public static void LogResult(string msg, object[] _4) { }
    }
}
