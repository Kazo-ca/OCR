namespace KazoOCR.Core;

/// <summary>
/// Detects installed WSL distributions and their capabilities.
/// </summary>
public interface IWslDistroDetector
{
    /// <summary>
    /// Returns the list of installed WSL distribution names.
    /// Excludes internal Docker Desktop distributions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<string>> ListDistrosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the name of the current WSL default distribution, or <c>null</c> if none.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> GetDefaultDistroAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the list of installed WSL distributions that have <c>ocrmypdf</c> available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<string>> ListDistrosWithOcrMyPdfAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether <c>ocrmypdf</c> is available in the specified WSL distribution.
    /// </summary>
    /// <param name="distro">WSL distribution name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasOcrMyPdfAsync(string distro, CancellationToken cancellationToken = default);
}
