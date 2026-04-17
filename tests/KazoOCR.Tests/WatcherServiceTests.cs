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
            .Setup(x => x.IsAlreadyProcessed(It.IsAny<string>(), "_OCR"))
            .Returns(false);
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

        try
        {
            var watchTask = service.WatchAsync(tempDir, settings, cts.Token);
            await Task.Delay(200, CancellationToken.None);

            await File.WriteAllTextAsync(targetPdf, "pdf");
            await File.WriteAllTextAsync(ignoredSuffixPdf, "pdf");
            await File.WriteAllTextAsync(ignoredText, "txt");

            var processed = await processedSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));
            processed.Should().BeTrue();

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
}
