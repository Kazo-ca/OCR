namespace KazoOCR.Core;

/// <summary>
/// Interface for OCR file operations including path computation, validation, and processing detection.
/// </summary>
public interface IOcrFileService
{
    /// <summary>
    /// Computes the output path for a processed file by adding a suffix before the extension.
    /// </summary>
    /// <param name="inputPath">The input file path.</param>
    /// <param name="suffix">The suffix to add (e.g., "_OCR").</param>
    /// <returns>The computed output path (e.g., "document_OCR.pdf").</returns>
    string ComputeOutputPath(string inputPath, string suffix);

    /// <summary>
    /// Determines whether a file has already been processed (contains the suffix).
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="suffix">The suffix to look for.</param>
    /// <returns><c>true</c> if the file has already been processed; otherwise, <c>false</c>.</returns>
    bool IsAlreadyProcessed(string filePath, string suffix);

    /// <summary>
    /// Validates the input file path for OCR processing.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the input is valid.</returns>
    ValidationResult ValidateInput(string path);
}
