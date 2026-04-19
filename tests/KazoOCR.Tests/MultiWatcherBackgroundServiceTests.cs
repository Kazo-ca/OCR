namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.CLI;
using KazoOCR.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

public class MultiWatcherBackgroundServiceTests
{
    private readonly Mock<IWatcherService> _watcherServiceMock;
    private readonly Mock<ILogger<MultiWatcherBackgroundService>> _loggerMock;

    public MultiWatcherBackgroundServiceTests()
    {
        _watcherServiceMock = new Mock<IWatcherService>();
        _loggerMock = new Mock<ILogger<MultiWatcherBackgroundService>>();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var action = () => new MultiWatcherBackgroundService(null!, _watcherServiceMock.Object, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_WithNullWatcherService_ThrowsArgumentNullException()
    {
        var configuration = new ConfigurationBuilder().Build();
        var action = () => new MultiWatcherBackgroundService(configuration, null!, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("watcherService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var configuration = new ConfigurationBuilder().Build();
        var action = () => new MultiWatcherBackgroundService(configuration, _watcherServiceMock.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoWatchFolders_DoesNotStartAnyWatchers()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        using var service = new MultiWatcherBackgroundService(configuration, _watcherServiceMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);

        // Wait a bit for the ExecuteAsync to complete
        await Task.Delay(50, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        _watcherServiceMock.Verify(
            x => x.WatchAsync(It.IsAny<string>(), It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPath_SkipsFolder()
    {
        var config = new Dictionary<string, string?>
        {
            ["WatchFolders:0:Path"] = "",
            ["WatchFolders:0:Suffix"] = "_OCR"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        using var service = new MultiWatcherBackgroundService(configuration, _watcherServiceMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);
        await Task.Delay(50, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        _watcherServiceMock.Verify(
            x => x.WatchAsync(It.IsAny<string>(), It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentPath_SkipsFolder()
    {
        var config = new Dictionary<string, string?>
        {
            ["WatchFolders:0:Path"] = "/this/path/should/not/exist/ever",
            ["WatchFolders:0:Suffix"] = "_OCR"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        using var service = new MultiWatcherBackgroundService(configuration, _watcherServiceMock.Object, _loggerMock.Object);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        await service.StartAsync(cts.Token);
        await Task.Delay(50, CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        _watcherServiceMock.Verify(
            x => x.WatchAsync(It.IsAny<string>(), It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidFolder_StartsWatcher()
    {
        var tempDir = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new Dictionary<string, string?>
            {
                ["WatchFolders:0:Path"] = tempDir,
                ["WatchFolders:0:Suffix"] = "_test",
                ["WatchFolders:0:Languages"] = "eng"
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(config)
                .Build();

            _watcherServiceMock
                .Setup(x => x.WatchAsync(tempDir, It.IsAny<OcrSettings>(), It.IsAny<CancellationToken>()))
                .Returns<string, OcrSettings, CancellationToken>(async (_, _, ct) =>
                {
                    // Wait for cancellation
                    try { await Task.Delay(Timeout.Infinite, ct); }
                    catch (OperationCanceledException) { }
                });

            using var service = new MultiWatcherBackgroundService(configuration, _watcherServiceMock.Object, _loggerMock.Object);
            using var cts = new CancellationTokenSource();
            await service.StartAsync(cts.Token);

            // Wait a bit for the watcher to start
            await Task.Delay(50, CancellationToken.None);

            // Cancel and stop
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);

            _watcherServiceMock.Verify(
                x => x.WatchAsync(tempDir, It.Is<OcrSettings>(s => s.Suffix == "_test" && s.Languages == "eng"), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
