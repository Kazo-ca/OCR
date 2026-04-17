using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace KazoOCR.Core;

/// <summary>
/// Cross-platform implementation of privilege elevation.
/// On Windows, uses WindowsIdentity to check admin status and "runas" verb for elevation.
/// On Linux/macOS, checks for root user (UID 0) but does not support automatic elevation.
/// </summary>
public sealed class PrivilegeElevator : IPrivilegeElevator
{
    /// <inheritdoc />
    public bool IsElevated()
    {
        if (OperatingSystem.IsWindows())
        {
            return IsWindowsAdministrator();
        }

        // On Linux/macOS, check if running as root (UID 0)
        return IsUnixRoot();
    }

    /// <inheritdoc />
    public Task<bool> RelaunchElevatedAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        // Honor cancellation token - check for pre-cancellation before attempting elevation
        cancellationToken.ThrowIfCancellationRequested();

        // Elevation via runas is only supported on Windows
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(TryRelaunchWithRunas(args));
    }

    /// <summary>
    /// Checks if the current process is running as Administrator on Windows.
    /// </summary>
    /// <returns><c>true</c> if running as Administrator; otherwise, <c>false</c>.</returns>
    [SupportedOSPlatform("windows")]
    internal static bool IsWindowsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Checks if the current process is running as the root user on Unix-like systems.
    /// This checks if <see cref="Environment.UserName"/> equals "root" (case-sensitive).
    /// </summary>
    /// <returns><c>true</c> if the current user is "root"; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This is a simplified check that compares the username against "root".
    /// On most Unix-like systems, this correctly identifies the superuser account.
    /// </remarks>
    [UnsupportedOSPlatform("windows")]
    internal static bool IsUnixRoot()
    {
        // On Unix-like systems, the root account name is "root".
        return string.Equals(Environment.UserName, "root", StringComparison.Ordinal);
    }

    /// <summary>
    /// Attempts to relaunch the current process with elevated privileges using the "runas" verb.
    /// </summary>
    /// <param name="args">The command-line arguments to pass to the elevated process.</param>
    /// <returns><c>true</c> if the process was started; <c>false</c> if the user declined or an error occurred.</returns>
    [SupportedOSPlatform("windows")]
    internal static bool TryRelaunchWithRunas(string[] args)
    {
        var processPath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(processPath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = string.Join(" ", EscapeArguments(args)),
                Verb = "runas",
                UseShellExecute = true
            };

            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User declined UAC prompt or other Windows error
            return false;
        }
    }

    /// <summary>
    /// Escapes command-line arguments that contain spaces or special characters.
    /// Null or empty arguments are filtered out and not included in the output.
    /// </summary>
    /// <param name="args">The arguments to escape.</param>
    /// <returns>An enumerable of escaped arguments, excluding null or empty values.</returns>
    internal static IEnumerable<string> EscapeArguments(string[] args)
    {
        foreach (var arg in args)
        {
            if (string.IsNullOrEmpty(arg))
            {
                continue;
            }

            // If the argument contains spaces or quotes, wrap it in quotes and escape existing quotes
            if (arg.Contains(' ', StringComparison.Ordinal) ||
                arg.Contains('"', StringComparison.Ordinal))
            {
                var escaped = arg.Replace("\"", "\\\"", StringComparison.Ordinal);
                yield return $"\"{escaped}\"";
            }
            else
            {
                yield return arg;
            }
        }
    }

    /// <summary>
    /// Determines whether the current operating system is Windows.
    /// </summary>
    /// <returns><c>true</c> if running on Windows; otherwise, <c>false</c>.</returns>
    internal static bool IsWindows() =>
        OperatingSystem.IsWindows();

}
