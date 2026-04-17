namespace KazoOCR.Tests;

using KazoOCR.Core;

public class OcrProcessRunnerTests
{
    #region BuildOcrArguments Tests

    [Fact]
    public void BuildOcrArguments_WithDefaultSettings_ReturnsCorrectArguments()
    {
        // Arrange
        var settings = new OcrSettings();

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.Contains("--deskew", result);
        Assert.Contains("--rotate-pages", result);
        Assert.Contains("--optimize 1", result);
        Assert.Contains("-l fra+eng", result);
        Assert.DoesNotContain("--clean", result);
    }

    [Fact]
    public void BuildOcrArguments_WithCleanEnabled_IncludesCleanFlag()
    {
        // Arrange
        var settings = new OcrSettings { Clean = true };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.Contains("--clean", result);
    }

    [Fact]
    public void BuildOcrArguments_WithDeskewDisabled_ExcludesDeskewFlag()
    {
        // Arrange
        var settings = new OcrSettings { Deskew = false };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.DoesNotContain("--deskew", result);
    }

    [Fact]
    public void BuildOcrArguments_WithRotateDisabled_ExcludesRotateFlag()
    {
        // Arrange
        var settings = new OcrSettings { Rotate = false };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.DoesNotContain("--rotate-pages", result);
    }

    [Fact]
    public void BuildOcrArguments_WithCustomOptimizeLevel_IncludesCorrectLevel()
    {
        // Arrange
        var settings = new OcrSettings { Optimize = 3 };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.Contains("--optimize 3", result);
    }

    [Fact]
    public void BuildOcrArguments_WithCustomLanguage_IncludesCorrectLanguage()
    {
        // Arrange
        var settings = new OcrSettings { Languages = "eng" };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.Contains("-l eng", result);
    }

    [Fact]
    public void BuildOcrArguments_WithEmptyLanguage_ExcludesLanguageFlag()
    {
        // Arrange
        var settings = new OcrSettings { Languages = "" };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.DoesNotContain("-l", result);
    }

    [Fact]
    public void BuildOcrArguments_WithAllOptionsEnabled_IncludesAllFlags()
    {
        // Arrange
        var settings = new OcrSettings
        {
            Deskew = true,
            Clean = true,
            Rotate = true,
            Optimize = 2,
            Languages = "fra+eng+deu"
        };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.Contains("--deskew", result);
        Assert.Contains("--clean", result);
        Assert.Contains("--rotate-pages", result);
        Assert.Contains("--optimize 2", result);
        Assert.Contains("-l fra+eng+deu", result);
    }

    [Fact]
    public void BuildOcrArguments_WithAllOptionsDisabled_ReturnsMinimalArguments()
    {
        // Arrange
        var settings = new OcrSettings
        {
            Deskew = false,
            Clean = false,
            Rotate = false,
            Optimize = 0,
            Languages = ""
        };

        // Act
        var result = OcrProcessRunner.BuildOcrArguments(settings);

        // Assert
        Assert.DoesNotContain("--deskew", result);
        Assert.DoesNotContain("--clean", result);
        Assert.DoesNotContain("--rotate-pages", result);
        Assert.Contains("--optimize 0", result);
        Assert.DoesNotContain("-l", result);
    }

    #endregion

    #region ConvertToWslPath Tests

    [Fact]
    public void ConvertToWslPath_WithDriveLetter_ReturnsWslPath()
    {
        // Arrange
        var windowsPath = @"C:\Users\Test\file.pdf";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(windowsPath);

        // Assert
        Assert.Equal("/mnt/c/Users/Test/file.pdf", result);
    }

    [Fact]
    public void ConvertToWslPath_WithLowercaseDriveLetter_ReturnsWslPath()
    {
        // Arrange
        var windowsPath = @"d:\Documents\file.pdf";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(windowsPath);

        // Assert
        Assert.Equal("/mnt/d/Documents/file.pdf", result);
    }

    [Fact]
    public void ConvertToWslPath_WithUppercaseDriveLetter_ReturnsLowercaseMount()
    {
        // Arrange
        var windowsPath = @"D:\Documents\file.pdf";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(windowsPath);

        // Assert
        Assert.Equal("/mnt/d/Documents/file.pdf", result);
    }

    [Fact]
    public void ConvertToWslPath_WithNestedPath_ReturnsCorrectPath()
    {
        // Arrange
        var windowsPath = @"C:\Users\John Doe\Documents\PDFs\input file.pdf";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(windowsPath);

        // Assert
        Assert.Equal("/mnt/c/Users/John Doe/Documents/PDFs/input file.pdf", result);
    }

    [Fact]
    public void ConvertToWslPath_WithDriveLetterOnly_ReturnsRootMount()
    {
        // Arrange
        var windowsPath = @"C:";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(windowsPath);

        // Assert
        Assert.Equal("/mnt/c", result);
    }

    [Fact]
    public void ConvertToWslPath_WithDriveAndBackslash_ReturnsRootMount()
    {
        // Arrange
        var windowsPath = @"C:\";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(windowsPath);

        // Assert
        Assert.Equal("/mnt/c/", result);
    }

    [Fact]
    public void ConvertToWslPath_WithRelativePath_ReplacesBackslashes()
    {
        // Arrange
        var windowsPath = @"folder\subfolder\file.pdf";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(windowsPath);

        // Assert
        Assert.Equal("folder/subfolder/file.pdf", result);
    }

    [Fact]
    public void ConvertToWslPath_WithUnixPath_ReturnsUnchanged()
    {
        // Arrange
        var unixPath = "/home/user/file.pdf";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(unixPath);

        // Assert
        Assert.Equal("/home/user/file.pdf", result);
    }

    [Fact]
    public void ConvertToWslPath_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var emptyPath = "";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(emptyPath);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ConvertToWslPath_WithNull_ReturnsEmptyString()
    {
        // Arrange
        string? nullPath = null;

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(nullPath!);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConvertToWslPath_WithWhitespace_ReturnsWhitespace()
    {
        // Arrange
        var whitespacePath = "   ";

        // Act
        var result = OcrProcessRunner.ConvertToWslPath(whitespacePath);

        // Assert
        Assert.Equal("   ", result);
    }

    #endregion

    #region BuildProcessStartInfo Tests

    [Fact]
    public void BuildProcessStartInfo_OnNonWindows_ReturnsDirectOcrmypdfCommand()
    {
        // Skip on Windows since we're testing non-Windows behavior
        if (OcrProcessRunner.IsWindows())
        {
            return;
        }

        // Arrange
        var settings = new OcrSettings();
        var inputPath = "/home/user/input.pdf";
        var outputPath = "/home/user/output.pdf";

        // Act
        var (fileName, arguments) = OcrProcessRunner.BuildProcessStartInfo(settings, inputPath, outputPath);

        // Assert
        Assert.Equal("ocrmypdf", fileName);
        Assert.Contains("--deskew", arguments);
        Assert.Contains("--rotate-pages", arguments);
        Assert.Contains("--optimize 1", arguments);
        Assert.Contains("-l fra+eng", arguments);
        Assert.Contains("\"/home/user/input.pdf\"", arguments);
        Assert.Contains("\"/home/user/output.pdf\"", arguments);
    }

    [Fact]
    public void BuildProcessStartInfo_OnWindows_ReturnsWslCommand()
    {
        // Skip on non-Windows since we're testing Windows behavior
        if (!OcrProcessRunner.IsWindows())
        {
            return;
        }

        // Arrange
        var settings = new OcrSettings();
        var inputPath = @"C:\Users\Test\input.pdf";
        var outputPath = @"C:\Users\Test\output.pdf";

        // Act
        var (fileName, arguments) = OcrProcessRunner.BuildProcessStartInfo(settings, inputPath, outputPath);

        // Assert
        Assert.Equal("wsl", fileName);
        Assert.Contains("ocrmypdf", arguments);
        Assert.Contains("--deskew", arguments);
        Assert.Contains("--rotate-pages", arguments);
        Assert.Contains("--optimize 1", arguments);
        Assert.Contains("-l fra+eng", arguments);
        Assert.Contains("/mnt/c/Users/Test/input.pdf", arguments);
        Assert.Contains("/mnt/c/Users/Test/output.pdf", arguments);
    }

    #endregion

    #region RunAsync Validation Tests

    [Fact]
    public async Task RunAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = new OcrProcessRunner();
        OcrSettings? settings = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => runner.RunAsync(settings!, "input.pdf", "output.pdf", CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithNullInputPath_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = new OcrProcessRunner();
        var settings = new OcrSettings();
        string? inputPath = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => runner.RunAsync(settings, inputPath!, "output.pdf", CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithNullOutputPath_ThrowsArgumentNullException()
    {
        // Arrange
        var runner = new OcrProcessRunner();
        var settings = new OcrSettings();
        string? outputPath = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => runner.RunAsync(settings, "input.pdf", outputPath!, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithEmptyInputPath_ThrowsArgumentException()
    {
        // Arrange
        var runner = new OcrProcessRunner();
        var settings = new OcrSettings();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => runner.RunAsync(settings, "", "output.pdf", CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithWhitespaceInputPath_ThrowsArgumentException()
    {
        // Arrange
        var runner = new OcrProcessRunner();
        var settings = new OcrSettings();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => runner.RunAsync(settings, "   ", "output.pdf", CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithEmptyOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var runner = new OcrProcessRunner();
        var settings = new OcrSettings();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => runner.RunAsync(settings, "input.pdf", "", CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_WithWhitespaceOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var runner = new OcrProcessRunner();
        var settings = new OcrSettings();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => runner.RunAsync(settings, "input.pdf", "   ", CancellationToken.None));
    }

    #endregion

    #region IsWindows Tests

    [Fact]
    public void IsWindows_ReturnsConsistentValue()
    {
        // Act
        var result1 = OcrProcessRunner.IsWindows();
        var result2 = OcrProcessRunner.IsWindows();

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void IsWindows_MatchesRuntimeInformation()
    {
        // Act
        var result = OcrProcessRunner.IsWindows();

        // Assert
        var expected = System.Runtime.InteropServices.RuntimeInformation
            .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        Assert.Equal(expected, result);
    }

    #endregion
}
