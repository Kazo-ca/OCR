namespace KazoOCR.CLI;

/// <summary>
/// Exit codes returned by the CLI application.
/// </summary>
public enum ExitCodes
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// A general error occurred.
    /// </summary>
    GeneralError = 1,

    /// <summary>
    /// The provided arguments are invalid.
    /// </summary>
    InvalidArguments = 2,

    /// <summary>
    /// The specified file was not found.
    /// </summary>
    FileNotFound = 3,

    /// <summary>
    /// The OCR processing failed.
    /// </summary>
    OcrFailed = 4
}
