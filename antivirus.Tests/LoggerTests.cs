using System;
using System.IO;
using Xunit;

namespace antivirus.Tests
{
    public class LoggerTests
    {
        [Fact]
        public void LogInfo_WritesInfoMessageToLogFile()
        {
            // Arrange
            string testMessage = "Test Info Message";
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "antivirus.log");
            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            // Act
            Logger.LogInfo(testMessage, Array.Empty<object>());

            // Assert
            Assert.True(File.Exists(logFilePath));
            string logContent = File.ReadAllText(logFilePath);
            Assert.Contains("[Info] " + testMessage, logContent);
        }

        [Fact]
        public void LogWarning_WritesWarningMessageToLogFile()
        {
            // Arrange
            string testMessage = "Test Warning Message";
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "antivirus.log");
            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            // Act
            Logger.LogWarning(testMessage, Array.Empty<object>());

            // Assert
            Assert.True(File.Exists(logFilePath));
            string logContent = File.ReadAllText(logFilePath);
            Assert.Contains("[Warning] " + testMessage, logContent);
        }

        [Fact]
        public void LogError_WritesErrorMessageToLogFile()
        {
            // Arrange
            string testMessage = "Test Error Message";
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "antivirus.log");
            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            // Act
            Logger.LogError(testMessage, Array.Empty<object>());

            // Assert
            Assert.True(File.Exists(logFilePath));
            string logContent = File.ReadAllText(logFilePath);
            Assert.Contains("[Error] " + testMessage, logContent);
        }

        [Fact]
        public void LogResult_WritesResultMessageToLogFile()
        {
            // Arrange
            string testMessage = "Test Result Message";
            string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "antivirus.log");
            if (File.Exists(logFilePath)) File.Delete(logFilePath);

            // Act
            Logger.LogResult(testMessage, Array.Empty<object>());

            // Assert
            Assert.True(File.Exists(logFilePath));
            string logContent = File.ReadAllText(logFilePath);
            Assert.Contains("[Result] " + testMessage, logContent);
        }
    }
}