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
    /// Builds the OCRmyPDF command-line arguments from the settings.
    /// </summary>
    /// <param name="settings">The OCR settings.</param>
    /// <returns>The command-line arguments string.</returns>
    internal static string BuildOcrArguments(OcrSettings settings)
    {
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
    /// Converts a Windows path to a WSL path.
    /// For example: "C:\Users\Test\file.pdf" becomes "/mnt/c/Users/Test/file.pdf"
    /// </summary>
    /// <param name="windowsPath">The Windows path to convert.</param>
    /// <returns>The equivalent WSL path, or empty string if the input is null or empty.</returns>
    internal static string ConvertToWslPath(string windowsPath)
    {
        // Handle null or empty inputs
        if (string.IsNullOrEmpty(windowsPath))
        {
            return string.Empty;
        }

        // Check if the path is an absolute Windows path with a drive letter
        if (windowsPath.Length >= 2 && char.IsLetter(windowsPath[0]) && windowsPath[1] == ':')
        {
            var driveLetter = char.ToLowerInvariant(windowsPath[0]);
            var remainingPath = windowsPath.Length > 2 ? windowsPath[2..] : string.Empty;

            // Replace backslashes with forward slashes
            remainingPath = remainingPath.Replace('\\', '/');

            return $"/mnt/{driveLetter}{remainingPath}";
        }

        // For relative paths, UNC paths, or whitespace-only strings, just replace backslashes
        return windowsPath.Replace('\\', '/');
    }

    /// <summary>
    /// Determines whether the current operating system is Windows.
    /// </summary>
    /// <returns><c>true</c> if running on Windows; otherwise, <c>false</c>.</returns>
    internal static bool IsWindows() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
}
