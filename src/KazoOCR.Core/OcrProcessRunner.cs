using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace KazoOCR.Core;

/// <summary>
/// Cross-platform wrapper for running OCRmyPDF processes.
/// On Linux/macOS, invokes ocrmypdf directly.
/// On Windows, invokes ocrmypdf via WSL with path conversion.
/// </summary>
public sealed class OcrProcessRunner : IOcrProcessRunner
{
    private const string OcrMyPdfCommand = "ocrmypdf";
    private const string WslCommand = "wsl";

    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(
        OcrSettings settings,
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(inputPath);
        ArgumentNullException.ThrowIfNull(outputPath);

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Input path cannot be empty or whitespace.", nameof(inputPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be empty or whitespace.", nameof(outputPath));
        }

        var (fileName, argumentList) = BuildProcessArgumentList(settings, inputPath, outputPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var arg in argumentList)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };

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

            // WaitForExitAsync does not guarantee that all OutputDataReceived/ErrorDataReceived
            // events have been raised. Call the synchronous WaitForExit() overload (no timeout)
            // to drain any remaining buffered output before reading the StringBuilders.
            process.WaitForExit();

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

    /// <summary>
    /// Builds the process start info based on the current operating system.
    /// </summary>
    /// <param name="settings">The OCR settings.</param>
    /// <param name="inputPath">The input file path.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <returns>A tuple containing the file name and arguments for the process.</returns>
    internal static (string FileName, string Arguments) BuildProcessStartInfo(
        OcrSettings settings,
        string inputPath,
        string outputPath)
    {
        var ocrArguments = BuildOcrArguments(settings);

        if (IsWindows())
        {
            // On Windows, use WSL to run ocrmypdf
            var wslInputPath = ConvertToWslPath(inputPath);
            var wslOutputPath = ConvertToWslPath(outputPath);

            var wslArguments = $"{OcrMyPdfCommand} {ocrArguments} \"{wslInputPath}\" \"{wslOutputPath}\"";
            return (WslCommand, wslArguments);
        }
        else
        {
            // On Linux/macOS, run ocrmypdf directly
            var arguments = $"{ocrArguments} \"{inputPath}\" \"{outputPath}\"";
            return (OcrMyPdfCommand, arguments);
        }
    }

    /// <summary>
    /// Builds the process file name and argument list using <see cref="ProcessStartInfo.ArgumentList"/>
    /// so each argument is passed as a discrete token (no shell quoting issues, Unicode-safe).
    /// On Windows, wraps ocrmypdf via WSL and optionally targets a specific distro via <c>-d</c>.
    /// </summary>
    internal static (string FileName, IReadOnlyList<string> ArgumentList) BuildProcessArgumentList(
        OcrSettings settings,
        string inputPath,
        string outputPath)
    {
        // Trigger validation via existing method
        var ocrArgs = BuildOcrArgumentList(settings);

        if (IsWindows())
        {
            var wslInputPath = ConvertToWslPath(inputPath);
            var wslOutputPath = ConvertToWslPath(outputPath);

            var args = new List<string>();

            if (!string.IsNullOrWhiteSpace(settings.WslDistro))
            {
                args.Add("-d");
                args.Add(settings.WslDistro);
            }

            args.Add("--");
            args.Add(OcrMyPdfCommand);
            args.AddRange(ocrArgs);
            args.Add(wslInputPath);
            args.Add(wslOutputPath);

            return (WslCommand, args);
        }
        else
        {
            var args = new List<string>(ocrArgs) { inputPath, outputPath };
            return (OcrMyPdfCommand, args);
        }
    }

    /// <summary>
    /// Builds the OCR argument tokens as a discrete list (for use with ArgumentList).
    /// Runs validation via <see cref="BuildOcrArguments"/> first.
    /// </summary>
    private static IReadOnlyList<string> BuildOcrArgumentList(OcrSettings settings)
    {
        // Invoke to trigger range / language validation
        BuildOcrArguments(settings);

        var args = new List<string>();

        if (settings.Deskew) args.Add("--deskew");
        if (settings.Clean) args.Add("--clean");
        if (settings.Rotate) args.Add("--rotate-pages");

        args.Add("--optimize");
        args.Add(settings.Optimize.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (!string.IsNullOrWhiteSpace(settings.Languages))
        {
            args.Add("-l");
            args.Add(settings.Languages);
        }

        return args;
    }



    /// <summary>
    /// Builds the OCRmyPDF command-line arguments from the settings.
    /// </summary>
    /// <param name="settings">The OCR settings.</param>
    /// <returns>The command-line arguments string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="OcrSettings.Optimize"/> is outside the valid range (0-3).</exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="OcrSettings.Languages"/> contains invalid characters.</exception>
    internal static string BuildOcrArguments(OcrSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Optimize < 0 || settings.Optimize > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                settings.Optimize,
                "Optimize level must be between 0 and 3.");
        }

        // Validate Languages to prevent command injection
        // Languages should only contain letters, numbers, underscores, and plus signs (e.g., "fra+eng")
        if (!string.IsNullOrWhiteSpace(settings.Languages) &&
            !IsValidLanguageCode(settings.Languages))
        {
            throw new ArgumentException(
                "Languages must contain only letters, numbers, underscores, and plus signs.",
                nameof(settings));
        }

        var args = new List<string>();

        if (settings.Deskew)
        {
            args.Add("--deskew");
        }

        if (settings.Clean)
        {
            args.Add("--clean");
        }

        if (settings.Rotate)
        {
            args.Add("--rotate-pages");
        }

        args.Add($"--optimize {settings.Optimize}");

        if (!string.IsNullOrWhiteSpace(settings.Languages))
        {
            args.Add($"-l {settings.Languages}");
        }

        return string.Join(" ", args);
    }

    /// <summary>
    /// Validates that a language code string contains only safe characters.
    /// Valid characters: a-z, A-Z, 0-9, underscore, plus sign.
    /// </summary>
    /// <param name="languages">The language code string to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    internal static bool IsValidLanguageCode(string languages)
    {
        return languages.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '+');
    }

    /// <summary>
    /// Converts a Windows path to a WSL path.
    /// For example: "C:\Users\Test\file.pdf" becomes "/mnt/c/Users/Test/file.pdf".
    /// Only absolute drive-letter paths are converted to <c>/mnt/&lt;drive&gt;/...</c>.
    /// Relative paths are normalized by replacing backslashes with forward slashes.
    /// Unsupported Windows path formats such as UNC paths, rooted paths without a drive letter,
    /// and drive-relative paths cause a <see cref="NotSupportedException"/> to be thrown.
    /// </summary>
    /// <param name="windowsPath">The Windows path to convert.</param>
    /// <returns>The equivalent WSL path, or empty string if the input is null or empty.</returns>
    internal static string ConvertToWslPath(string windowsPath)
    {
        if (string.IsNullOrEmpty(windowsPath))
        {
            return string.Empty;
        }

        if (windowsPath.StartsWith(@"\\", StringComparison.Ordinal) ||
            windowsPath.StartsWith("//", StringComparison.Ordinal))
        {
            throw new NotSupportedException("UNC paths are not supported for WSL path conversion.");
        }

        if (windowsPath.Length >= 2 && char.IsLetter(windowsPath[0]) && windowsPath[1] == ':')
        {
            if (windowsPath.Length == 2)
            {
                throw new NotSupportedException("Drive-relative Windows paths are not supported for WSL path conversion.");
            }

            if (windowsPath[2] != '\\' && windowsPath[2] != '/')
            {
                throw new NotSupportedException("Drive-relative Windows paths are not supported for WSL path conversion.");
            }

            var driveLetter = char.ToLowerInvariant(windowsPath[0]);
            var remainingPath = windowsPath[2..].Replace('\\', '/');

            return $"/mnt/{driveLetter}{remainingPath}";
        }

        if (windowsPath[0] == '\\' || windowsPath[0] == '/')
        {
            throw new NotSupportedException("Rooted Windows paths without a drive letter are not supported for WSL path conversion.");
        }
        return windowsPath.Replace('\\', '/');
    }

    /// <summary>
    /// Determines whether the current operating system is Windows.
    /// </summary>
    /// <returns><c>true</c> if running on Windows; otherwise, <c>false</c>.</returns>
    internal static bool IsWindows() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
