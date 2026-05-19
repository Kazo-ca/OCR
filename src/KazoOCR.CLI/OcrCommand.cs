using CommandDotNet;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace KazoOCR.CLI;

/// <summary>
/// Root CLI commands for KazoOCR.
/// </summary>
public class RootCommand
{
    [Subcommand]
    public OcrCommand? Ocr { get; set; }

    [Subcommand]
    public WatchCommand? Watch { get; set; }

    [Subcommand]
    public KazoOcrCommands? Environment { get; set; }

    [Subcommand]
    public ServiceCommand? Service { get; set; }
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
    private readonly IWslDistroDetector _wslDistroDetector;
    private readonly IKazoOcrConfigStore _configStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="OcrCommand"/> class.
    /// </summary>
    /// <param name="fileService">The OCR file service.</param>
    /// <param name="processRunner">The OCR process runner.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="wslDistroDetector">The WSL distro detector.</param>
    /// <param name="configStore">The KazoOCR config store.</param>
    public OcrCommand(
        IOcrFileService fileService,
        IOcrProcessRunner processRunner,
        ILogger<OcrCommand> logger,
        IWslDistroDetector wslDistroDetector,
        IKazoOcrConfigStore configStore)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wslDistroDetector = wslDistroDetector ?? throw new ArgumentNullException(nameof(wslDistroDetector));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
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
        string? wslDistro = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            wslDistro = await ResolveWslDistroAsync(cancellationToken);
            if (wslDistro is null)
            {
                _logger.LogError("No WSL distribution with ocrmypdf found. Cannot proceed.");
                return (int)ExitCodes.GeneralError;
            }
        }

        if (Directory.Exists(input))
        {
            return await ProcessDirectoryAsync(input, suffix, languages, deskew, clean, rotate, optimize, wslDistro, cancellationToken);
        }

        return await ProcessFileAsync(input, suffix, languages, deskew, clean, rotate, optimize, wslDistro, cancellationToken);
    }

    private async Task<string?> ResolveWslDistroAsync(CancellationToken cancellationToken)
    {
        var config = _configStore.Load();
        if (!string.IsNullOrWhiteSpace(config.WslDistro))
        {
            _logger.LogInformation("Using saved WSL distribution: {Distro}", config.WslDistro);
            return config.WslDistro;
        }

        var distros = await _wslDistroDetector.ListDistrosAsync(cancellationToken).ConfigureAwait(false);
        if (distros.Count == 0)
        {
            _logger.LogError("No WSL distributions found. Install Ubuntu from https://aka.ms/wslstore");
            return null;
        }

        var readyDistros = await _wslDistroDetector.ListDistrosWithOcrMyPdfAsync(cancellationToken).ConfigureAwait(false);
        if (readyDistros.Count == 0)
        {
            _logger.LogError("No WSL distribution has ocrmypdf installed.");
            _logger.LogInformation("Install with: sudo apt-get install -y ocrmypdf tesseract-ocr tesseract-ocr-fra tesseract-ocr-eng ghostscript");
            _logger.LogInformation("Available Microsoft Store: https://aka.ms/wslstore");
            return null;
        }

        if (readyDistros.Count == 1)
        {
            var only = readyDistros[0];
            _logger.LogInformation("Auto-selected WSL distribution: {Distro}", only);
            _configStore.Save(new KazoOcrConfig { WslDistro = only });
            return only;
        }

        // Prefer Ubuntu, otherwise prompt
        var preferred = readyDistros.FirstOrDefault(
            d => d.StartsWith("Ubuntu", StringComparison.OrdinalIgnoreCase));

        if (preferred is not null)
        {
            _logger.LogInformation("Auto-selected WSL distribution: {Distro} (ocrmypdf ready)", preferred);
            _configStore.Save(new KazoOcrConfig { WslDistro = preferred });
            return preferred;
        }

        // Multiple distros with ocrmypdf — prompt user
        _logger.LogInformation("Multiple WSL distributions have ocrmypdf. Select one:");
        for (var i = 0; i < readyDistros.Count; i++)
        {
            _logger.LogInformation("  [{Index}] {Distro}", i + 1, readyDistros[i]);
        }

        Console.Write("Enter number: ");
        var line = Console.ReadLine();
        if (int.TryParse(line, out var choice) && choice >= 1 && choice <= readyDistros.Count)
        {
            var selected = readyDistros[choice - 1];
            _configStore.Save(new KazoOcrConfig { WslDistro = selected });
            _logger.LogInformation("Selected: {Distro} (saved to config)", selected);
            return selected;
        }

        _logger.LogError("Invalid selection.");
        return null;
    }

    private async Task<int> ProcessDirectoryAsync(
        string directoryPath,
        string suffix,
        string languages,
        bool deskew,
        bool clean,
        bool rotate,
        int optimize,
        string? wslDistro,
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

            var result = await ProcessFileAsync(file, suffix, languages, deskew, clean, rotate, optimize, wslDistro, cancellationToken);
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
        string? wslDistro,
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
            Optimize = optimize,
            WslDistro = wslDistro
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

            if (IsOcrMyPdfNotFoundError(result.StandardError) || IsOcrMyPdfNotFoundError(result.StandardOutput))
            {
                _logger.LogError("ocrmypdf was not found in WSL distribution '{Distro}'.", wslDistro);
                LogOcrMyPdfInstallInstructions();
                return (int)ExitCodes.OcrFailed;
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

    private static bool IsOcrMyPdfNotFoundError(string text) =>
        !string.IsNullOrEmpty(text) &&
        (text.Contains("ocrmypdf: not found", StringComparison.OrdinalIgnoreCase) ||
         text.Contains("ocrmypdf: command not found", StringComparison.OrdinalIgnoreCase));

    private void LogOcrMyPdfInstallInstructions()
    {
        _logger.LogInformation("Install ocrmypdf in your WSL distribution:");
        _logger.LogInformation("  sudo apt-get install -y ocrmypdf tesseract-ocr tesseract-ocr-fra tesseract-ocr-eng ghostscript");
        _logger.LogInformation("Or install Ubuntu from the Microsoft Store: https://aka.ms/wslstore");
        _logger.LogInformation("Delete %APPDATA%\\KazoOCR\\config.json to reset the distro selection.");
    }
}
