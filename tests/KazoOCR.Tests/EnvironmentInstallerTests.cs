namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;
using System.Runtime.InteropServices;

public class EnvironmentInstallerTests
{
    #region DefaultPackages Tests

    [Fact]
    public void DefaultPackages_ContainsOcrMyPdf()
    {
        EnvironmentInstaller.DefaultPackages.Should().Contain("ocrmypdf");
    }

    [Fact]
    public void DefaultPackages_ContainsTesseractFra()
    {
        EnvironmentInstaller.DefaultPackages.Should().Contain("tesseract-ocr-fra");
    }

    [Fact]
    public void DefaultPackages_ContainsTesseractEng()
    {
        EnvironmentInstaller.DefaultPackages.Should().Contain("tesseract-ocr-eng");
    }

    [Fact]
    public void DefaultPackages_ContainsUnpaper()
    {
        EnvironmentInstaller.DefaultPackages.Should().Contain("unpaper");
    }

    [Fact]
    public void DefaultPackages_HasCorrectCount()
    {
        EnvironmentInstaller.DefaultPackages.Should().HaveCount(4);
    }

    #endregion

    #region IsWindows Tests

    [Fact]
    public void IsWindows_ReturnsConsistentValue()
    {
        // Act
        var result1 = EnvironmentInstaller.IsWindows();
        var result2 = EnvironmentInstaller.IsWindows();

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void IsWindows_MatchesRuntimeInformation()
    {
        // Act
        var result = EnvironmentInstaller.IsWindows();

        // Assert
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        result.Should().Be(expected);
    }

    #endregion

    #region InstallTesseractLanguageAsync Validation Tests

    [Fact]
    public async Task InstallTesseractLanguageAsync_WithNullLang_ThrowsArgumentNullException()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await installer.InstallTesseractLanguageAsync(null!));
    }

    [Fact]
    public async Task InstallTesseractLanguageAsync_WithEmptyLang_ThrowsArgumentException()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await installer.InstallTesseractLanguageAsync(""));
    }

    [Fact]
    public async Task InstallTesseractLanguageAsync_WithWhitespaceLang_ThrowsArgumentException()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await installer.InstallTesseractLanguageAsync("   "));
    }

    [Fact]
    public async Task InstallTesseractLanguageAsync_WithShellInjection_ThrowsArgumentException()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act & Assert - attempt shell injection with semicolon
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await installer.InstallTesseractLanguageAsync("fra;rm -rf /"));
    }

    [Fact]
    public async Task InstallTesseractLanguageAsync_WithSpecialCharacters_ThrowsArgumentException()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act & Assert - attempt with special characters
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await installer.InstallTesseractLanguageAsync("fra&&echo"));
    }

    [Fact]
    public async Task InstallTesseractLanguageAsync_WithQuotes_ThrowsArgumentException()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act & Assert - attempt with quotes
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await installer.InstallTesseractLanguageAsync("fra\"echo"));
    }

    #endregion

    #region Integration Tests (requires sudo - skipped in CI)

    // Note: These tests are skipped by default as they require elevated privileges
    // To run them locally with sudo, remove the Skip attribute

    [Fact(Skip = "Requires elevated privileges - run manually with sudo")]
    public async Task InstallDependenciesAsync_InstallsPackages()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act
        var result = await installer.InstallDependenciesAsync();

        // Assert
        result.ExitCode.Should().Be(0);
    }

    [Fact(Skip = "Requires elevated privileges - run manually with sudo")]
    public async Task InstallTesseractLanguageAsync_InstallsLanguagePack()
    {
        // Arrange
        var installer = new EnvironmentInstaller();

        // Act
        var result = await installer.InstallTesseractLanguageAsync("deu");

        // Assert
        result.ExitCode.Should().Be(0);
    }

    #endregion
}
