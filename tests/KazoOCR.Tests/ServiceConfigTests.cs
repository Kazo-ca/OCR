namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;

public class ServiceConfigTests
{
    [Fact]
    public void WatchFolderConfig_DefaultValues_AreCorrect()
    {
        var config = new WatchFolderConfig();

        config.Path.Should().BeEmpty();
        config.Suffix.Should().Be("_OCR");
        config.Languages.Should().Be("fra+eng");
        config.Deskew.Should().BeTrue();
        config.Clean.Should().BeFalse();
        config.Rotate.Should().BeTrue();
        config.Optimize.Should().Be(1);
    }

    [Fact]
    public void WatchFolderConfig_ToOcrSettings_CopiesAllValues()
    {
        var config = new WatchFolderConfig
        {
            Path = "/test/path",
            Suffix = "_processed",
            Languages = "eng+deu",
            Deskew = false,
            Clean = true,
            Rotate = false,
            Optimize = 2
        };

        var settings = config.ToOcrSettings();

        settings.Suffix.Should().Be("_processed");
        settings.Languages.Should().Be("eng+deu");
        settings.Deskew.Should().BeFalse();
        settings.Clean.Should().BeTrue();
        settings.Rotate.Should().BeFalse();
        settings.Optimize.Should().Be(2);
    }

    [Fact]
    public void ServiceConfig_DefaultValues_AreCorrect()
    {
        var config = new ServiceConfig();

        config.WatchFolders.Should().NotBeNull();
        config.WatchFolders.Should().BeEmpty();
    }

    [Fact]
    public void ServiceConfig_WithMultipleFolders_StoresAllFolders()
    {
        var config = new ServiceConfig
        {
            WatchFolders =
            [
                new WatchFolderConfig { Path = "/folder1" },
                new WatchFolderConfig { Path = "/folder2" },
                new WatchFolderConfig { Path = "/folder3" }
            ]
        };

        config.WatchFolders.Should().HaveCount(3);
        config.WatchFolders[0].Path.Should().Be("/folder1");
        config.WatchFolders[1].Path.Should().Be("/folder2");
        config.WatchFolders[2].Path.Should().Be("/folder3");
    }
}
