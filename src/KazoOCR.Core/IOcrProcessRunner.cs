namespace KazoOCR.Core;

/// <summary>
/// Interface for running OCR processes cross-platform.
/// </summary>
public interface IOcrProcessRunner
{
    /// <summary>
    /// Runs the OCR process on the specified input file.
    /// </summary>
    /// <param name="settings">The OCR settings to use.</param>
    /// <param name="inputPath">The path to the input PDF file.</param>
    /// <param name="outputPath">The path to write the output PDF file.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the outcome of the process.</returns>
    Task<ProcessResult> RunAsync(OcrSettings settings, string inputPath, string outputPath, CancellationToken cancellationToken);
}
