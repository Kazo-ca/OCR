namespace KazoOCR.Core;

/// <summary>
/// Interface for installing required environment dependencies.
/// </summary>
public interface IEnvironmentInstaller
{
    /// <summary>
    /// Installs all required dependencies (ocrmypdf, tesseract-ocr-fra, unpaper).
    /// On Windows, installs via WSL apt-get. On Linux, installs via apt-get directly.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the outcome of the installation.</returns>
    Task<ProcessResult> InstallDependenciesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a specific Tesseract language pack.
    /// </summary>
    /// <param name="lang">The language code (e.g., "fra", "eng", "deu").</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="ProcessResult"/> containing the outcome of the installation.</returns>
    Task<ProcessResult> InstallTesseractLanguageAsync(string lang, CancellationToken cancellationToken = default);
}
