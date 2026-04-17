using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace KazoOCR.Core;

/// <summary>
/// Installs required environment dependencies via apt-get.
/// On Windows, uses WSL. On Linux, runs commands directly.
/// </summary>
public sealed partial class EnvironmentInstaller : IEnvironmentInstaller
{
    private const string WslCommand = "wsl";
    private const string AptGetCommand = "apt-get";

    /// <summary>
    /// The default packages to install: ocrmypdf, tesseract-ocr-fra, tesseract-ocr-eng, and unpaper.
    /// </summary>
    internal static readonly string[] DefaultPackages = ["ocrmypdf", "tesseract-ocr-fra", "tesseract-ocr-eng", "unpaper"];

    /// <summary>
    /// Regex pattern for validating language codes (letters and numbers only).
    /// </summary>
    [GeneratedRegex("^[a-z0-9]+$", RegexOptions.IgnoreCase)]
    private static partial Regex LanguageCodeRegex();

    /// <inheritdoc />
    public async Task<ProcessResult> InstallDependenciesAsync(CancellationToken cancellationToken = default)
    {
        // Run apt-get update first
        var updateResult = await RunAptGetAsync(["update"], cancellationToken).ConfigureAwait(false);
        if (updateResult.ExitCode != 0)
        {
            return updateResult;
        }

        // Then install packages
        var installArgs = new List<string> { "install", "-y" };
        installArgs.AddRange(DefaultPackages);
        return await RunAptGetAsync(installArgs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ProcessResult> InstallTesseractLanguageAsync(string lang, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lang);

        if (string.IsNullOrWhiteSpace(lang))
        {
            throw new ArgumentException("Language code cannot be empty or whitespace.", nameof(lang));
        }

        // Validate language code to prevent shell injection
        if (!LanguageCodeRegex().IsMatch(lang))
        {
            throw new ArgumentException("Language code must contain only letters and numbers.", nameof(lang));
        }

        var packageName = $"tesseract-ocr-{lang.ToLowerInvariant()}";

        // Run apt-get update first
        var updateResult = await RunAptGetAsync(["update"], cancellationToken).ConfigureAwait(false);
        if (updateResult.ExitCode != 0)
        {
            return updateResult;
        }

        // Then install the language package
        return await RunAptGetAsync(["install", "-y", packageName], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the current operating system is Windows.
    /// </summary>
    internal static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary>
    /// Runs apt-get with explicit arguments (no shell interpolation).
    /// </summary>
    private async Task<ProcessResult> RunAptGetAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        string fileName;
        string args;

        var argsList = arguments.ToList();

        if (IsWindows())
        {
            // On Windows, run via WSL with explicit arguments
            fileName = WslCommand;
            args = $"sudo {AptGetCommand} {string.Join(" ", argsList)}";
        }
        else
        {
            // On Linux, run sudo apt-get with explicit arguments
            fileName = "sudo";
            args = $"{AptGetCommand} {string.Join(" ", argsList)}";
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
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
