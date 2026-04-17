namespace KazoOCR.Core;

/// <summary>
/// Represents the result of executing an external process.
/// </summary>
public sealed class ProcessResult
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// Gets the standard output from the process.
    /// </summary>
    public string StandardOutput { get; }

    /// <summary>
    /// Gets the standard error output from the process.
    /// </summary>
    public string StandardError { get; }

    /// <summary>
    /// Gets a value indicating whether the process completed successfully (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessResult"/> class.
    /// </summary>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="standardOutput">The standard output from the process.</param>
    /// <param name="standardError">The standard error output from the process.</param>
    public ProcessResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput ?? string.Empty;
        StandardError = standardError ?? string.Empty;
    }

    /// <summary>
    /// Creates a successful process result.
    /// </summary>
    /// <param name="standardOutput">The standard output from the process.</param>
    /// <returns>A successful <see cref="ProcessResult"/>.</returns>
    public static ProcessResult Success(string standardOutput = "") =>
        new(0, standardOutput, string.Empty);

    /// <summary>
    /// Creates a failed process result.
    /// </summary>
    /// <param name="exitCode">The exit code of the process.</param>
    /// <param name="standardError">The standard error output from the process.</param>
    /// <param name="standardOutput">The standard output from the process.</param>
    /// <returns>A failed <see cref="ProcessResult"/>.</returns>
    public static ProcessResult Failure(int exitCode, string standardError, string standardOutput = "") =>
        new(exitCode, standardOutput, standardError);
}
