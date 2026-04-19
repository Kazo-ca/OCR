namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;
using KazoOCR.Docker;

public class WorkerTests
{
    [Fact]
    public void BuildOcrSettings_WithNoEnvVars_ReturnsDefaults()
    {
        // Arrange — clear all env vars
        ClearEnvironmentVariables();

        try
        {
            // Act
            var settings = Worker.BuildOcrSettings();

            // Assert
            settings.Suffix.Should().Be(Worker.DefaultSuffix);
            settings.Languages.Should().Be(Worker.DefaultLanguages);
            settings.Deskew.Should().Be(Worker.DefaultDeskew);
            settings.Clean.Should().Be(Worker.DefaultClean);
            settings.Rotate.Should().Be(Worker.DefaultRotate);
            settings.Optimize.Should().Be(Worker.DefaultOptimize);
        }
        finally
        {
            ClearEnvironmentVariables();
        }
    }

    [Fact]
    public void GetWatchPath_WithNoEnvVar_ReturnsDefault()
    {
        // Arrange
        Environment.SetEnvironmentVariable(Worker.EnvWatchPath, null);

        // Act
        var path = Worker.GetWatchPath();

        // Assert
        path.Should().Be(Worker.DefaultWatchPath);
    }

    [Fact]
    public void GetWatchPath_WithEnvVar_ReturnsEnvValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable(Worker.EnvWatchPath, "/custom/path");

        try
        {
            // Act
            var path = Worker.GetWatchPath();

            // Assert
            path.Should().Be("/custom/path");
        }
        finally
        {
            Environment.SetEnvironmentVariable(Worker.EnvWatchPath, null);
        }
    }

    [Fact]
    public void BuildOcrSettings_WithEnvVars_OverridesDefaults()
    {
        // Arrange
        Environment.SetEnvironmentVariable(Worker.EnvSuffix, "_PROCESSED");
        Environment.SetEnvironmentVariable(Worker.EnvLanguages, "eng");
        Environment.SetEnvironmentVariable(Worker.EnvDeskew, "false");
        Environment.SetEnvironmentVariable(Worker.EnvClean, "true");
        Environment.SetEnvironmentVariable(Worker.EnvRotate, "false");
        Environment.SetEnvironmentVariable(Worker.EnvOptimize, "3");

        try
        {
            // Act
            var settings = Worker.BuildOcrSettings();

            // Assert
            settings.Suffix.Should().Be("_PROCESSED");
            settings.Languages.Should().Be("eng");
            settings.Deskew.Should().BeFalse();
            settings.Clean.Should().BeTrue();
            settings.Rotate.Should().BeFalse();
            settings.Optimize.Should().Be(3);
        }
        finally
        {
            ClearEnvironmentVariables();
        }
    }

    [Fact]
    public void ParseBool_WithValidTrue_ReturnsTrue()
    {
        Worker.ParseBool("true", false).Should().BeTrue();
    }

    [Fact]
    public void ParseBool_WithValidFalse_ReturnsFalse()
    {
        Worker.ParseBool("false", true).Should().BeFalse();
    }

    [Fact]
    public void ParseBool_WithNull_ReturnsDefault()
    {
        Worker.ParseBool(null, true).Should().BeTrue();
        Worker.ParseBool(null, false).Should().BeFalse();
    }

    [Fact]
    public void ParseBool_WithInvalid_ReturnsDefault()
    {
        Worker.ParseBool("invalid", true).Should().BeTrue();
        Worker.ParseBool("invalid", false).Should().BeFalse();
    }

    [Fact]
    public void ParseInt_WithValidValue_ReturnsParsedValue()
    {
        Worker.ParseInt("3", 1).Should().Be(3);
    }

    [Fact]
    public void ParseInt_WithNull_ReturnsDefault()
    {
        Worker.ParseInt(null, 1).Should().Be(1);
    }

    [Fact]
    public void ParseInt_WithInvalid_ReturnsDefault()
    {
        Worker.ParseInt("abc", 1).Should().Be(1);
    }

    [Fact]
    public void DefaultConstants_MatchIssueSpecification()
    {
        // These values are specified in the issue
        Worker.DefaultWatchPath.Should().Be("/watch");
        Worker.DefaultSuffix.Should().Be("_OCR");
        Worker.DefaultLanguages.Should().Be("fra+eng");
        Worker.DefaultDeskew.Should().BeTrue();
        Worker.DefaultClean.Should().BeFalse();
        Worker.DefaultRotate.Should().BeTrue();
        Worker.DefaultOptimize.Should().Be(1);
    }

    [Fact]
    public void EnvironmentVariableNames_MatchSpecification()
    {
        Worker.EnvWatchPath.Should().Be("KAZO_WATCH_PATH");
        Worker.EnvSuffix.Should().Be("KAZO_SUFFIX");
        Worker.EnvLanguages.Should().Be("KAZO_LANGUAGES");
        Worker.EnvDeskew.Should().Be("KAZO_DESKEW");
        Worker.EnvClean.Should().Be("KAZO_CLEAN");
        Worker.EnvRotate.Should().Be("KAZO_ROTATE");
        Worker.EnvOptimize.Should().Be("KAZO_OPTIMIZE");
    }

    private static void ClearEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable(Worker.EnvWatchPath, null);
        Environment.SetEnvironmentVariable(Worker.EnvSuffix, null);
        Environment.SetEnvironmentVariable(Worker.EnvLanguages, null);
        Environment.SetEnvironmentVariable(Worker.EnvDeskew, null);
        Environment.SetEnvironmentVariable(Worker.EnvClean, null);
        Environment.SetEnvironmentVariable(Worker.EnvRotate, null);
        Environment.SetEnvironmentVariable(Worker.EnvOptimize, null);
    }
}
