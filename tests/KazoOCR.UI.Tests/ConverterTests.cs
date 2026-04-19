// This file contains tests for UI converter classes
// These tests are compiled only when BuildMAUI=true (on Windows)
#if BUILD_MAUI
using FluentAssertions;
using KazoOCR.Core;
using KazoOCR.UI.Converters;

namespace KazoOCR.UI.Tests;

/// <summary>
/// Tests for the UI converter classes.
/// Note: These tests can run on any platform since converters are pure .NET classes.
/// </summary>
public class ConverterTests
{
    #region IntToVisibilityConverter Tests

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(-1, false)]
    public void IntToVisibilityConverter_Convert_ReturnsCorrectValue(int count, bool expected)
    {
        // Arrange
        var converter = new IntToVisibilityConverter();

        // Act
        var result = converter.Convert(count, typeof(bool), null, null!);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(5, false)]
    public void IntToVisibilityConverter_Convert_WithInverseParameter_ReturnsInvertedValue(int count, bool expected)
    {
        // Arrange
        var converter = new IntToVisibilityConverter();

        // Act
        var result = converter.Convert(count, typeof(bool), "inverse", null!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IntToVisibilityConverter_Convert_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var converter = new IntToVisibilityConverter();

        // Act
        var result = converter.Convert(null, typeof(bool), null, null!);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void IntToVisibilityConverter_ConvertBack_ThrowsNotSupportedException()
    {
        // Arrange
        var converter = new IntToVisibilityConverter();

        // Act & Assert
        var act = () => converter.ConvertBack(true, typeof(int), null, null!);
        act.Should().Throw<NotSupportedException>();
    }

    #endregion

    #region PercentToProgressConverter Tests

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(50.0, 0.5)]
    [InlineData(100.0, 1.0)]
    [InlineData(25.0, 0.25)]
    public void PercentToProgressConverter_Convert_ReturnsCorrectValue(double percent, double expected)
    {
        // Arrange
        var converter = new PercentToProgressConverter();

        // Act
        var result = converter.Convert(percent, typeof(double), null, null!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void PercentToProgressConverter_Convert_WithNonDoubleValue_ReturnsZero()
    {
        // Arrange
        var converter = new PercentToProgressConverter();

        // Act
        var result = converter.Convert("not a double", typeof(double), null, null!);

        // Assert
        result.Should().Be(0.0);
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 50.0)]
    [InlineData(1.0, 100.0)]
    [InlineData(0.25, 25.0)]
    public void PercentToProgressConverter_ConvertBack_ReturnsCorrectValue(double progress, double expected)
    {
        // Arrange
        var converter = new PercentToProgressConverter();

        // Act
        var result = converter.ConvertBack(progress, typeof(double), null, null!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void PercentToProgressConverter_ConvertBack_WithNonDoubleValue_ReturnsZero()
    {
        // Arrange
        var converter = new PercentToProgressConverter();

        // Act
        var result = converter.ConvertBack("not a double", typeof(double), null, null!);

        // Assert
        result.Should().Be(0.0);
    }

    #endregion
}
#endif
