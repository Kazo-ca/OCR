using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace KazoOCR.Core;

/// <summary>
/// Windows Service manager implementation using sc.exe for service operations.
/// </summary>
public sealed partial class ServiceManager : IServiceManager
{
    /// <summary>
    /// The name of the Windows Service.
    /// </summary>
    public const string DefaultServiceName = "KazoOCR";

    /// <summary>
    /// The display name shown in Services Manager.
    /// </summary>
    public const string DefaultDisplayName = "KazoOCR PDF Processing Service";

    /// <inheritdoc />
    public string ServiceName => DefaultServiceName;

    /// <inheritdoc />
    public bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return IsWindowsAdministrator();
    }

    /// <inheritdoc />
    public async Task<ProcessResult> InstallAsync(string configPath, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return ProcessResult.Failure(1, "Windows Service installation is only supported on Windows.");
        }

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return ProcessResult.Failure(1, "Could not determine the executable path.");
        }

        // Build the binary path with --service flag and config path
        var binPath = $"\"{exePath}\" --service --config \"{configPath}\"";

        // Use sc.exe create to register the service
        var createResult = await RunScCommandAsync(
            $"create {ServiceName} binPath= \"{binPath}\" start= auto DisplayName= \"{DefaultDisplayName}\"",
            cancellationToken);

        if (createResult.ExitCode != 0)
        {
            return createResult;
        }

        // Set the service description
        await RunScCommandAsync(
            $"description {ServiceName} \"Automatic PDF OCR processing service using OCRmyPDF.\"",
            cancellationToken);

        // Start the service
        var startResult = await RunScCommandAsync($"start {ServiceName}", cancellationToken);

        // Return success if service was created, even if start failed
        var message = $"Service '{ServiceName}' installed successfully."
            + (startResult.ExitCode != 0 ? $" Note: Service start returned: {startResult.StandardError}" : " Service started.");
        return ProcessResult.Success(message);
    }

    /// <inheritdoc />
    public async Task<ProcessResult> UninstallAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return ProcessResult.Failure(1, "Windows Service uninstallation is only supported on Windows.");
        }

        // Stop the service first (ignore errors if service is not running)
        await RunScCommandAsync($"stop {ServiceName}", cancellationToken);

        // Wait a moment for the service to stop
        await Task.Delay(1000, cancellationToken);

        // Delete the service
        var deleteResult = await RunScCommandAsync($"delete {ServiceName}", cancellationToken);

        return deleteResult.ExitCode == 0
            ? ProcessResult.Success($"Service '{ServiceName}' uninstalled successfully.")
            : deleteResult;
    }

    /// <inheritdoc />
    public async Task<ServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ServiceStatus
            {
                ServiceName = ServiceName,
                IsInstalled = false,
                State = "Not applicable (not Windows)"
            };
        }

        var result = await RunScCommandAsync($"query {ServiceName}", cancellationToken);

        if (result.ExitCode != 0)
        {
            // Service doesn't exist or access denied
            return new ServiceStatus
            {
                ServiceName = ServiceName,
                IsInstalled = false,
                State = "Not installed"
            };
        }

        var state = ExtractServiceState(result.StandardOutput);
        var startType = await GetStartTypeAsync(cancellationToken);

        return new ServiceStatus
        {
            ServiceName = ServiceName,
            IsInstalled = true,
            State = state,
            StartType = startType,
            DisplayName = DefaultDisplayName
        };
    }

    private static async Task<ProcessResult> RunScCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            return new ProcessResult(process.ExitCode, output.Trim(), error.Trim());
        }
        catch (Exception ex)
        {
            return ProcessResult.Failure(-1, $"Failed to execute sc.exe: {ex.Message}");
        }
    }

    private static string ExtractServiceState(string output)
    {
        // Parse output like:
        //   STATE              : 4  RUNNING
        var match = ServiceStateRegex().Match(output);
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    private async Task<string> GetStartTypeAsync(CancellationToken cancellationToken)
    {
        var result = await RunScCommandAsync($"qc {ServiceName}", cancellationToken);
        if (result.ExitCode != 0)
        {
            return "Unknown";
        }

        // Parse output like:
        //   START_TYPE         : 2   AUTO_START
        var match = StartTypeRegex().Match(result.StandardOutput);
        return match.Success ? match.Groups[1].Value : "Unknown";
    }

    [SupportedOSPlatform("windows")]
    private static bool IsWindowsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    [GeneratedRegex(@"STATE\s+:\s+\d+\s+(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex ServiceStateRegex();

    [GeneratedRegex(@"START_TYPE\s+:\s+\d+\s+(\w+)", RegexOptions.IgnoreCase)]
    private static partial Regex StartTypeRegex();
}
