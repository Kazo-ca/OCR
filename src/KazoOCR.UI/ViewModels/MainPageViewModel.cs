using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KazoOCR.Core;

namespace KazoOCR.UI.ViewModels;

/// <summary>
/// ViewModel for the main page of the KazoOCR MAUI application.
/// </summary>
public sealed class MainPageViewModel : INotifyPropertyChanged
{
    private readonly IOcrFileService _fileService;
    private readonly IOcrProcessRunner _processRunner;

    private string _suffix = "_OCR";
    private string _languages = "fra+eng";
    private bool _deskew = true;
    private bool _clean;
    private bool _rotate = true;
    private int _optimize = 1;
    private double _progress;
    private string _statusMessage = "Ready. Drag & drop PDF files or select files to process.";
    private bool _isProcessing;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
    /// </summary>
    /// <param name="fileService">The OCR file service.</param>
    /// <param name="processRunner">The OCR process runner.</param>
    public MainPageViewModel(IOcrFileService fileService, IOcrProcessRunner processRunner)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        LogMessages = [];
        PendingFiles = [];
    }

    /// <summary>
    /// Gets or sets the suffix to append to processed files.
    /// </summary>
    public string Suffix
    {
        get => _suffix;
        set => SetProperty(ref _suffix, value);
    }

    /// <summary>
    /// Gets or sets the OCR languages.
    /// </summary>
    public string Languages
    {
        get => _languages;
        set => SetProperty(ref _languages, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to apply deskewing.
    /// </summary>
    public bool Deskew
    {
        get => _deskew;
        set => SetProperty(ref _deskew, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to apply cleaning.
    /// </summary>
    public bool Clean
    {
        get => _clean;
        set => SetProperty(ref _clean, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to apply rotation correction.
    /// </summary>
    public bool Rotate
    {
        get => _rotate;
        set => SetProperty(ref _rotate, value);
    }

    /// <summary>
    /// Gets or sets the optimization level (0-3).
    /// </summary>
    public int Optimize
    {
        get => _optimize;
        set => SetProperty(ref _optimize, Math.Clamp(value, 0, 3));
    }

    /// <summary>
    /// Gets or sets the current progress (0-100).
    /// </summary>
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    /// <summary>
    /// Gets or sets the status message.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether processing is in progress.
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                OnPropertyChanged(nameof(IsNotProcessing));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether processing is not in progress.
    /// </summary>
    public bool IsNotProcessing => !IsProcessing;

    /// <summary>
    /// Gets the collection of log messages.
    /// </summary>
    public ObservableCollection<string> LogMessages { get; }

    /// <summary>
    /// Gets the collection of pending files to process.
    /// </summary>
    public ObservableCollection<string> PendingFiles { get; }

    /// <summary>
    /// Adds files to the pending files list.
    /// </summary>
    /// <param name="filePaths">The file paths to add.</param>
    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (!string.IsNullOrWhiteSpace(filePath) &&
                filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                !PendingFiles.Contains(filePath))
            {
                PendingFiles.Add(filePath);
                AddLog($"Added: {Path.GetFileName(filePath)}");
            }
        }

        UpdateStatusMessage();
    }

    /// <summary>
    /// Clears all pending files.
    /// </summary>
    public void ClearFiles()
    {
        PendingFiles.Clear();
        AddLog("Cleared all pending files.");
        UpdateStatusMessage();
    }

    /// <summary>
    /// Processes all pending files.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessFilesAsync()
    {
        if (PendingFiles.Count == 0)
        {
            AddLog("No files to process.");
            return;
        }

        IsProcessing = true;
        Progress = 0;
        using var cts = new CancellationTokenSource();
        _cancellationTokenSource = cts;
        var token = cts.Token;

        var files = PendingFiles.ToList();
        var total = files.Count;
        var processed = 0;
        var successful = 0;
        var failed = 0;

        AddLog($"Starting OCR processing for {total} file(s)...");

        try
        {
            foreach (var filePath in files)
            {
                if (token.IsCancellationRequested)
                {
                    AddLog("Processing cancelled by user.");
                    break;
                }

                StatusMessage = $"Processing: {Path.GetFileName(filePath)}";

                var result = await ProcessSingleFileAsync(filePath, token);
                processed++;

                if (result)
                {
                    successful++;
                }
                else
                {
                    failed++;
                }

                Progress = (double)processed / total * 100;
            }

            AddLog($"Processing complete: {successful} succeeded, {failed} failed.");
            StatusMessage = $"Complete: {successful} succeeded, {failed} failed.";
        }
        catch (OperationCanceledException)
        {
            AddLog("Processing was cancelled.");
            StatusMessage = "Processing cancelled.";
        }
        catch (IOException ex)
        {
            AddLog($"I/O error: {ex.Message}");
            StatusMessage = "Processing failed with I/O error.";
        }
        catch (UnauthorizedAccessException ex)
        {
            AddLog($"Access denied: {ex.Message}");
            StatusMessage = "Processing failed: access denied.";
        }
        catch (InvalidOperationException ex)
        {
            AddLog($"Error: {ex.Message}");
            StatusMessage = "Processing failed with error.";
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Cancels the current processing operation.
    /// </summary>
    public void CancelProcessing()
    {
        if (_cancellationTokenSource is not null && !_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Cancel();
            AddLog("Cancellation requested...");
        }
    }

    /// <summary>
    /// Clears the log messages.
    /// </summary>
    public void ClearLog()
    {
        LogMessages.Clear();
    }

    private async Task<bool> ProcessSingleFileAsync(string filePath, CancellationToken cancellationToken)
    {
        AddLog($"Processing: {Path.GetFileName(filePath)}");

        // Validate input file
        var validation = _fileService.ValidateInput(filePath);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                AddLog($"  Validation error: {error}");
            }
            return false;
        }

        // Check if already processed
        if (_fileService.IsAlreadyProcessed(filePath, Suffix))
        {
            AddLog($"  Skipped (already processed): {Path.GetFileName(filePath)}");
            return true;
        }

        // Create settings
        var settings = new OcrSettings
        {
            Suffix = Suffix,
            Languages = Languages,
            Deskew = Deskew,
            Clean = Clean,
            Rotate = Rotate,
            Optimize = Optimize
        };

        // Compute output path
        var outputPath = _fileService.ComputeOutputPath(filePath, Suffix);

        try
        {
            var result = await _processRunner.RunAsync(settings, filePath, outputPath, cancellationToken);

            if (result.IsSuccess)
            {
                AddLog($"  Success: {Path.GetFileName(outputPath)}");
                return true;
            }

            AddLog($"  Failed: {result.StandardError}");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            AddLog($"  I/O error: {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            AddLog($"  Access denied: {ex.Message}");
            return false;
        }
        catch (InvalidOperationException ex)
        {
            AddLog($"  Error: {ex.Message}");
            return false;
        }
    }

    private void AddLog(string message)
    {
        // Using local time for user-facing log display
        var timestamp = DateTimeOffset.Now.ToString("HH:mm:ss");
        LogMessages.Add($"[{timestamp}] {message}");
    }

    private void UpdateStatusMessage()
    {
        StatusMessage = PendingFiles.Count == 0
            ? "Ready. Drag & drop PDF files or select files to process."
            : $"{PendingFiles.Count} file(s) ready to process.";
    }

    #region INotifyPropertyChanged

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
