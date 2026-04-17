using CommandDotNet;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;

namespace KazoOCR.CLI;

/// <summary>
/// Root CLI commands for KazoOCR.
/// </summary>
public class RootCommand
{
    [Subcommand]
    public OcrCommand? Ocr { get; set; }

    [Subcommand]
    public KazoOcrCommands? Environment { get; set; }
}

/// <summary>
/// CLI commands for OCR processing.
/// </summary>
[Command("ocr", Description = "Process PDF files (one-shot or batch mode).")]
public class OcrCommand
{
    private readonly IOcrFileService _fileService;
    private readonly IOcrProcessRunner _processRunner;
    private readonly ILogger<OcrCommand> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrCommand"/> class.
    /// </summary>
    /// <param name="fileService">The OCR file service.</param>
    /// <param name="processRunner">The OCR process runner.</param>
    /// <param name="logger">The logger instance.</param>
    public OcrCommand(IOcrFileService fileService, IOcrProcessRunner processRunner, ILogger<OcrCommand> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Process PDF files (one-shot or batch mode).
    /// </summary>
    /// <param name="input">Source file or folder.</param>
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
        [Option('i', Description = "Source file or folder")] string input,
        [Option('s', Description = "Suffix for output file")] string suffix = "_OCR",
        [Option('l', Description = "Tesseract language codes")] string languages = "fra+eng",
        [Option(Description = "Enable deskew correction")] bool deskew = true,
        [Option(Description = "Enable Unpaper cleaning")] bool clean = false,
        [Option(Description = "Enable orientation correction")] bool rotate = true,
        [Option(Description = "Compression level (0-3)")] int optimize = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            _logger.LogError("Input path is required.");
            return (int)ExitCodes.InvalidArguments;
        }

        // Validate optimize range early
        if (optimize < 0 || optimize > 3)
        {
            _logger.LogError("Optimize level must be between 0 and 3. Got: {Optimize}", optimize);
            return (int)ExitCodes.InvalidArguments;
        }

        // Check if input is a directory for batch processing
        if (Directory.Exists(input))
        {
            return await ProcessDirectoryAsync(input, suffix, languages, deskew, clean, rotate, optimize, cancellationToken);
        }

        // Process single file
        return await ProcessFileAsync(input, suffix, languages, deskew, clean, rotate, optimize, cancellationToken);
    }

    private async Task<int> ProcessDirectoryAsync(
        string directoryPath,
        string suffix,
        string languages,
        bool deskew,
        bool clean,
        bool rotate,
        int optimize,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing directory: {Directory}", directoryPath);

        string[] pdfFiles;
        try
        {
            pdfFiles = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.AllDirectories);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied while enumerating PDF files in directory '{Directory}': {Message}", directoryPath, ex.Message);
            return (int)ExitCodes.GeneralError;
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directory not found while enumerating PDF files in directory '{Directory}': {Message}", directoryPath, ex.Message);
            return (int)ExitCodes.GeneralError;
        }
        catch (PathTooLongException ex)
        {
            _logger.LogError(ex, "Path too long while enumerating PDF files in directory '{Directory}': {Message}", directoryPath, ex.Message);
            return (int)ExitCodes.GeneralError;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while enumerating PDF files in directory '{Directory}': {Message}", directoryPath, ex.Message);
            return (int)ExitCodes.GeneralError;
        }

        if (pdfFiles.Length == 0)
        {
            _logger.LogWarning("No PDF files found in directory: {Directory}", directoryPath);
            return (int)ExitCodes.FileNotFound;
        }

        _logger.LogInformation("Found {Count} PDF file(s) to process.", pdfFiles.Length);

        var hasErrors = false;

        foreach (var file in pdfFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Processing cancelled.");
                return (int)ExitCodes.GeneralError;
            }

            var result = await ProcessFileAsync(file, suffix, languages, deskew, clean, rotate, optimize, cancellationToken);
            if (result != (int)ExitCodes.Success)
            {
                hasErrors = true;
            }
        }

        return hasErrors ? (int)ExitCodes.OcrFailed : (int)ExitCodes.Success;
    }

    private async Task<int> ProcessFileAsync(
        string filePath,
        string suffix,
        string languages,
        bool deskew,
        bool clean,
        bool rotate,
        int optimize,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing file: {File}", filePath);

        // Validate input file
        var validation = _fileService.ValidateInput(filePath);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                _logger.LogError("{Error}", error);
            }

            // Determine appropriate exit code based on error
            if (validation.Errors.Any(e => e.Contains("does not exist", StringComparison.OrdinalIgnoreCase)))
            {
                return (int)ExitCodes.FileNotFound;
            }

            return (int)ExitCodes.InvalidArguments;
        }

        // Check if already processed (centralized check)
        if (_fileService.IsAlreadyProcessed(filePath, suffix))
        {
            _logger.LogInformation("File already processed: {File}", filePath);
            return (int)ExitCodes.Success;
        }

        // Create settings
        var settings = new OcrSettings
        {
            Suffix = suffix,
            Languages = languages,
            Deskew = deskew,
            Clean = clean,
            Rotate = rotate,
            Optimize = optimize
        };

        // Compute output path
        var outputPath = _fileService.ComputeOutputPath(filePath, suffix);

        // Run OCR with exception handling
        try
        {
            var result = await _processRunner.RunAsync(settings, filePath, outputPath, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully processed: {File} -> {Output}", filePath, outputPath);
                return (int)ExitCodes.Success;
            }

            _logger.LogError("OCR processing failed for {File}: {Error}", filePath, result.StandardError);
            return (int)ExitCodes.OcrFailed;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OCR processing was canceled for {File}", filePath);
            return (int)ExitCodes.GeneralError;
        }
    }
}
