namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;

public class OcrSettingsTests
{
    [Fact]
    public void OcrSettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new OcrSettings();

        // Assert
        settings.Suffix.Should().Be("_OCR");
        settings.Languages.Should().Be("fra+eng");
        settings.Deskew.Should().BeTrue();
        settings.Clean.Should().BeFalse();
        settings.Rotate.Should().BeTrue();
        settings.Optimize.Should().Be(1);
    }

    [Fact]
    public void OcrSettings_CanSetSuffix()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Suffix = "_PROCESSED";

        // Assert
        settings.Suffix.Should().Be("_PROCESSED");
    }

    [Fact]
    public void OcrSettings_CanSetLanguages()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Languages = "eng";

        // Assert
        settings.Languages.Should().Be("eng");
    }

    [Fact]
    public void OcrSettings_CanSetDeskew()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Deskew = false;

        // Assert
        settings.Deskew.Should().BeFalse();
    }

    [Fact]
    public void OcrSettings_CanSetClean()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Clean = true;

        // Assert
        settings.Clean.Should().BeTrue();
    }

    [Fact]
    public void OcrSettings_CanSetRotate()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Rotate = false;

        // Assert
        settings.Rotate.Should().BeFalse();
    }

    [Fact]
    public void OcrSettings_CanSetOptimize()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Optimize = 3;

        // Assert
        settings.Optimize.Should().Be(3);
    }
}
