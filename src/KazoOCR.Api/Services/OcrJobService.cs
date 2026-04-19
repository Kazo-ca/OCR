using System.Collections.Concurrent;
using KazoOCR.Api.Models;

namespace KazoOCR.Api.Services;

/// <summary>
/// In-memory implementation of OCR job management.
/// </summary>
public sealed class OcrJobService : IOcrJobService
{
    private readonly ConcurrentDictionary<string, OcrJob> _jobs = new();

    /// <inheritdoc />
    public OcrJobResult CreateJob(string inputFileName, string inputPath)
    {
        var job = new OcrJob
        {
            InputFileName = inputFileName,
            InputPath = inputPath
        };

        _jobs[job.Id] = job;
        return job.ToResult();
    }

    /// <inheritdoc />
    public OcrJobResult? GetJob(string id)
    {
        return _jobs.TryGetValue(id, out var job) ? job.ToResult() : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<OcrJobResult> GetAllJobs()
    {
        return _jobs.Values
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => j.ToResult())
            .ToList();
    }

    /// <inheritdoc />
    public bool StartProcessing(string id)
    {
        if (!_jobs.TryGetValue(id, out var job))
            return false;

        // Use the job's dedicated SyncRoot for atomic state transition
        lock (job.SyncRoot)
        {
            if (job.Status != JobStatus.Pending)
                return false;

            job.Status = JobStatus.Processing;
            return true;
        }
    }

    /// <inheritdoc />
    public bool MarkCompleted(string id, string outputPath)
    {
        if (!_jobs.TryGetValue(id, out var job))
            return false;

        // Use the job's dedicated SyncRoot for atomic state transition
        lock (job.SyncRoot)
        {
            job.Status = JobStatus.Completed;
            job.OutputPath = outputPath;
            job.CompletedAt = DateTimeOffset.UtcNow;
            return true;
        }
    }

    /// <inheritdoc />
    public bool MarkFailed(string id, string errorMessage)
    {
        if (!_jobs.TryGetValue(id, out var job))
            return false;

        // Use the job's dedicated SyncRoot for atomic state transition
        lock (job.SyncRoot)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = errorMessage;
            job.CompletedAt = DateTimeOffset.UtcNow;
            return true;
        }
    }

    /// <inheritdoc />
    public bool RemoveJob(string id)
    {
        return _jobs.TryRemove(id, out _);
    }

    /// <inheritdoc />
    public OcrJobResult? GetNextPendingJob()
    {
        // Find the oldest pending job using thread-safe enumeration
        var pendingJob = _jobs.Values
            .Where(j => j.Status == JobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefault();

        return pendingJob?.ToResult();
    }

    /// <inheritdoc />
    public string? GetJobInputPath(string id)
    {
        return _jobs.TryGetValue(id, out var job) ? job.InputPath : null;
    }
}
