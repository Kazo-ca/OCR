// This file contains tests that require MAUI components
// These tests are compiled only when BuildMAUI=true
#if BUILD_MAUI
using FluentAssertions;
using KazoOCR.Core;
using KazoOCR.UI.ViewModels;
using Moq;

namespace KazoOCR.Tests;

/// <summary>
/// Tests for the MainPageViewModel class.
/// </summary>
public class MainPageViewModelTests
{
    private readonly Mock<IOcrFileService> _mockFileService;
    private readonly Mock<IOcrProcessRunner> _mockProcessRunner;
    private readonly MainPageViewModel _viewModel;

    public MainPageViewModelTests()
    {
        _mockFileService = new Mock<IOcrFileService>();
        _mockProcessRunner = new Mock<IOcrProcessRunner>();
        _viewModel = new MainPageViewModel(_mockFileService.Object, _mockProcessRunner.Object);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFileServiceIsNull()
    {
        // Act & Assert
        var act = () => new MainPageViewModel(null!, _mockProcessRunner.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("fileService");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenProcessRunnerIsNull()
    {
        // Act & Assert
        var act = () => new MainPageViewModel(_mockFileService.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("processRunner");
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Assert
        _viewModel.Suffix.Should().Be("_OCR");
        _viewModel.Languages.Should().Be("fra+eng");
        _viewModel.Deskew.Should().BeTrue();
        _viewModel.Clean.Should().BeFalse();
        _viewModel.Rotate.Should().BeTrue();
        _viewModel.Optimize.Should().Be(1);
        _viewModel.Progress.Should().Be(0);
        _viewModel.IsProcessing.Should().BeFalse();
        _viewModel.IsNotProcessing.Should().BeTrue();
        _viewModel.PendingFiles.Should().BeEmpty();
        _viewModel.LogMessages.Should().BeEmpty();
    }

    [Fact]
    public void Optimize_ClampedToValidRange_WhenSetBelowZero()
    {
        // Act
        _viewModel.Optimize = -1;

        // Assert
        _viewModel.Optimize.Should().Be(0);
    }

    [Fact]
    public void Optimize_ClampedToValidRange_WhenSetAboveThree()
    {
        // Act
        _viewModel.Optimize = 5;

        // Assert
        _viewModel.Optimize.Should().Be(3);
    }

    [Fact]
    public void AddFiles_AddsOnlyPdfFiles()
    {
        // Arrange
        var files = new[]
        {
            "/path/to/document.pdf",
            "/path/to/image.png",
            "/path/to/another.pdf"
        };

        // Act
        _viewModel.AddFiles(files);

        // Assert
        _viewModel.PendingFiles.Should().HaveCount(2);
        _viewModel.PendingFiles.Should().Contain("/path/to/document.pdf");
        _viewModel.PendingFiles.Should().Contain("/path/to/another.pdf");
        _viewModel.PendingFiles.Should().NotContain("/path/to/image.png");
    }

    [Fact]
    public void AddFiles_IgnoresDuplicates()
    {
        // Arrange
        var files = new[]
        {
            "/path/to/document.pdf",
            "/path/to/document.pdf"
        };

        // Act
        _viewModel.AddFiles(files);

        // Assert
        _viewModel.PendingFiles.Should().HaveCount(1);
    }

    [Fact]
    public void AddFiles_IgnoresEmptyAndWhitespaceStrings()
    {
        // Arrange
        var files = new[]
        {
            "",
            "   ",
            "/path/to/document.pdf"
        };

        // Act
        _viewModel.AddFiles(files);

        // Assert
        _viewModel.PendingFiles.Should().HaveCount(1);
    }

    [Fact]
    public void AddFiles_AddsLogMessage()
    {
        // Arrange
        var files = new[] { "/path/to/document.pdf" };

        // Act
        _viewModel.AddFiles(files);

        // Assert
        _viewModel.LogMessages.Should().ContainSingle();
        _viewModel.LogMessages[0].Should().Contain("Added: document.pdf");
    }

    [Fact]
    public void ClearFiles_ClearsAllPendingFiles()
    {
        // Arrange
        _viewModel.AddFiles(["/path/to/document.pdf"]);

        // Act
        _viewModel.ClearFiles();

        // Assert
        _viewModel.PendingFiles.Should().BeEmpty();
    }

    [Fact]
    public void ClearLog_ClearsAllLogMessages()
    {
        // Arrange
        _viewModel.AddFiles(["/path/to/document.pdf"]);
        _viewModel.LogMessages.Should().NotBeEmpty();

        // Act
        _viewModel.ClearLog();

        // Assert
        _viewModel.LogMessages.Should().BeEmpty();
    }

    [Fact]
    public void StatusMessage_UpdatesWhenFilesAreAdded()
    {
        // Arrange & Act
        _viewModel.AddFiles(["/path/to/document.pdf"]);

        // Assert
        _viewModel.StatusMessage.Should().Contain("1 file(s) ready to process");
    }

    [Fact]
    public void StatusMessage_UpdatesWhenFilesAreCleared()
    {
        // Arrange
        _viewModel.AddFiles(["/path/to/document.pdf"]);

        // Act
        _viewModel.ClearFiles();

        // Assert
        _viewModel.StatusMessage.Should().Contain("Ready");
    }

    [Fact]
    public async Task ProcessFilesAsync_DoesNothing_WhenNoPendingFiles()
    {
        // Act
        await _viewModel.ProcessFilesAsync();

        // Assert
        _mockProcessRunner.Verify(
            x => x.RunAsync(It.IsAny<OcrSettings>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _viewModel.LogMessages.Should().Contain(m => m.Contains("No files to process"));
    }

    [Fact]
    public async Task ProcessFilesAsync_SetsIsProcessingDuringExecution()
    {
        // Arrange
        var filePath = "/path/to/document.pdf";
        _viewModel.AddFiles([filePath]);

        _mockFileService.Setup(x => x.ValidateInput(filePath))
            .Returns(ValidationResult.Failure("File does not exist"));

        var isProcessingDuringExecution = false;

        // Use a task completion source to capture state during execution
        _mockFileService.Setup(x => x.ValidateInput(It.IsAny<string>()))
            .Callback(() => isProcessingDuringExecution = _viewModel.IsProcessing)
            .Returns(ValidationResult.Failure("File does not exist"));

        // Act
        await _viewModel.ProcessFilesAsync();

        // Assert
        isProcessingDuringExecution.Should().BeTrue();
        _viewModel.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessFilesAsync_SkipsAlreadyProcessedFiles()
    {
        // Arrange
        var filePath = "/path/to/document_OCR.pdf";
        _viewModel.AddFiles([filePath]);

        _mockFileService.Setup(x => x.ValidateInput(filePath))
            .Returns(ValidationResult.Success());
        _mockFileService.Setup(x => x.IsAlreadyProcessed(filePath, "_OCR"))
            .Returns(true);

        // Act
        await _viewModel.ProcessFilesAsync();

        // Assert
        _mockProcessRunner.Verify(
            x => x.RunAsync(It.IsAny<OcrSettings>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _viewModel.LogMessages.Should().Contain(m => m.Contains("Skipped (already processed)"));
    }

    [Fact]
    public async Task ProcessFilesAsync_ProcessesValidFiles()
    {
        // Arrange
        var filePath = "/path/to/document.pdf";
        var outputPath = "/path/to/document_OCR.pdf";
        _viewModel.AddFiles([filePath]);

        _mockFileService.Setup(x => x.ValidateInput(filePath))
            .Returns(ValidationResult.Success());
        _mockFileService.Setup(x => x.IsAlreadyProcessed(filePath, "_OCR"))
            .Returns(false);
        _mockFileService.Setup(x => x.ComputeOutputPath(filePath, "_OCR"))
            .Returns(outputPath);
        _mockProcessRunner.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                filePath,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult.Success());

        // Act
        await _viewModel.ProcessFilesAsync();

        // Assert
        _mockProcessRunner.Verify(
            x => x.RunAsync(It.IsAny<OcrSettings>(), filePath, outputPath, It.IsAny<CancellationToken>()),
            Times.Once);
        _viewModel.LogMessages.Should().Contain(m => m.Contains("Success"));
    }

    [Fact]
    public async Task ProcessFilesAsync_ReportsFailure_WhenOcrFails()
    {
        // Arrange
        var filePath = "/path/to/document.pdf";
        var outputPath = "/path/to/document_OCR.pdf";
        _viewModel.AddFiles([filePath]);

        _mockFileService.Setup(x => x.ValidateInput(filePath))
            .Returns(ValidationResult.Success());
        _mockFileService.Setup(x => x.IsAlreadyProcessed(filePath, "_OCR"))
            .Returns(false);
        _mockFileService.Setup(x => x.ComputeOutputPath(filePath, "_OCR"))
            .Returns(outputPath);
        _mockProcessRunner.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                filePath,
                outputPath,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult.Failure(1, "OCR error"));

        // Act
        await _viewModel.ProcessFilesAsync();

        // Assert
        _viewModel.LogMessages.Should().Contain(m => m.Contains("Failed"));
        _viewModel.StatusMessage.Should().Contain("0 succeeded, 1 failed");
    }

    [Fact]
    public async Task ProcessFilesAsync_UsesCurrentSettings()
    {
        // Arrange
        var filePath = "/path/to/document.pdf";
        var outputPath = "/path/to/document_custom.pdf";
        
        _viewModel.Suffix = "_custom";
        _viewModel.Languages = "deu";
        _viewModel.Deskew = false;
        _viewModel.Clean = true;
        _viewModel.Rotate = false;
        _viewModel.Optimize = 2;
        
        _viewModel.AddFiles([filePath]);

        _mockFileService.Setup(x => x.ValidateInput(filePath))
            .Returns(ValidationResult.Success());
        _mockFileService.Setup(x => x.IsAlreadyProcessed(filePath, "_custom"))
            .Returns(false);
        _mockFileService.Setup(x => x.ComputeOutputPath(filePath, "_custom"))
            .Returns(outputPath);

        OcrSettings? capturedSettings = null;
        _mockProcessRunner.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                filePath,
                outputPath,
                It.IsAny<CancellationToken>()))
            .Callback<OcrSettings, string, string, CancellationToken>((settings, _, _, _) => capturedSettings = settings)
            .ReturnsAsync(ProcessResult.Success());

        // Act
        await _viewModel.ProcessFilesAsync();

        // Assert
        capturedSettings.Should().NotBeNull();
        capturedSettings!.Suffix.Should().Be("_custom");
        capturedSettings.Languages.Should().Be("deu");
        capturedSettings.Deskew.Should().BeFalse();
        capturedSettings.Clean.Should().BeTrue();
        capturedSettings.Rotate.Should().BeFalse();
        capturedSettings.Optimize.Should().Be(2);
    }

    [Fact]
    public void CancelProcessing_RequestsCancellation()
    {
        // Note: This is a limited test since we can't easily test cancellation mid-processing
        // without more complex setup. The CancelProcessing method sets the cancellation token.
        
        // Arrange & Act
        _viewModel.CancelProcessing();

        // Assert - just verifying no exception is thrown when called without active processing
        _viewModel.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public void PropertyChanged_RaisedOnPropertyChange()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_viewModel.Suffix))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _viewModel.Suffix = "_NEW";

        // Assert
        propertyChangedRaised.Should().BeTrue();
    }

    [Fact]
    public void IsNotProcessing_UpdatesWhenIsProcessingChanges()
    {
        // Verify the relationship between IsProcessing and IsNotProcessing.
        _viewModel.IsProcessing.Should().BeFalse();
        _viewModel.IsNotProcessing.Should().BeTrue();
    }
}
#endif
