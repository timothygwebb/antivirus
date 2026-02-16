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
        public static string? LastError;
        public static void Reset() => LastError = null;
        public static void LogError(string msg, object[] _) => LastError = msg;
        public static void LogWarning(string msg, object[] _) { }
        public static void LogInfo(string msg, object[] _) { }
        public static void LogResult(string msg, object[] _) { }
    }
}
