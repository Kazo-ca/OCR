namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.CLI;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;
using Moq;

public class OcrCommandTests
{
    private const int UniqueIdLength = 8;

    private readonly Mock<IOcrFileService> _fileServiceMock;
    private readonly Mock<IOcrProcessRunner> _processRunnerMock;
    private readonly Mock<ILogger<OcrCommand>> _loggerMock;
    private readonly OcrCommand _command;

    public OcrCommandTests()
    {
        _fileServiceMock = new Mock<IOcrFileService>();
        _processRunnerMock = new Mock<IOcrProcessRunner>();
        _loggerMock = new Mock<ILogger<OcrCommand>>();
        _command = new OcrCommand(_fileServiceMock.Object, _processRunnerMock.Object, _loggerMock.Object);
    }

    /// <summary>
    /// Creates a temporary test directory with a unique name.
    /// </summary>
    /// <returns>The full path to the created temporary directory.</returns>
    private static string CreateTemporaryTestDirectory()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..UniqueIdLength];
        var directoryName = $"ocr-test-{uniqueId}";
        var tempDir = Path.Join(Path.GetTempPath(), directoryName);
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullFileService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new OcrCommand(null!, _processRunnerMock.Object, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("fileService");
    }

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new OcrCommand(_fileServiceMock.Object, null!, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("processRunner");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new OcrCommand(_fileServiceMock.Object, _processRunnerMock.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Execute Tests - Invalid Arguments

    [Fact]
    public async Task Execute_WithNullInput_ReturnsInvalidArguments()
    {
        // Act
        var result = await _command.Execute(
            input: null!,
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.InvalidArguments);
    }

    [Fact]
    public async Task Execute_WithEmptyInput_ReturnsInvalidArguments()
    {
        // Act
        var result = await _command.Execute(
            input: "",
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.InvalidArguments);
    }

    [Fact]
    public async Task Execute_WithWhitespaceInput_ReturnsInvalidArguments()
    {
        // Act
        var result = await _command.Execute(
            input: "   ",
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.InvalidArguments);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    [InlineData(-100)]
    public async Task Execute_WithOutOfRangeOptimize_ReturnsInvalidArguments(int invalidOptimize)
    {
        // Act
        var result = await _command.Execute(
            input: "/path/to/document.pdf",
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: invalidOptimize);

        // Assert
        result.Should().Be((int)ExitCodes.InvalidArguments);
        _processRunnerMock.Verify(x => x.RunAsync(
            It.IsAny<OcrSettings>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Execute Tests - Single File

    [Fact]
    public async Task Execute_WithNonExistentFile_ReturnsFileNotFound()
    {
        // Arrange
        var inputPath = "/nonexistent/file.pdf";
        _fileServiceMock.Setup(x => x.ValidateInput(inputPath))
            .Returns(ValidationResult.Failure($"File does not exist: {inputPath}"));

        // Act
        var result = await _command.Execute(
            input: inputPath,
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.FileNotFound);
    }

    [Fact]
    public async Task Execute_WithInvalidFile_ReturnsInvalidArguments()
    {
        // Arrange
        var inputPath = "/path/to/invalid.txt";
        _fileServiceMock.Setup(x => x.ValidateInput(inputPath))
            .Returns(ValidationResult.Failure("Invalid file extension"));

        // Act
        var result = await _command.Execute(
            input: inputPath,
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.InvalidArguments);
    }

    [Fact]
    public async Task Execute_WithAlreadyProcessedFile_ReturnsSuccess()
    {
        // Arrange
        var inputPath = "/path/to/document_OCR.pdf";
        _fileServiceMock.Setup(x => x.ValidateInput(inputPath))
            .Returns(ValidationResult.Success());
        _fileServiceMock.Setup(x => x.IsAlreadyProcessed(inputPath, "_OCR"))
            .Returns(true);

        // Act
        var result = await _command.Execute(
            input: inputPath,
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.Success);
        _processRunnerMock.Verify(x => x.RunAsync(
            It.IsAny<OcrSettings>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_WithValidFile_ProcessesSuccessfully()
    {
        // Arrange
        var inputPath = "/path/to/document.pdf";
        var outputPath = "/path/to/document_OCR.pdf";

        _fileServiceMock.Setup(x => x.ValidateInput(inputPath))
            .Returns(ValidationResult.Success());
        _fileServiceMock.Setup(x => x.IsAlreadyProcessed(inputPath, "_OCR"))
            .Returns(false);
        _fileServiceMock.Setup(x => x.ComputeOutputPath(inputPath, "_OCR"))
            .Returns(outputPath);
        _processRunnerMock.Setup(x => x.RunAsync(
            It.IsAny<OcrSettings>(),
            inputPath,
            outputPath,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult.Success());

        // Act
        var result = await _command.Execute(
            input: inputPath,
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.Success);
        _processRunnerMock.Verify(x => x.RunAsync(
            It.IsAny<OcrSettings>(),
            inputPath,
            outputPath,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithOcrFailure_ReturnsOcrFailed()
    {
        // Arrange
        var inputPath = "/path/to/document.pdf";
        var outputPath = "/path/to/document_OCR.pdf";

        _fileServiceMock.Setup(x => x.ValidateInput(inputPath))
            .Returns(ValidationResult.Success());
        _fileServiceMock.Setup(x => x.IsAlreadyProcessed(inputPath, "_OCR"))
            .Returns(false);
        _fileServiceMock.Setup(x => x.ComputeOutputPath(inputPath, "_OCR"))
            .Returns(outputPath);
        _processRunnerMock.Setup(x => x.RunAsync(
            It.IsAny<OcrSettings>(),
            inputPath,
            outputPath,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult.Failure(1, "OCR failed"));

        // Act
        var result = await _command.Execute(
            input: inputPath,
            suffix: "_OCR",
            languages: "fra+eng",
            deskew: false,
            clean: false,
            rotate: false,
            optimize: 1);

        // Assert
        result.Should().Be((int)ExitCodes.OcrFailed);
    }

    [Fact]
    public async Task Execute_WithCustomOptions_PassesCorrectSettings()
    {
        // Arrange
        var inputPath = "/path/to/document.pdf";
        var outputPath = "/path/to/document_PROCESSED.pdf";
        OcrSettings? capturedSettings = null;

        _fileServiceMock.Setup(x => x.ValidateInput(inputPath))
            .Returns(ValidationResult.Success());
        _fileServiceMock.Setup(x => x.IsAlreadyProcessed(inputPath, "_PROCESSED"))
            .Returns(false);
        _fileServiceMock.Setup(x => x.ComputeOutputPath(inputPath, "_PROCESSED"))
            .Returns(outputPath);
        _processRunnerMock.Setup(x => x.RunAsync(
            It.IsAny<OcrSettings>(),
            inputPath,
            outputPath,
            It.IsAny<CancellationToken>()))
            .Callback<OcrSettings, string, string, CancellationToken>((settings, _, _, _) =>
                capturedSettings = settings)
            .ReturnsAsync(ProcessResult.Success());

        // Act
        var result = await _command.Execute(
            input: inputPath,
            suffix: "_PROCESSED",
            languages: "fra+deu",
            deskew: true,
            clean: true,
            rotate: true,
            optimize: 2);

        // Assert
        result.Should().Be((int)ExitCodes.Success);
        capturedSettings.Should().NotBeNull();
        capturedSettings!.Suffix.Should().Be("_PROCESSED");
        capturedSettings.Languages.Should().Be("fra+deu");
        capturedSettings.Deskew.Should().BeTrue();
        capturedSettings.Clean.Should().BeTrue();
        capturedSettings.Rotate.Should().BeTrue();
        capturedSettings.Optimize.Should().Be(2);
    }

    #endregion

    #region Execute Tests - Directory Processing

    [Fact]
    public async Task Execute_WithEmptyDirectory_ReturnsFileNotFound()
    {
        // Arrange
        var tempDir = CreateTemporaryTestDirectory();

        try
        {
            // Act
            var result = await _command.Execute(
                input: tempDir,
                suffix: "_OCR",
                languages: "fra+eng",
                deskew: false,
                clean: false,
                rotate: false,
                optimize: 1);

            // Assert
            result.Should().Be((int)ExitCodes.FileNotFound);
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task Execute_WithDirectoryContainingPdfs_ProcessesAllFiles()
    {
        // Arrange
        var tempDir = CreateTemporaryTestDirectory();
        var pdfFile1 = Path.Join(tempDir, "doc1.pdf");
        var pdfFile2 = Path.Join(tempDir, "doc2.pdf");
        File.WriteAllText(pdfFile1, "test1");
        File.WriteAllText(pdfFile2, "test2");

        try
        {
            _fileServiceMock.Setup(x => x.IsAlreadyProcessed(It.IsAny<string>(), "_OCR"))
                .Returns(false);
            _fileServiceMock.Setup(x => x.ValidateInput(It.IsAny<string>()))
                .Returns(ValidationResult.Success());
            _fileServiceMock.Setup(x => x.ComputeOutputPath(It.IsAny<string>(), "_OCR"))
                .Returns<string, string>((input, suffix) =>
                    Path.Join(
                        Path.GetDirectoryName(input)!,
                        Path.GetFileNameWithoutExtension(input) + suffix + Path.GetExtension(input)));
            _processRunnerMock.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult.Success());

            // Act
            var result = await _command.Execute(
                input: tempDir,
                suffix: "_OCR",
                languages: "fra+eng",
                deskew: false,
                clean: false,
                rotate: false,
                optimize: 1);

            // Assert
            result.Should().Be((int)ExitCodes.Success);
            _processRunnerMock.Verify(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Execute_WithDirectoryContainingAlreadyProcessedFiles_SkipsProcessed()
    {
        // Arrange
        var tempDir = CreateTemporaryTestDirectory();
        var pdfFile1 = Path.Join(tempDir, "doc1.pdf");
        var pdfFile2 = Path.Join(tempDir, "doc2_OCR.pdf");
        File.WriteAllText(pdfFile1, "test1");
        File.WriteAllText(pdfFile2, "test2");

        try
        {
            _fileServiceMock.Setup(x => x.ValidateInput(pdfFile1))
                .Returns(ValidationResult.Success());
            _fileServiceMock.Setup(x => x.ValidateInput(pdfFile2))
                .Returns(ValidationResult.Success());
            _fileServiceMock.Setup(x => x.IsAlreadyProcessed(pdfFile1, "_OCR"))
                .Returns(false);
            _fileServiceMock.Setup(x => x.IsAlreadyProcessed(pdfFile2, "_OCR"))
                .Returns(true);
            _fileServiceMock.Setup(x => x.ComputeOutputPath(pdfFile1, "_OCR"))
                .Returns(Path.Join(tempDir, "doc1_OCR.pdf"));
            _processRunnerMock.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                pdfFile1,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult.Success());

            // Act
            var result = await _command.Execute(
                input: tempDir,
                suffix: "_OCR",
                languages: "fra+eng",
                deskew: false,
                clean: false,
                rotate: false,
                optimize: 1);

            // Assert
            result.Should().Be((int)ExitCodes.Success);
            _processRunnerMock.Verify(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                pdfFile1,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
            _processRunnerMock.Verify(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                pdfFile2,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Execute_WithSubdirectoryContainingPdfs_ProcessesRecursively()
    {
        // Arrange
        var tempDir = CreateTemporaryTestDirectory();
        var subDir = Path.Join(tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        var pdfFile1 = Path.Join(tempDir, "doc1.pdf");
        var pdfFile2 = Path.Join(subDir, "doc2.pdf");
        File.WriteAllText(pdfFile1, "test1");
        File.WriteAllText(pdfFile2, "test2");

        try
        {
            _fileServiceMock.Setup(x => x.IsAlreadyProcessed(It.IsAny<string>(), "_OCR"))
                .Returns(false);
            _fileServiceMock.Setup(x => x.ValidateInput(It.IsAny<string>()))
                .Returns(ValidationResult.Success());
            _fileServiceMock.Setup(x => x.ComputeOutputPath(It.IsAny<string>(), "_OCR"))
                .Returns<string, string>((input, suffix) =>
                    Path.Join(
                        Path.GetDirectoryName(input)!,
                        Path.GetFileNameWithoutExtension(input) + suffix + Path.GetExtension(input)));
            _processRunnerMock.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult.Success());

            // Act
            var result = await _command.Execute(
                input: tempDir,
                suffix: "_OCR",
                languages: "fra+eng",
                deskew: false,
                clean: false,
                rotate: false,
                optimize: 1);

            // Assert
            result.Should().Be((int)ExitCodes.Success);
            _processRunnerMock.Verify(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task Execute_WithSomeFilesFailingOcr_ReturnsOcrFailed()
    {
        // Arrange
        var tempDir = CreateTemporaryTestDirectory();
        var pdfFile1 = Path.Join(tempDir, "doc1.pdf");
        var pdfFile2 = Path.Join(tempDir, "doc2.pdf");
        File.WriteAllText(pdfFile1, "test1");
        File.WriteAllText(pdfFile2, "test2");

        try
        {
            _fileServiceMock.Setup(x => x.IsAlreadyProcessed(It.IsAny<string>(), "_OCR"))
                .Returns(false);
            _fileServiceMock.Setup(x => x.ValidateInput(It.IsAny<string>()))
                .Returns(ValidationResult.Success());
            _fileServiceMock.Setup(x => x.ComputeOutputPath(It.IsAny<string>(), "_OCR"))
                .Returns<string, string>((input, suffix) =>
                    Path.Join(
                        Path.GetDirectoryName(input)!,
                        Path.GetFileNameWithoutExtension(input) + suffix + Path.GetExtension(input)));
            _processRunnerMock.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                pdfFile1,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult.Success());
            _processRunnerMock.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                pdfFile2,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessResult.Failure(1, "OCR failed"));

            // Act
            var result = await _command.Execute(
                input: tempDir,
                suffix: "_OCR",
                languages: "fra+eng",
                deskew: false,
                clean: false,
                rotate: false,
                optimize: 1);

            // Assert
            result.Should().Be((int)ExitCodes.OcrFailed);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Execute_WithCancellation_StopsProcessing()
    {
        // Arrange
        var tempDir = CreateTemporaryTestDirectory();
        var pdfFile1 = Path.Join(tempDir, "doc1.pdf");
        var pdfFile2 = Path.Join(tempDir, "doc2.pdf");
        File.WriteAllText(pdfFile1, "test1");
        File.WriteAllText(pdfFile2, "test2");

        using var cts = new CancellationTokenSource();

        try
        {
            _fileServiceMock.Setup(x => x.IsAlreadyProcessed(It.IsAny<string>(), "_OCR"))
                .Returns(false);
            _fileServiceMock.Setup(x => x.ValidateInput(It.IsAny<string>()))
                .Returns(ValidationResult.Success());
            _fileServiceMock.Setup(x => x.ComputeOutputPath(It.IsAny<string>(), "_OCR"))
                .Returns<string, string>((input, suffix) =>
                    Path.Join(
                        Path.GetDirectoryName(input)!,
                        Path.GetFileNameWithoutExtension(input) + suffix + Path.GetExtension(input)));

            // Cancel after first file
            _processRunnerMock.Setup(x => x.RunAsync(
                It.IsAny<OcrSettings>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Callback(() => cts.Cancel())
                .ReturnsAsync(ProcessResult.Success());

            // Act
            var result = await _command.Execute(
                input: tempDir,
                suffix: "_OCR",
                languages: "fra+eng",
                deskew: false,
                clean: false,
                rotate: false,
                optimize: 1,
                cancellationToken: cts.Token);

            // Assert
            result.Should().Be((int)ExitCodes.GeneralError);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    #endregion
}
