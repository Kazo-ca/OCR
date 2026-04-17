namespace KazoOCR.Core;

/// <summary>
/// Service for OCR file operations including path computation, validation, and processing detection.
/// </summary>
public sealed class OcrFileService : IOcrFileService
{
    private static readonly string[] ValidExtensions = [".pdf"];

    /// <inheritdoc />
    public string ComputeOutputPath(string inputPath, string suffix)
    {
        ArgumentNullException.ThrowIfNull(inputPath);
        ArgumentNullException.ThrowIfNull(suffix);

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Input path cannot be empty or whitespace.", nameof(inputPath));
        }

        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);

        var newFileName = $"{fileNameWithoutExtension}{suffix}{extension}";
        return string.IsNullOrEmpty(directory)
            ? newFileName
            : Path.Combine(directory, newFileName);
    }

    /// <inheritdoc />
    public bool IsAlreadyProcessed(string filePath, string suffix)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(suffix);

        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(suffix))
        {
            return false;
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        return fileNameWithoutExtension.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public ValidationResult ValidateInput(string path)
    {
        var result = new ValidationResult();

        // Check for null or empty path
        if (string.IsNullOrWhiteSpace(path))
        {
            result.AddError("Path cannot be null or empty.");
            return result;
        }

        // Check if file exists
        if (!File.Exists(path))
        {
            result.AddError($"File does not exist: {path}");
            return result;
        }

        // Check file extension
        var extension = Path.GetExtension(path);
        if (!ValidExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            result.AddError($"Invalid file extension '{extension}'. Only PDF files are supported.");
            return result;
        }

        // Check read permissions by attempting to open the file
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"Access denied: {path}");
        }
        catch (IOException ex)
        {
            result.AddError($"Cannot access file: {ex.Message}");
        }

        return result;
    }
}
