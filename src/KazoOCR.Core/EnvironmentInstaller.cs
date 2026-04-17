using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace KazoOCR.Core;

/// <summary>
/// Installs required environment dependencies via apt-get.
/// On Windows, uses WSL. On Linux, runs commands directly.
/// </summary>
public sealed class EnvironmentInstaller : IEnvironmentInstaller
{
    private const string WslCommand = "wsl";
    private const string BashCommand = "bash";
    private const string AptGetCommand = "apt-get";

    /// <summary>
    /// The default packages to install: ocrmypdf, tesseract-ocr-fra, and unpaper.
    /// </summary>
    internal static readonly string[] DefaultPackages = ["ocrmypdf", "tesseract-ocr-fra", "unpaper"];

    /// <inheritdoc />
    public async Task<ProcessResult> InstallDependenciesAsync(CancellationToken cancellationToken = default)
    {
        var packages = string.Join(" ", DefaultPackages);
        var installCommand = $"sudo {AptGetCommand} update && sudo {AptGetCommand} install -y {packages}";

        return await RunInstallCommandAsync(installCommand, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProcessResult> InstallTesseractLanguageAsync(string lang, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lang);

        if (string.IsNullOrWhiteSpace(lang))
        {
            throw new ArgumentException("Language code cannot be empty or whitespace.", nameof(lang));
        }

        var packageName = $"tesseract-ocr-{lang.ToLowerInvariant()}";
        var installCommand = $"sudo {AptGetCommand} update && sudo {AptGetCommand} install -y {packageName}";

        return await RunInstallCommandAsync(installCommand, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the current operating system is Windows.
    /// </summary>
    internal static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Runs an installation command via bash (or WSL on Windows).
    /// </summary>
    private async Task<ProcessResult> RunInstallCommandAsync(string installCommand, CancellationToken cancellationToken)
    {
        string fileName;
        string arguments;

        if (IsWindows())
        {
            // On Windows, run via WSL bash
            fileName = WslCommand;
            arguments = $"{BashCommand} -c \"{installCommand}\"";
        }
        else
        {
            // On Linux, run via bash directly
            fileName = BashCommand;
            arguments = $"-c \"{installCommand}\"";
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var stdOutBuilder = new StringBuilder();
        var stdErrBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdOutBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdErrBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return new ProcessResult(
                process.ExitCode,
                stdOutBuilder.ToString().TrimEnd(),
                stdErrBuilder.ToString().TrimEnd());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited, ignore
            }

            throw;
        }
    }
}
