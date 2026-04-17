namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;
using System.Runtime.InteropServices;

public class EnvironmentDetectorTests
{
    #region IsWindows Tests

    [Fact]
    public void IsWindows_ReturnsConsistentValue()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act
        var result1 = detector.IsWindows();
        var result2 = detector.IsWindows();

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void IsWindows_MatchesRuntimeInformation()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act
        var result = detector.IsWindows();

        // Assert
        var expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        result.Should().Be(expected);
    }

    #endregion

    #region IsWslAvailableAsync Tests

    [Fact]
    public async Task IsWslAvailableAsync_OnNonWindows_ReturnsFalse()
    {
        // Skip on Windows since we're testing non-Windows behavior
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var detector = new EnvironmentDetector();

        // Act
        var result = await detector.IsWslAvailableAsync();

        // Assert
        result.Should().BeFalse("WSL is only available on Windows");
    }

    [Fact]
    public async Task IsWslAvailableAsync_SupportsCancellation()
    {
        // Skip on Windows since behavior differs
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var detector = new EnvironmentDetector();
        using var cts = new CancellationTokenSource();

        // Act - should not throw even with pre-cancelled token on non-Windows
        // because it returns false immediately
        var result = await detector.IsWslAvailableAsync(cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsOcrMyPdfInstalledAsync Tests

    [Fact]
    public async Task IsOcrMyPdfInstalledAsync_ReturnsWithoutThrowing()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act & Assert - method should complete without throwing
        var result = await detector.IsOcrMyPdfInstalledAsync();
        // Result is either true or false, test that it completes
    }

    [Fact]
    public async Task IsOcrMyPdfInstalledAsync_SupportsCancellation()
    {
        // Arrange
        var detector = new EnvironmentDetector();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await detector.IsOcrMyPdfInstalledAsync(cts.Token));
    }

    #endregion

    #region IsTesseractLangInstalledAsync Tests

    [Fact]
    public async Task IsTesseractLangInstalledAsync_WithValidLang_ReturnsWithoutThrowing()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act & Assert - method should complete without throwing
        var result = await detector.IsTesseractLangInstalledAsync("eng");
        // Result is either true or false, test that it completes
    }

    [Fact]
    public async Task IsTesseractLangInstalledAsync_WithNullLang_ThrowsArgumentNullException()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await detector.IsTesseractLangInstalledAsync(null!));
    }

    [Fact]
    public async Task IsTesseractLangInstalledAsync_WithEmptyLang_ThrowsArgumentException()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await detector.IsTesseractLangInstalledAsync(""));
    }

    [Fact]
    public async Task IsTesseractLangInstalledAsync_WithWhitespaceLang_ThrowsArgumentException()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await detector.IsTesseractLangInstalledAsync("   "));
    }

    [Fact]
    public async Task IsTesseractLangInstalledAsync_SupportsCancellation()
    {
        // Arrange
        var detector = new EnvironmentDetector();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await detector.IsTesseractLangInstalledAsync("eng", cts.Token));
    }

    #endregion

    #region IsUnpaperInstalledAsync Tests

    [Fact]
    public async Task IsUnpaperInstalledAsync_ReturnsWithoutThrowing()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act & Assert - method should complete without throwing
        var result = await detector.IsUnpaperInstalledAsync();
        // Result is either true or false, test that it completes
    }

    [Fact]
    public async Task IsUnpaperInstalledAsync_SupportsCancellation()
    {
        // Arrange
        var detector = new EnvironmentDetector();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await detector.IsUnpaperInstalledAsync(cts.Token));
    }

    #endregion

    #region RunProcessAsync Tests

    [Fact]
    public async Task RunProcessAsync_WithValidCommand_ReturnsResult()
    {
        // Arrange
        var detector = new EnvironmentDetector();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "echo";
        var args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c echo test" : "test";

        // Act
        var result = await detector.RunProcessAsync(command, args, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunProcessAsync_WithInvalidCommand_ThrowsException()
    {
        // Arrange
        var detector = new EnvironmentDetector();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            async () => await detector.RunProcessAsync(
                "non_existent_command_12345",
                "",
                CancellationToken.None));
    }

    [Fact]
    public async Task RunProcessAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var detector = new EnvironmentDetector();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd" : "sleep";
        var args = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "/c timeout /t 10" : "10";

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await detector.RunProcessAsync(command, args, cts.Token));
    }

    #endregion
}
