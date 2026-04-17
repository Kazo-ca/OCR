using System.Diagnostics;
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

        var (fileName, arguments) = BuildProcessStartInfo(settings, inputPath, outputPath);

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
    /// Adds the OCRmyPDF arguments from the settings as discrete command-line arguments.
    /// Each flag and value is appended separately so that callers can use
    /// <see cref="ProcessStartInfo.ArgumentList"/> safely without manual escaping.
    /// </summary>
    /// <param name="arguments">The argument collection to populate.</param>
    /// <param name="settings">The OCR settings.</param>
    internal static void AddOcrArguments(ICollection<string> arguments, OcrSettings settings)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Deskew)
        {
            arguments.Add("--deskew");
        }

        if (settings.Clean)
        {
            arguments.Add("--clean");
        }

        if (settings.Rotate)
        {
            arguments.Add("--rotate-pages");
        }

        arguments.Add("--optimize");
        arguments.Add(settings.Optimize.ToString());

        if (!string.IsNullOrWhiteSpace(settings.Languages))
        {
            arguments.Add("-l");
            arguments.Add(settings.Languages);
        }
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
