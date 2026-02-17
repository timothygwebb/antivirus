using Xunit;

namespace antivirus.antivirus.Tests;

public class BasicTests
{
    [Fact]
    public static void SampleTest()
    {
        // Arrange
        int expected = 4;
        int actual = 2 + 2;

        // Assert
        Assert.Equal(expected, actual);
    }
}
