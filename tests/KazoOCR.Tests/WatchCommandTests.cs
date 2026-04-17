namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.CLI;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;
using Moq;

public class WatchCommandTests
{
    private readonly Mock<IWatcherService> _watcherServiceMock;
    private readonly Mock<ILogger<WatchCommand>> _loggerMock;
    private readonly WatchCommand _command;

    public WatchCommandTests()
    {
        _watcherServiceMock = new Mock<IWatcherService>();
        _loggerMock = new Mock<ILogger<WatchCommand>>();
        _command = new WatchCommand(_watcherServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullWatcherService_ThrowsArgumentNullException()
    {
        var action = () => new WatchCommand(null!, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("watcherService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new WatchCommand(_watcherServiceMock.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task Execute_WithInvalidInput_ReturnsInvalidArguments()
    {
        var result = await _command.Execute(input: string.Empty);

        result.Should().Be((int)ExitCodes.InvalidArguments);
        _watcherServiceMock.Verify(
            x => x.WatchAsync(It.IsAny<string>(), It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WithMissingDirectory_ReturnsFileNotFound()
    {
        var result = await _command.Execute(input: "/tmp/this-directory-should-not-exist");

        result.Should().Be((int)ExitCodes.FileNotFound);
        _watcherServiceMock.Verify(
            x => x.WatchAsync(It.IsAny<string>(), It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Execute_WithOutOfRangeOptimize_ReturnsInvalidArguments()
    {
        var tempDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = await _command.Execute(input: tempDir, optimize: 5);

            result.Should().Be((int)ExitCodes.InvalidArguments);
            _watcherServiceMock.Verify(
                x => x.WatchAsync(It.IsAny<string>(), It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Execute_WithValidInput_CallsWatcherWithExpectedSettings()
    {
        var tempDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        OcrSettings? capturedSettings = null;

        try
        {
            _watcherServiceMock
                .Setup(x => x.WatchAsync(tempDir, It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()))
                .Callback<string, OcrSettings, CancellationToken>((_, settings, _) => capturedSettings = settings)
                .Returns(Task.CompletedTask);

            var result = await _command.Execute(
                input: tempDir,
                suffix: "_PROCESSED",
                languages: "fra+deu",
                deskew: true,
                clean: true,
                rotate: true,
                optimize: 2);

            result.Should().Be((int)ExitCodes.Success);
            capturedSettings.Should().NotBeNull();
            capturedSettings!.Suffix.Should().Be("_PROCESSED");
            capturedSettings.Languages.Should().Be("fra+deu");
            capturedSettings.Deskew.Should().BeTrue();
            capturedSettings.Clean.Should().BeTrue();
            capturedSettings.Rotate.Should().BeTrue();
            capturedSettings.Optimize.Should().Be(2);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Execute_WhenCanceled_ReturnsSuccess()
    {
        var tempDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            _watcherServiceMock
                .Setup(x => x.WatchAsync(tempDir, It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            var result = await _command.Execute(input: tempDir);

            result.Should().Be((int)ExitCodes.Success);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
