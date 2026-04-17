namespace KazoOCR.Tests;

using KazoOCR.Core;

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_DefaultConstructor_IsValid()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_WithErrors_IsInvalid()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = new ValidationResult(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
    }

    [Fact]
    public void ValidationResult_WithNullErrors_IsValid()
    {
        // Arrange & Act
        var result = new ValidationResult(null!);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_AddError_AddsError()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Test error");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Test error", result.Errors[0]);
    }

    [Fact]
    public void ValidationResult_AddError_WithNullOrEmpty_DoesNotAddError()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError(null!);
        result.AddError("");
        result.AddError("   ");

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_Success_ReturnsValidResult()
    {
        // Arrange & Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_Failure_WithSingleError_ReturnsInvalidResult()
    {
        // Arrange & Act
        var result = ValidationResult.Failure("Test error");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Test error", result.Errors[0]);
    }

    [Fact]
    public void ValidationResult_Failure_WithMultipleErrors_ReturnsInvalidResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }
}
