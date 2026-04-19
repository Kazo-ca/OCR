using KazoOCR.UI.ViewModels;

namespace KazoOCR.UI;

/// <summary>
/// Main page for the KazoOCR MAUI application.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this page.</param>
    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        BindingContext = _viewModel;

        // Set up drag and drop event handlers
        SetupDragAndDrop();
    }

    private void SetupDragAndDrop()
    {
        var dropGesture = new DropGestureRecognizer();
        dropGesture.DragOver += OnDragOver;
        dropGesture.Drop += OnDrop;
        DropZone.GestureRecognizers.Add(dropGesture);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private async void OnDrop(object? sender, DropEventArgs e)
    {
        if (_viewModel.IsProcessing)
        {
            return;
        }

        try
        {
            // Handle file drop - MAUI provides file paths through different mechanisms
            // On Windows, files are typically provided as StorageItems
            var data = e.Data;
            if (data is not null)
            {
                // Try to get file paths from the data package
                var filePaths = await GetDroppedFilePaths(data);
                if (filePaths.Any())
                {
                    _viewModel.AddFiles(filePaths);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Drop Error", $"Failed to process dropped files: {ex.Message}", "OK");
        }
    }

    private static async Task<IEnumerable<string>> GetDroppedFilePaths(DataPackage data)
    {
        var filePaths = new List<string>();

        try
        {
            // Try to get text data which might contain file paths
            var text = await data.View.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                // Split by newlines in case multiple paths are provided
                var paths = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in paths)
                {
                    var cleanPath = path.Trim();
                    if (File.Exists(cleanPath) && cleanPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        filePaths.Add(cleanPath);
                    }
                }
            }
        }
        catch
        {
            // Text extraction failed, which is fine - continue to other methods
        }

        return filePaths;
    }

    private async void OnSelectFilesClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsProcessing)
        {
            return;
        }

        try
        {
            var options = new PickOptions
            {
                PickerTitle = "Select PDF Files",
                FileTypes = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, [".pdf"] },
                        { DevicePlatform.macOS, ["pdf"] }
                    })
            };

            var result = await FilePicker.PickMultipleAsync(options);
            if (result is not null)
            {
                var filePaths = result.Select(f => f.FullPath).Where(p => p is not null).Cast<string>();
                _viewModel.AddFiles(filePaths);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to select files: {ex.Message}", "OK");
        }
    }

    private async void OnSelectFolderClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsProcessing)
        {
            return;
        }

        try
        {
            var result = await FolderPicker.PickAsync(default);
            if (result is not null && result.IsSuccessful && result.Folder is not null)
            {
                var folderPath = result.Folder.Path;
                var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);
                _viewModel.AddFiles(pdfFiles);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to select folder: {ex.Message}", "OK");
        }
    }

    private void OnClearFilesClicked(object? sender, EventArgs e)
    {
        if (!_viewModel.IsProcessing)
        {
            _viewModel.ClearFiles();
        }
    }

    private async void OnProcessClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsProcessing)
        {
            return;
        }

        await _viewModel.ProcessFilesAsync();
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsProcessing)
        {
            _viewModel.CancelProcessing();
        }
    }

    private void OnClearLogClicked(object? sender, EventArgs e)
    {
        _viewModel.ClearLog();
    }
}

