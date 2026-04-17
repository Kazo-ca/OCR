namespace KazoOCR.Tests;

using KazoOCR.Core;

public class OcrSettingsTests
{
    [Fact]
    public void OcrSettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new OcrSettings();

        // Assert
        Assert.Equal("_OCR", settings.Suffix);
        Assert.Equal("fra+eng", settings.Languages);
        Assert.True(settings.Deskew);
        Assert.False(settings.Clean);
        Assert.True(settings.Rotate);
        Assert.Equal(1, settings.Optimize);
    }

    [Fact]
    public void OcrSettings_CanSetSuffix()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Suffix = "_PROCESSED";

        // Assert
        Assert.Equal("_PROCESSED", settings.Suffix);
    }

    [Fact]
    public void OcrSettings_CanSetLanguages()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Languages = "eng";

        // Assert
        Assert.Equal("eng", settings.Languages);
    }

    [Fact]
    public void OcrSettings_CanSetDeskew()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Deskew = false;

        // Assert
        Assert.False(settings.Deskew);
    }

    [Fact]
    public void OcrSettings_CanSetClean()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Clean = true;

        // Assert
        Assert.True(settings.Clean);
    }

    [Fact]
    public void OcrSettings_CanSetRotate()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Rotate = false;

        // Assert
        Assert.False(settings.Rotate);
    }

    [Fact]
    public void OcrSettings_CanSetOptimize()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        settings.Optimize = 3;

        // Assert
        Assert.Equal(3, settings.Optimize);
    }
}
