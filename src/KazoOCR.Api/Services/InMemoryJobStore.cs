using System.Collections.Concurrent;
using KazoOCR.Api.Models;

namespace KazoOCR.Api.Services;

/// <summary>
/// In-memory implementation of the job store.
/// </summary>
public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<string, OcrJobResult> _jobs = new();

    /// <inheritdoc />
    public OcrJobResult CreateJob(string inputFileName)
    {
        var id = Guid.NewGuid().ToString("N");
        var job = new OcrJobResult(
            Id: id,
            InputFileName: inputFileName,
            Status: JobStatus.Pending,
            OutputPath: null,
            ErrorMessage: null,
            CreatedAt: DateTimeOffset.UtcNow,
            CompletedAt: null);

        _jobs[id] = job;
        return job;
    }

    /// <inheritdoc />
    public OcrJobResult? UpdateJob(string id, JobStatus status, string? outputPath = null, string? errorMessage = null)
    {
        if (!_jobs.TryGetValue(id, out var existing))
        {
            return null;
        }

        var completedAt = status is JobStatus.Completed or JobStatus.Failed
            ? DateTimeOffset.UtcNow
            : existing.CompletedAt;

        var updated = existing with
        {
            Status = status,
            OutputPath = outputPath ?? existing.OutputPath,
            ErrorMessage = errorMessage ?? existing.ErrorMessage,
            CompletedAt = completedAt
        };

        _jobs[id] = updated;
        return updated;
    }

    /// <inheritdoc />
    public OcrJobResult? GetJob(string id)
    {
        return _jobs.TryGetValue(id, out var job) ? job : null;
    }

    /// <inheritdoc />
    public IEnumerable<OcrJobResult> GetAllJobs()
    {
        return _jobs.Values.OrderByDescending(j => j.CreatedAt);
    }

    /// <inheritdoc />
    public bool RemoveJob(string id)
    {
        return _jobs.TryRemove(id, out _);
    }
}
