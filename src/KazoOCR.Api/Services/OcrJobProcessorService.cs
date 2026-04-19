using KazoOCR.Api.Models;
using KazoOCR.Core;
using static KazoOCR.Api.ApiConfiguration;

namespace KazoOCR.Api.Services;

/// <summary>
/// Background service that processes OCR jobs from the queue.
/// </summary>
public sealed class OcrJobProcessorService : BackgroundService
{
    private readonly OcrJobService _jobService;
    private readonly IOcrFileService _fileService;
    private readonly IOcrProcessRunner _processRunner;
    private readonly ILogger<OcrJobProcessorService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrJobProcessorService"/> class.
    /// </summary>
    public OcrJobProcessorService(
        OcrJobService jobService,
        IOcrFileService fileService,
        IOcrProcessRunner processRunner,
        ILogger<OcrJobProcessorService> logger,
        IConfiguration configuration)
    {
        _jobService = jobService;
        _fileService = fileService;
        _processRunner = processRunner;
        _logger = logger;
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OcrJobProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingJob = _jobService.GetNextPendingJob();
                if (pendingJob is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                await ProcessJobAsync(pendingJob, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in job processing loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("OcrJobProcessorService stopped");
    }

    private async Task ProcessJobAsync(OcrJobResult job, CancellationToken cancellationToken)
    {
        var inputPath = _jobService.GetJobInputPath(job.Id);
        if (inputPath is null)
        {
            _logger.LogWarning("Job {JobId} input path not found", job.Id);
            return;
        }

        if (!_jobService.StartProcessing(job.Id))
        {
            _logger.LogWarning("Job {JobId} could not be started (already processing or completed)", job.Id);
            return;
        }

        _logger.LogInformation("Processing job {JobId}", job.Id);

        try
        {
            var settings = ApiConfiguration.BuildOcrSettings(_configuration);
            var outputPath = _fileService.ComputeOutputPath(inputPath, settings.Suffix);

            var result = await _processRunner.RunAsync(settings, inputPath, outputPath, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _jobService.MarkCompleted(job.Id, outputPath);
                _logger.LogInformation("Job {JobId} completed successfully", job.Id);
            }
            else
            {
                var errorMessage = string.IsNullOrWhiteSpace(result.StandardError)
                    ? $"OCR process failed with exit code {result.ExitCode}"
                    : result.StandardError;
                _jobService.MarkFailed(job.Id, errorMessage);
                _logger.LogError("Job {JobId} failed with exit code {ExitCode}", job.Id, result.ExitCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _jobService.MarkFailed(job.Id, ex.Message);
            _logger.LogError(ex, "Job {JobId} failed with exception", job.Id);
        }
    }
}
