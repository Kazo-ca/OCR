using CommandDotNet;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace KazoOCR.CLI;

/// <summary>
/// CLI command for continuous OCR folder monitoring.
/// </summary>
[Command("watch", Description = "Watch a folder for new PDF files and process them automatically.")]
public sealed class WatchCommand
{
    private readonly IWatcherService _watcherService;
    private readonly ILogger<WatchCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchCommand"/> class.
    /// </summary>
    /// <param name="watcherService">The watcher service.</param>
    /// <param name="logger">The logger instance.</param>
    public WatchCommand(IWatcherService watcherService, ILogger<WatchCommand> logger)
    {
        _watcherService = watcherService ?? throw new ArgumentNullException(nameof(watcherService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Watch a folder for new PDF files and process them continuously.
    /// </summary>
    /// <param name="input">Source folder to watch.</param>
    /// <param name="suffix">Suffix for output file.</param>
    /// <param name="languages">Tesseract language codes.</param>
    /// <param name="deskew">Enable deskew correction.</param>
    /// <param name="clean">Enable Unpaper cleaning.</param>
    /// <param name="rotate">Enable orientation correction.</param>
    /// <param name="optimize">Compression level (0-3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    [DefaultCommand]
    public async Task<int> Execute(
        [Option('i', Description = "Source folder to watch")] string input,
        [Option('s', Description = "Suffix for output file")] string suffix = "_OCR",
        [Option('l', Description = "Tesseract language codes")] string languages = "fra+eng",
        [Option(Description = "Enable deskew correction")] bool deskew = false,
        [Option(Description = "Enable Unpaper cleaning")] bool clean = false,
        [Option(Description = "Enable orientation correction")] bool rotate = false,
        [Option(Description = "Compression level (0-3)")] int optimize = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            _logger.LogError("Input directory is required.");
            return (int)ExitCodes.InvalidArguments;
        }

        if (!Directory.Exists(input))
        {
            _logger.LogError("Input directory not found: {Directory}", input);
            return (int)ExitCodes.FileNotFound;
        }

        if (optimize < 0 || optimize > 3)
        {
            _logger.LogError("Optimize level must be between 0 and 3. Got: {Optimize}", optimize);
            return (int)ExitCodes.InvalidArguments;
        }

        var settings = new OcrSettings
        {
            Suffix = suffix,
            Languages = languages,
            Deskew = deskew,
            Clean = clean,
            Rotate = rotate,
            Optimize = optimize
        };

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            linkedCts.Cancel();
        };

        using PosixSignalRegistration? sigTermRegistration = !OperatingSystem.IsWindows()
            ? PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                linkedCts.Cancel();
            })
            : null;

        Console.CancelKeyPress += cancelHandler;

        try
        {
            await _watcherService.WatchAsync(input, settings, linkedCts.Token);
            return (int)ExitCodes.Success;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Watch command canceled.");
            return (int)ExitCodes.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Watch command failed.");
            return (int)ExitCodes.GeneralError;
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
