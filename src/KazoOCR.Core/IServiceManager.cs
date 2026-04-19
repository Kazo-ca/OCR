namespace KazoOCR.Core;

/// <summary>
/// Service status information.
/// </summary>
public sealed class ServiceStatus
{
    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the service is installed.
    /// </summary>
    public bool IsInstalled { get; init; }

    /// <summary>
    /// Gets the current state of the service.
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// Gets the startup type of the service.
    /// </summary>
    public string StartType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the service display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Interface for Windows Service management operations.
/// Provides methods to install, uninstall, and query service status using sc.exe.
/// </summary>
public interface IServiceManager
{
    /// <summary>
    /// Gets the name of the Windows Service.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Installs the Windows Service.
    /// </summary>
    /// <param name="configPath">The path to the service configuration file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ProcessResult"/> indicating the result of the operation.</returns>
    Task<ProcessResult> InstallAsync(string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls the Windows Service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ProcessResult"/> indicating the result of the operation.</returns>
    Task<ProcessResult> UninstallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the Windows Service.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="ServiceStatus"/> of the service.</returns>
    Task<ServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current process is running with administrator privileges.
    /// </summary>
    /// <returns><c>true</c> if running as administrator; otherwise, <c>false</c>.</returns>
    bool IsAdministrator();
}
