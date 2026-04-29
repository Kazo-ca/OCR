using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KazoOCR.Core;

/// <summary>
/// Detects the availability of required environment dependencies.
/// On Windows, checks via WSL. On Linux/macOS, checks directly.
/// </summary>
public sealed class EnvironmentDetector : IEnvironmentDetector
{
    private const string WslCommand = "wsl";
    private const string WhichCommand = "which";
    private const string TesseractCommand = "tesseract";

    /// <inheritdoc />
    public async Task<bool> IsWslAvailableAsync(CancellationToken cancellationToken = default)
    {
        // WSL is only relevant on Windows
        if (!IsWindows())
        {
            return false;
        }

        try
        {
            var result = await RunProcessAsync(WslCommand, "--status", cancellationToken).ConfigureAwait(false);
            return result.ExitCode == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // WSL is not installed or command failed
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsOcrMyPdfInstalledAsync(CancellationToken cancellationToken = default)
    {
        return await IsCommandAvailableAsync("ocrmypdf", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsTesseractLangInstalledAsync(string lang, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lang);

        if (string.IsNullOrWhiteSpace(lang))
        {
            throw new ArgumentException("Language code cannot be empty or whitespace.", nameof(lang));
        }

        try
        {
            // Tesseract lists languages with --list-langs
            var arguments = "--list-langs";
            var result = IsWindows()
                ? await RunProcessAsync(WslCommand, $"{TesseractCommand} {arguments}", cancellationToken).ConfigureAwait(false)
                : await RunProcessAsync(TesseractCommand, arguments, cancellationToken).ConfigureAwait(false);

            if (result.ExitCode != 0)
            {
                return false;
            }

            // Parse output to find the language
            // Format: each language on a new line after "List of available languages"
            var output = result.StandardOutput + result.StandardError;
            var lines = output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

            return lines.Any(line => line.Trim().Equals(lang, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUnpaperInstalledAsync(CancellationToken cancellationToken = default)
    {
        return await IsCommandAvailableAsync("unpaper", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Checks if a command is available in the system PATH.
    /// </summary>
    private async Task<bool> IsCommandAvailableAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var result = IsWindows()
                ? await RunProcessAsync(WslCommand, $"{WhichCommand} {command}", cancellationToken).ConfigureAwait(false)
                : await RunProcessAsync(WhichCommand, command, cancellationToken).ConfigureAwait(false);

            return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Runs an external process and returns the result.
    /// </summary>
    internal async Task<ProcessResult> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
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

        var stdOut = new System.Text.StringBuilder();
        var stdErr = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdOut.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                stdErr.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            // WaitForExitAsync does not guarantee that all OutputDataReceived/ErrorDataReceived
            // events have been raised. Call the synchronous WaitForExit() overload (no timeout)
            // to drain any remaining buffered output before reading the StringBuilders.
            process.WaitForExit();

            return new ProcessResult(
                process.ExitCode,
                stdOut.ToString().TrimEnd(),
                stdErr.ToString().TrimEnd());
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
