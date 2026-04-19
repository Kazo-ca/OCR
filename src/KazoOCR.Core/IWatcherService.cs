namespace KazoOCR.Core;

/// <summary>
/// Interface for folder monitoring that watches for new PDF files and processes them via OCR.
/// </summary>
public interface IWatcherService
{
    /// <summary>
    /// Watches the specified directory for new PDF files and processes them using OCR.
    /// </summary>
    /// <param name="watchPath">The directory path to monitor for PDF files.</param>
    /// <param name="settings">The OCR settings to apply when processing files.</param>
    /// <param name="cancellationToken">A cancellation token to stop the watcher.</param>
    /// <returns>A task that completes when the watcher is stopped.</returns>
    Task WatchAsync(string watchPath, OcrSettings settings, CancellationToken cancellationToken);
}
