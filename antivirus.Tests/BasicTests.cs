using Xunit;

namespace antivirus.Tests
{
    public class BasicTests
    {
        [Fact]
        public void SampleTest()
        {
            // Arrange
            int expected = 4;
            int actual = 2 + 2;

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}