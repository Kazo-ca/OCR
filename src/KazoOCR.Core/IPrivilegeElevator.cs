namespace KazoOCR.Core;

/// <summary>
/// Interface for privilege elevation operations.
/// Provides cross-platform support for checking and requesting elevated privileges.
/// </summary>
public interface IPrivilegeElevator
{
    /// <summary>
    /// Determines whether the current process is running with elevated privileges.
    /// </summary>
    /// <returns>
    /// <c>true</c> if running as administrator (Windows) or root (Linux/macOS);
    /// otherwise, <c>false</c>.
    /// </returns>
    bool IsElevated();

    /// <summary>
    /// Attempts to relaunch the current application with elevated privileges.
    /// </summary>
    /// <param name="args">The command-line arguments to pass to the relaunched process.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that resolves to <c>true</c> if the elevated process was started successfully;
    /// <c>false</c> if the user declined or elevation is not supported on this platform.
    /// </returns>
    /// <remarks>
    /// On Windows, this uses the "runas" verb to trigger a UAC prompt.
    /// On Linux/macOS, this returns <c>false</c> and the caller should suggest using sudo.
    /// </remarks>
    Task<bool> RelaunchElevatedAsync(string[] args, CancellationToken cancellationToken = default);
}
