namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_DefaultConstructor_IsValid()
    {
        // Arrange & Act
        var result = new ValidationResult();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_WithErrors_IsInvalid()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = new ValidationResult(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("Error 1");
        result.Errors.Should().Contain("Error 2");
    }

    [Fact]
    public void ValidationResult_WithNullErrors_IsValid()
    {
        // Arrange & Act
        var result = new ValidationResult(null!);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_AddError_AddsError()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("Test error");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Test error");
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
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Success_ReturnsValidResult()
    {
        // Arrange & Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Failure_WithSingleError_ReturnsInvalidResult()
    {
        // Arrange & Act
        var result = ValidationResult.Failure("Test error");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Test error");
    }

    [Fact]
    public void ValidationResult_Failure_WithMultipleErrors_ReturnsInvalidResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }
}
