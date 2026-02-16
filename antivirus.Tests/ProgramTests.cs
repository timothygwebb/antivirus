using System;
using Xunit;

namespace antivirus.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void Main_DoesNotThrowException()
        {
            // Arrange
            string[] args = Array.Empty<string>();

            // Act & Assert
            var exception = Record.Exception(() => Program.Main(args));
            Assert.Null(exception);
        }
    }
}