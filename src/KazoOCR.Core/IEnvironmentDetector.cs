namespace KazoOCR.Core;

/// <summary>
/// Interface for detecting the availability of required environment dependencies.
/// </summary>
public interface IEnvironmentDetector
{
    /// <summary>
    /// Checks if WSL (Windows Subsystem for Linux) is available and running.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns><c>true</c> if WSL is available; otherwise, <c>false</c>.</returns>
    Task<bool> IsWslAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if OCRmyPDF is installed.
    /// On Windows, checks via WSL. On Linux/macOS, checks directly.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns><c>true</c> if OCRmyPDF is installed; otherwise, <c>false</c>.</returns>
    Task<bool> IsOcrMyPdfInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific Tesseract language pack is installed.
    /// </summary>
    /// <param name="lang">The language code (e.g., "fra", "eng", "deu").</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns><c>true</c> if the language pack is installed; otherwise, <c>false</c>.</returns>
    Task<bool> IsTesseractLangInstalledAsync(string lang, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Unpaper is installed.
    /// On Windows, checks via WSL. On Linux/macOS, checks directly.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns><c>true</c> if Unpaper is installed; otherwise, <c>false</c>.</returns>
    Task<bool> IsUnpaperInstalledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current operating system is Windows.
    /// </summary>
    /// <returns><c>true</c> if running on Windows; otherwise, <c>false</c>.</returns>
    bool IsWindows();
}
