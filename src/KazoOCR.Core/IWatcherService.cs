namespace KazoOCR.Core;

/// <summary>
/// Watches a directory and processes new PDF files with OCR.
/// </summary>
public interface IWatcherService
{
    /// <summary>
    /// Starts watching a directory and processes newly created PDF files until cancellation.
    /// </summary>
    /// <param name="inputDirectory">The directory to watch.</param>
    /// <param name="settings">OCR settings to apply.</param>
    /// <param name="cancellationToken">A cancellation token to stop watching.</param>
    /// <returns>A task that completes when the watcher stops.</returns>
    Task WatchAsync(string inputDirectory, OcrSettings settings, CancellationToken cancellationToken);
}
