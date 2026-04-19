namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;
using Moq;

public class WatcherServiceTests
{
    private readonly Mock<IOcrFileService> _fileServiceMock = new();
    private readonly Mock<IOcrProcessRunner> _processRunnerMock = new();
    private readonly Mock<ILogger<WatcherService>> _loggerMock = new();

    private WatcherService CreateService() =>
        new(_fileServiceMock.Object, _processRunnerMock.Object, _loggerMock.Object);

    [Fact]
    public void Constructor_WithNullFileService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new WatcherService(null!, _processRunnerMock.Object, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("fileService");
    }

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new WatcherService(_fileServiceMock.Object, null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("processRunner");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new WatcherService(_fileServiceMock.Object, _processRunnerMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task WatchAsync_WithNullWatchPath_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.WatchAsync(null!, new OcrSettings(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("watchPath");
    }

    [Fact]
    public async Task WatchAsync_WithEmptyWatchPath_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.WatchAsync("  ", new OcrSettings(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("watchPath");
    }

    [Fact]
    public async Task WatchAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.WatchAsync("/some/path", null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("settings");
    }

    [Fact]
    public async Task WatchAsync_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = () => service.WatchAsync("/nonexistent/path", new OcrSettings(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task WatchAsync_WhenCancelled_StopsGracefully()
    {
        // Arrange
        var service = CreateService();
        var tempDir = Path.Combine(Path.GetTempPath(), $"kazoocr_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            using var cts = new CancellationTokenSource();

            // Cancel after a brief delay so the watcher starts and then stops
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act & Assert — should complete without throwing
            var act = () => service.WatchAsync(tempDir, new OcrSettings(), cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WatchAsync_WhenNewPdfCreated_ProcessesFile()
    {
        // Arrange
        var service = CreateService();
        var tempDir = Path.Combine(Path.GetTempPath(), $"kazoocr_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var settings = new OcrSettings { Suffix = "_OCR" };
        var testFile = Path.Combine(tempDir, "test.pdf");
        var outputFile = Path.Combine(tempDir, "test_OCR.pdf");

        _fileServiceMock.Setup(f => f.IsAlreadyProcessed(testFile, "_OCR")).Returns(false);
        _fileServiceMock.Setup(f => f.ComputeOutputPath(testFile, "_OCR")).Returns(outputFile);
        _processRunnerMock
            .Setup(p => p.RunAsync(settings, testFile, outputFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult.Success());

        try
        {
            using var cts = new CancellationTokenSource();

            // Start the watcher in the background
            var watchTask = service.WatchAsync(tempDir, settings, cts.Token);

            // Give the watcher time to start
            await Task.Delay(200);

            // Create a PDF file
            await File.WriteAllTextAsync(testFile, "test content");

            // Give time for the file to be detected and processed
            await Task.Delay(500);

            // Stop the watcher
            cts.Cancel();

            try
            {
                await watchTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            _processRunnerMock.Verify(
                p => p.RunAsync(settings, testFile, outputFile, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WatchAsync_WhenAlreadyProcessedFile_SkipsFile()
    {
        // Arrange
        var service = CreateService();
        var tempDir = Path.Combine(Path.GetTempPath(), $"kazoocr_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var settings = new OcrSettings { Suffix = "_OCR" };
        var processedFile = Path.Combine(tempDir, "test_OCR.pdf");

        _fileServiceMock.Setup(f => f.IsAlreadyProcessed(processedFile, "_OCR")).Returns(true);

        try
        {
            using var cts = new CancellationTokenSource();

            // Start the watcher
            var watchTask = service.WatchAsync(tempDir, settings, cts.Token);

            // Give the watcher time to start
            await Task.Delay(200);

            // Create a file that should be skipped
            await File.WriteAllTextAsync(processedFile, "test content");

            // Give time for detection
            await Task.Delay(500);

            // Stop
            cts.Cancel();

            try
            {
                await watchTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert — process runner should never be called
            _processRunnerMock.Verify(
                p => p.RunAsync(It.IsAny<OcrSettings>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
