using CommandDotNet;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;

namespace KazoOCR.CLI;

/// <summary>
/// CLI commands for Windows Service management.
/// </summary>
[Command("service", Description = "Manage the KazoOCR Windows Service.")]
public sealed class ServiceCommand
{
    private readonly IServiceManager _serviceManager;
    private readonly IPrivilegeElevator _privilegeElevator;
    private readonly ILogger<ServiceCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceCommand"/> class.
    /// </summary>
    /// <param name="serviceManager">The service manager.</param>
    /// <param name="privilegeElevator">The privilege elevator.</param>
    /// <param name="logger">The logger instance.</param>
    public ServiceCommand(
        IServiceManager serviceManager,
        IPrivilegeElevator privilegeElevator,
        ILogger<ServiceCommand> logger)
    {
        _serviceManager = serviceManager ?? throw new ArgumentNullException(nameof(serviceManager));
        _privilegeElevator = privilegeElevator ?? throw new ArgumentNullException(nameof(privilegeElevator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Install the KazoOCR Windows Service.
    /// </summary>
    /// <param name="config">Path to the service configuration file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    [Command("install", Description = "Install the KazoOCR Windows Service.")]
    public async Task<int> Install(
        [Option('c', Description = "Path to the service configuration file")] string? config = null,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("ERROR: Windows Service installation is only supported on Windows.");
            return (int)ExitCodes.GeneralError;
        }

        if (!_serviceManager.IsAdministrator())
        {
            Console.WriteLine("ERROR: Administrator privileges are required to install the service.");
            Console.WriteLine("Please run this command as Administrator.");

            // Attempt elevation
            Console.WriteLine("\nAttempting to elevate privileges...");
            var args = Environment.GetCommandLineArgs()[1..];
            if (await _privilegeElevator.RelaunchElevatedAsync(args, cancellationToken))
            {
                Console.WriteLine("Elevated process started. Please check the new window.");
                return (int)ExitCodes.Success;
            }

            Console.WriteLine("Elevation failed or was cancelled.");
            return (int)ExitCodes.GeneralError;
        }

        // Determine config path
        var configPath = config ?? GetDefaultConfigPath();

        if (!File.Exists(configPath))
        {
            Console.WriteLine($"Configuration file not found: {configPath}");
            Console.WriteLine("Creating default configuration file...");
            CreateDefaultConfigFile(configPath);
            Console.WriteLine($"Default configuration created at: {configPath}");
            Console.WriteLine("Please edit this file to configure your watch folders, then run the install command again.");
            return (int)ExitCodes.InvalidArguments;
        }

        Console.WriteLine($"Installing KazoOCR Windows Service...");
        Console.WriteLine($"Configuration: {configPath}");

        var result = await _serviceManager.InstallAsync(configPath, cancellationToken);

        if (result.ExitCode == 0)
        {
            Console.WriteLine();
            Console.WriteLine("✓ " + result.StandardOutput);
            Console.WriteLine();
            Console.WriteLine("The service will now monitor the folders configured in:");
            Console.WriteLine($"  {configPath}");
            Console.WriteLine();
            Console.WriteLine("Manage the service using:");
            Console.WriteLine("  sc query KazoOCR    - Check status");
            Console.WriteLine("  sc stop KazoOCR     - Stop service");
            Console.WriteLine("  sc start KazoOCR    - Start service");
            return (int)ExitCodes.Success;
        }

        Console.WriteLine($"✗ Service installation failed: {result.StandardError}");
        return (int)ExitCodes.GeneralError;
    }

    /// <summary>
    /// Uninstall the KazoOCR Windows Service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    [Command("uninstall", Description = "Uninstall the KazoOCR Windows Service.")]
    public async Task<int> Uninstall(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("ERROR: Windows Service uninstallation is only supported on Windows.");
            return (int)ExitCodes.GeneralError;
        }

        if (!_serviceManager.IsAdministrator())
        {
            Console.WriteLine("ERROR: Administrator privileges are required to uninstall the service.");
            Console.WriteLine("Please run this command as Administrator.");

            // Attempt elevation
            Console.WriteLine("\nAttempting to elevate privileges...");
            var args = Environment.GetCommandLineArgs()[1..];
            if (await _privilegeElevator.RelaunchElevatedAsync(args, cancellationToken))
            {
                Console.WriteLine("Elevated process started. Please check the new window.");
                return (int)ExitCodes.Success;
            }

            Console.WriteLine("Elevation failed or was cancelled.");
            return (int)ExitCodes.GeneralError;
        }

        Console.WriteLine("Uninstalling KazoOCR Windows Service...");

        var result = await _serviceManager.UninstallAsync(cancellationToken);

        if (result.ExitCode == 0)
        {
            Console.WriteLine("✓ " + result.StandardOutput);
            return (int)ExitCodes.Success;
        }

        Console.WriteLine($"✗ Service uninstallation failed: {result.StandardError}");
        return (int)ExitCodes.GeneralError;
    }

    /// <summary>
    /// Get the status of the KazoOCR Windows Service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    [Command("status", Description = "Get the status of the KazoOCR Windows Service.")]
    public async Task<int> Status(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("Windows Service status is only applicable on Windows.");
            Console.WriteLine("On Linux/macOS, consider using Docker or running the watch command directly.");
            return (int)ExitCodes.Success;
        }

        var status = await _serviceManager.GetStatusAsync(cancellationToken);

        Console.WriteLine("=== KazoOCR Service Status ===");
        Console.WriteLine();
        Console.WriteLine($"Service Name:  {status.ServiceName}");
        Console.WriteLine($"Display Name:  {status.DisplayName}");
        Console.WriteLine($"Installed:     {(status.IsInstalled ? "Yes" : "No")}");

        if (status.IsInstalled)
        {
            Console.WriteLine($"State:         {status.State}");
            Console.WriteLine($"Start Type:    {status.StartType}");
        }

        return (int)ExitCodes.Success;
    }

    private static string GetDefaultConfigPath()
    {
        // Use the directory where the executable is located
        var exeDir = AppContext.BaseDirectory;
        return Path.Join(exeDir, "appsettings.service.json");
    }

    private static void CreateDefaultConfigFile(string path)
    {
        var defaultConfig = """
            {
              "WatchFolders": [
                {
                  "Path": "C:\\Users\\Public\\Documents\\OCR",
                  "Suffix": "_OCR",
                  "Languages": "fra+eng",
                  "Deskew": true,
                  "Clean": false,
                  "Rotate": true,
                  "Optimize": 1
                }
              ]
            }
            """;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, defaultConfig);
    }
}
