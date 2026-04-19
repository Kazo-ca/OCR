namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;
using Moq;

public class WatcherServiceTests
{
    [Fact]
    public async Task WatchAsync_ProcessesOnlyNewPdfFiles_AndSkipsSuffixFiles()
    {
        var tempDir = Path.Join(Path.GetTempPath(), $"watch-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var fileServiceMock = new Mock<IOcrFileService>();
        var processRunnerMock = new Mock<IOcrProcessRunner>();
        var loggerMock = new Mock<ILogger<WatcherService>>();

        var targetPdf = Path.Join(tempDir, "document.pdf");
        var ignoredSuffixPdf = Path.Join(tempDir, "document_OCR.pdf");
        var ignoredText = Path.Join(tempDir, "note.txt");
        var outputTarget = Path.Join(tempDir, "document_OCR.pdf");
        var processedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        fileServiceMock
            .Setup(x => x.IsAlreadyProcessed(targetPdf, "_OCR"))
            .Returns(false);
        fileServiceMock
            .Setup(x => x.IsAlreadyProcessed(ignoredSuffixPdf, "_OCR"))
            .Returns(true);
        fileServiceMock
            .Setup(x => x.ValidateInput(It.IsAny<string>()))
            .Returns(ValidationResult.Success());
        fileServiceMock
            .Setup(x => x.ComputeOutputPath(targetPdf, "_OCR"))
            .Returns(outputTarget);

        processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<OcrSettings>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<OcrSettings, string, string, CancellationToken>((_, input, _, _) =>
            {
                if (input == targetPdf)
                {
                    processedSignal.TrySetResult(true);
                }
            })
            .ReturnsAsync(ProcessResult.Success());

        var service = new WatcherService(fileServiceMock.Object, processRunnerMock.Object, loggerMock.Object);
        var settings = new OcrSettings
        {
            Suffix = "_OCR",
            Languages = "fra+eng",
            Optimize = 1
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        async Task WaitForTargetProcessingAsync()
        {
            const int maxAttempts = 10;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                await File.WriteAllTextAsync(targetPdf, "pdf", cts.Token);
                try
                {
                    (await processedSignal.Task.WaitAsync(TimeSpan.FromMilliseconds(500), cts.Token)).Should().BeTrue();
                    return;
                }
                catch (TimeoutException) when (attempt < maxAttempts - 1)
                {
                    // Retry until watcher is ready to consume events.
                }
            }

            (await processedSignal.Task.WaitAsync(TimeSpan.FromSeconds(1), cts.Token)).Should().BeTrue();
        }

        async Task AssertNoAdditionalProcessingAsync(int expectedInvocationCount)
        {
            var deadline = DateTime.UtcNow.AddSeconds(1);
            while (DateTime.UtcNow < deadline)
            {
                processRunnerMock.Invocations.Count.Should().Be(expectedInvocationCount);
                await Task.Delay(50, cts.Token);
            }
        }

        try
        {
            var watchTask = service.WatchAsync(tempDir, settings, cts.Token);
            await WaitForTargetProcessingAsync();

            await File.WriteAllTextAsync(ignoredSuffixPdf, "pdf", cts.Token);
            await File.WriteAllTextAsync(ignoredText, "txt", cts.Token);
            await AssertNoAdditionalProcessingAsync(expectedInvocationCount: 1);

            cts.Cancel();
            var action = async () => await watchTask;
            await action.Should().ThrowAsync<OperationCanceledException>();

            processRunnerMock.Verify(
                x => x.RunAsync(It.IsAny<OcrSettings>(), targetPdf, outputTarget, It.IsAny<CancellationToken>()),
                Times.Once);
            processRunnerMock.Verify(
                x => x.RunAsync(It.IsAny<OcrSettings>(), ignoredSuffixPdf, It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WatchAsync_ProcessesFileContainingSuffixSubstring_WhenNotEndingWithSuffix()
    {
        var tempDir = Path.Join(Path.GetTempPath(), $"watch-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var fileServiceMock = new Mock<IOcrFileService>();
        var processRunnerMock = new Mock<IOcrProcessRunner>();
        var loggerMock = new Mock<ILogger<WatcherService>>();

        var inputPdf = Path.Join(tempDir, "invoice_OCR_source.pdf");
        var outputPdf = Path.Join(tempDir, "invoice_OCR_source_OCR.pdf");
        var processedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        fileServiceMock.Setup(x => x.IsAlreadyProcessed(inputPdf, "_OCR")).Returns(false);
        fileServiceMock.Setup(x => x.ValidateInput(inputPdf)).Returns(ValidationResult.Success());
        fileServiceMock.Setup(x => x.ComputeOutputPath(inputPdf, "_OCR")).Returns(outputPdf);

        processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<OcrSettings>(), inputPdf, outputPdf, It.IsAny<CancellationToken>()))
            .Callback(() => processedSignal.TrySetResult(true))
            .ReturnsAsync(ProcessResult.Success());

        var service = new WatcherService(fileServiceMock.Object, processRunnerMock.Object, loggerMock.Object);
        var settings = new OcrSettings { Suffix = "_OCR", Languages = "fra+eng", Optimize = 1 };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            var watchTask = service.WatchAsync(tempDir, settings, cts.Token);

            await File.WriteAllTextAsync(inputPdf, "pdf", cts.Token);
            (await processedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token)).Should().BeTrue();

            cts.Cancel();
            await FluentActions.Awaiting(() => watchTask).Should().ThrowAsync<OperationCanceledException>();

            processRunnerMock.Verify(
                x => x.RunAsync(It.IsAny<OcrSettings>(), inputPdf, outputPdf, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task WatchAsync_RetriesTransientValidationFailures_ThenProcessesFile()
    {
        var tempDir = Path.Join(Path.GetTempPath(), $"watch-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        var fileServiceMock = new Mock<IOcrFileService>();
        var processRunnerMock = new Mock<IOcrProcessRunner>();
        var loggerMock = new Mock<ILogger<WatcherService>>();

        var inputPdf = Path.Join(tempDir, "document.pdf");
        var outputPdf = Path.Join(tempDir, "document_OCR.pdf");
        var processedSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        fileServiceMock.Setup(x => x.IsAlreadyProcessed(inputPdf, "_OCR")).Returns(false);
        fileServiceMock
            .SetupSequence(x => x.ValidateInput(inputPdf))
            .Returns(ValidationResult.Failure("Cannot access file: The process cannot access the file because it is being used by another process."))
            .Returns(ValidationResult.Success());
        fileServiceMock.Setup(x => x.ComputeOutputPath(inputPdf, "_OCR")).Returns(outputPdf);

        processRunnerMock
            .Setup(x => x.RunAsync(It.IsAny<OcrSettings>(), inputPdf, outputPdf, It.IsAny<CancellationToken>()))
            .Callback(() => processedSignal.TrySetResult(true))
            .ReturnsAsync(ProcessResult.Success());

        var service = new WatcherService(fileServiceMock.Object, processRunnerMock.Object, loggerMock.Object);
        var settings = new OcrSettings { Suffix = "_OCR", Languages = "fra+eng", Optimize = 1 };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            var watchTask = service.WatchAsync(tempDir, settings, cts.Token);

            await File.WriteAllTextAsync(inputPdf, "pdf", cts.Token);
            (await processedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5), cts.Token)).Should().BeTrue();

            cts.Cancel();
            await FluentActions.Awaiting(() => watchTask).Should().ThrowAsync<OperationCanceledException>();

            fileServiceMock.Verify(x => x.ValidateInput(inputPdf), Times.Exactly(2));
            processRunnerMock.Verify(
                x => x.RunAsync(It.IsAny<OcrSettings>(), inputPdf, outputPdf, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
