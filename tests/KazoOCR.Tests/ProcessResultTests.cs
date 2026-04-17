namespace KazoOCR.Tests;

using KazoOCR.Core;

public class ProcessResultTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        // Arrange & Act
        var result = new ProcessResult(0, "output", "error");

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("output", result.StandardOutput);
        Assert.Equal("error", result.StandardError);
    }

    [Fact]
    public void Constructor_WithNullOutput_SetsEmptyString()
    {
        // Arrange & Act
        var result = new ProcessResult(0, null!, "error");

        // Assert
        Assert.Equal(string.Empty, result.StandardOutput);
    }

    [Fact]
    public void Constructor_WithNullError_SetsEmptyString()
    {
        // Arrange & Act
        var result = new ProcessResult(0, "output", null!);

        // Assert
        Assert.Equal(string.Empty, result.StandardError);
    }

    #endregion

    #region IsSuccess Tests

    [Fact]
    public void IsSuccess_WithExitCodeZero_ReturnsTrue()
    {
        // Arrange
        var result = new ProcessResult(0, "output", string.Empty);

        // Act & Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void IsSuccess_WithNonZeroExitCode_ReturnsFalse()
    {
        // Arrange
        var result = new ProcessResult(1, string.Empty, "error");

        // Act & Assert
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(127)]
    [InlineData(255)]
    public void IsSuccess_WithVariousNonZeroExitCodes_ReturnsFalse(int exitCode)
    {
        // Arrange
        var result = new ProcessResult(exitCode, string.Empty, "error");

        // Act & Assert
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void Success_ReturnsSuccessfulResult()
    {
        // Arrange & Act
        var result = ProcessResult.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
    }

    [Fact]
    public void Success_WithOutput_ReturnsSuccessfulResultWithOutput()
    {
        // Arrange & Act
        var result = ProcessResult.Success("test output");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("test output", result.StandardOutput);
        Assert.Equal(string.Empty, result.StandardError);
    }

    [Fact]
    public void Failure_ReturnsFailedResult()
    {
        // Arrange & Act
        var result = ProcessResult.Failure(1, "error message");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal(string.Empty, result.StandardOutput);
        Assert.Equal("error message", result.StandardError);
    }

    [Fact]
    public void Failure_WithOutput_ReturnsFailedResultWithOutput()
    {
        // Arrange & Act
        var result = ProcessResult.Failure(1, "error message", "some output");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("some output", result.StandardOutput);
        Assert.Equal("error message", result.StandardError);
    }

    #endregion
}
