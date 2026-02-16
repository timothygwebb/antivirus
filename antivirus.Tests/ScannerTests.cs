using System;
using Xunit;

namespace antivirus.Tests
{
    public class ScannerTests
    {
        [Fact]
        public void IsRunningFromRemovable_ReturnsFalse_WhenNotRemovable()
        {
            // Arrange
            // Simulate a non-removable drive (mocking or stubbing would be ideal here)

            // Act
            bool result = Scanner.IsRunningFromRemovable();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRunningFromRemovable_ReturnsTrue_WhenRemovable()
        {
            // Arrange
            // Simulate a removable drive (mocking or stubbing would be ideal here)

            // Act
            bool result = Scanner.IsRunningFromRemovable();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Scan_DoesNotThrowException_ForValidPath()
        {
            // Arrange
            string validPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Act & Assert
            var exception = Record.Exception(() => Scanner.Scan(validPath));
            Assert.Null(exception);
        }

        [Fact]
        public void Scan_LogsError_ForInvalidPath()
        {
            // Arrange
            string invalidPath = "Z:\\NonExistentPath";

            // Act
            Scanner.Scan(invalidPath);

            // Assert
            // Verify that an error was logged (mocking Logger would be ideal here)
        }
    }
}