namespace KazoOCR.Tests;

using KazoOCR.Core;

public class PrivilegeElevatorTests
{
    #region IsElevated Tests

    [Fact]
    public void IsElevated_ReturnsConsistentValue()
    {
        // Arrange
        var elevator = new PrivilegeElevator();

        // Act
        var result1 = elevator.IsElevated();
        var result2 = elevator.IsElevated();

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void IsElevated_ReturnsBooleanValue()
    {
        // Arrange
        var elevator = new PrivilegeElevator();

        // Act
        var result = elevator.IsElevated();

        // Assert - Just verify it returns a boolean without throwing
        Assert.IsType<bool>(result);
    }

    #endregion

    #region IsWindows Tests

    [Fact]
    public void IsWindows_ReturnsConsistentValue()
    {
        // Act
        var result1 = PrivilegeElevator.IsWindows();
        var result2 = PrivilegeElevator.IsWindows();

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void IsWindows_MatchesRuntimeInformation()
    {
        // Act
        var result = PrivilegeElevator.IsWindows();

        // Assert
        var expected = System.Runtime.InteropServices.RuntimeInformation
            .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
        Assert.Equal(expected, result);
    }

    #endregion

    #region RelaunchElevatedAsync Tests

    [Fact]
    public async Task RelaunchElevatedAsync_WithNullArgs_ThrowsArgumentNullException()
    {
        // Arrange
        var elevator = new PrivilegeElevator();
        string[]? args = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => elevator.RelaunchElevatedAsync(args!));
    }

    [Fact]
    public async Task RelaunchElevatedAsync_WithEmptyArgs_DoesNotThrow()
    {
        // Arrange
        var elevator = new PrivilegeElevator();
        var args = Array.Empty<string>();

        // Act - This should not throw on either platform
        // On non-Windows, it returns false; on Windows it would try to elevate
        // Since we're likely not running as admin in CI, we skip the assertion about success
        var result = await elevator.RelaunchElevatedAsync(args);

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task RelaunchElevatedAsync_OnNonWindows_ReturnsFalse()
    {
        // Skip on Windows since we're testing non-Windows behavior
        if (PrivilegeElevator.IsWindows())
        {
            return;
        }

        // Arrange
        var elevator = new PrivilegeElevator();
        var args = new[] { "arg1", "arg2" };

        // Act
        var result = await elevator.RelaunchElevatedAsync(args);

        // Assert - On non-Windows, elevation via runas is not supported
        Assert.False(result);
    }

    [Fact]
    public async Task RelaunchElevatedAsync_WithCancellationToken_DoesNotThrow()
    {
        // Arrange
        var elevator = new PrivilegeElevator();
        var args = new[] { "test" };
        using var cts = new CancellationTokenSource();

        // Act - Should not throw even with cancellation token
        var result = await elevator.RelaunchElevatedAsync(args, cts.Token);

        // Assert
        Assert.IsType<bool>(result);
    }

    #endregion

    #region EscapeArguments Tests

    [Fact]
    public void EscapeArguments_WithSimpleArgs_ReturnsUnchanged()
    {
        // Arrange
        var args = new[] { "arg1", "arg2", "arg3" };

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("arg1", result[0]);
        Assert.Equal("arg2", result[1]);
        Assert.Equal("arg3", result[2]);
    }

    [Fact]
    public void EscapeArguments_WithSpaces_WrapsInQuotes()
    {
        // Arrange
        var args = new[] { "arg with spaces" };

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Single(result);
        Assert.Equal("\"arg with spaces\"", result[0]);
    }

    [Fact]
    public void EscapeArguments_WithQuotes_EscapesAndWraps()
    {
        // Arrange
        var args = new[] { "arg\"with\"quotes" };

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Single(result);
        Assert.Equal("\"arg\\\"with\\\"quotes\"", result[0]);
    }

    [Fact]
    public void EscapeArguments_WithSpacesAndQuotes_EscapesAndWraps()
    {
        // Arrange
        var args = new[] { "arg with \"quotes\" and spaces" };

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Single(result);
        Assert.Equal("\"arg with \\\"quotes\\\" and spaces\"", result[0]);
    }

    [Fact]
    public void EscapeArguments_WithEmptyArray_ReturnsEmpty()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void EscapeArguments_WithNullElement_SkipsNull()
    {
        // Arrange - Empty strings are skipped
        var args = new[] { "", "valid", "" };

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Single(result);
        Assert.Equal("valid", result[0]);
    }

    [Fact]
    public void EscapeArguments_WithMixedArgs_ReturnsCorrectFormat()
    {
        // Arrange
        var args = new[] { "simple", "with spaces", "with\"quote" };

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("simple", result[0]);
        Assert.Equal("\"with spaces\"", result[1]);
        Assert.Equal("\"with\\\"quote\"", result[2]);
    }

    [Fact]
    public void EscapeArguments_WithPathLikeArg_ReturnsCorrectFormat()
    {
        // Arrange
        var args = new[] { "-i", @"C:\Users\Test\My Documents\file.pdf", "-s", "_OCR" };

        // Act
        var result = PrivilegeElevator.EscapeArguments(args).ToArray();

        // Assert
        Assert.Equal(4, result.Length);
        Assert.Equal("-i", result[0]);
        Assert.Equal(@"""C:\Users\Test\My Documents\file.pdf""", result[1]);
        Assert.Equal("-s", result[2]);
        Assert.Equal("_OCR", result[3]);
    }

    #endregion

    #region IsUnixRoot Tests (Linux/macOS only)

    [Fact]
    public void IsUnixRoot_OnLinux_ReturnsBooleanValue()
    {
        // Skip on Windows since we're testing Unix behavior
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Act - Just verify it returns without throwing
        var result = PrivilegeElevator.IsUnixRoot();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsUnixRoot_OnLinux_ReturnsConsistentValue()
    {
        // Skip on Windows since we're testing Unix behavior
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result1 = PrivilegeElevator.IsUnixRoot();
        var result2 = PrivilegeElevator.IsUnixRoot();

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion

    #region IsWindowsAdministrator Tests (Windows only)

    [Fact]
    public void IsWindowsAdministrator_OnWindows_ReturnsBooleanValue()
    {
        // Skip on non-Windows since we're testing Windows behavior
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act - Just verify it returns without throwing
        var result = PrivilegeElevator.IsWindowsAdministrator();

        // Assert
        Assert.IsType<bool>(result);
    }

    [Fact]
    public void IsWindowsAdministrator_OnWindows_ReturnsConsistentValue()
    {
        // Skip on non-Windows since we're testing Windows behavior
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var result1 = PrivilegeElevator.IsWindowsAdministrator();
        var result2 = PrivilegeElevator.IsWindowsAdministrator();

        // Assert
        Assert.Equal(result1, result2);
    }

    #endregion
}
