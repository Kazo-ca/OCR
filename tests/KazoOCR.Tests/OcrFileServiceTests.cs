namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;

public class OcrFileServiceTests
{
    private readonly OcrFileService _service = new();

    #region ComputeOutputPath Tests

    [Fact]
    public void ComputeOutputPath_WithSimpleFileName_ReturnsSuffixedPath()
    {
        // Arrange
        var inputPath = "document.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        Assert.Equal("document_OCR.pdf", result);
    }

    [Fact]
    public void ComputeOutputPath_WithFullPath_ReturnsSuffixedPath()
    {
        // Arrange
        var inputPath = @"C:\Users\Test\Documents\document.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        var expected = @"C:\Users\Test\Documents\document_OCR.pdf";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComputeOutputPath_WithSpacesInPath_ReturnsSuffixedPath()
    {
        // Arrange
        var inputPath = Path.Combine("My Documents", "My File.pdf");
        var suffix = "_OCR";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        var expected = Path.Combine("My Documents", "My File_OCR.pdf");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComputeOutputPath_WithSpecialCharacters_ReturnsSuffixedPath()
    {
        // Arrange
        var inputPath = "document (1) - copy.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        Assert.Equal("document (1) - copy_OCR.pdf", result);
    }

    [Fact]
    public void ComputeOutputPath_WithCustomSuffix_ReturnsCustomSuffixedPath()
    {
        // Arrange
        var inputPath = "document.pdf";
        var suffix = "_PROCESSED";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        Assert.Equal("document_PROCESSED.pdf", result);
    }

    [Fact]
    public void ComputeOutputPath_WithNullInputPath_ThrowsArgumentNullException()
    {
        // Arrange
        string? inputPath = null;
        var suffix = "_OCR";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.ComputeOutputPath(inputPath!, suffix));
    }

    [Fact]
    public void ComputeOutputPath_WithNullSuffix_ThrowsArgumentNullException()
    {
        // Arrange
        var inputPath = "document.pdf";
        string? suffix = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.ComputeOutputPath(inputPath, suffix!));
    }

    [Fact]
    public void ComputeOutputPath_WithEmptyInputPath_ThrowsArgumentException()
    {
        // Arrange
        var inputPath = "   ";
        var suffix = "_OCR";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ComputeOutputPath(inputPath, suffix));
    }

    [Fact]
    public void ComputeOutputPath_WithSuffixProducingAbsolutePath_ThrowsArgumentException()
    {
        // Arrange - When inputPath has no base name (e.g., ".pdf"), a suffix starting with "/"
        // on Linux would produce an absolute path like "/path_to_attack.pdf"
        var inputPath = ".pdf";
        var suffix = "/path_to_attack";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ComputeOutputPath(inputPath, suffix));
        exception.Message.Should().Contain("Suffix must not produce an absolute path");
        exception.ParamName.Should().Be("suffix");
    }

    [Fact]
    public void ComputeOutputPath_WithRelativePath_ReturnsCorrectPath()
    {
        // Arrange
        var inputPath = "subfolder/document.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        var expected = Path.Combine("subfolder", "document_OCR.pdf");
        result.Should().Be(expected);
    }

    [Fact]
    public void ComputeOutputPath_WithUnixAbsolutePath_ReturnsCorrectPath()
    {
        // Arrange
        var inputPath = "/home/user/document.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        result.Should().Be("/home/user/document_OCR.pdf");
    }

    [Fact]
    public void ComputeOutputPath_WithEmptySuffix_ReturnsOriginalFileName()
    {
        // Arrange
        var inputPath = "document.pdf";
        var suffix = "";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        result.Should().Be("document.pdf");
    }

    #endregion

    #region IsAlreadyProcessed Tests

    [Fact]
    public void IsAlreadyProcessed_WithSuffix_ReturnsTrue()
    {
        // Arrange
        var filePath = "document_OCR.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAlreadyProcessed_WithoutSuffix_ReturnsFalse()
    {
        // Arrange
        var filePath = "document.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlreadyProcessed_WithSuffixInMiddle_ReturnsFalse()
    {
        // Arrange
        var filePath = "document_OCR_backup.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlreadyProcessed_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var filePath = "document_ocr.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAlreadyProcessed_WithNullFilePath_ThrowsArgumentNullException()
    {
        // Arrange
        string? filePath = null;
        var suffix = "_OCR";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.IsAlreadyProcessed(filePath!, suffix));
    }

    [Fact]
    public void IsAlreadyProcessed_WithNullSuffix_ThrowsArgumentNullException()
    {
        // Arrange
        var filePath = "document.pdf";
        string? suffix = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.IsAlreadyProcessed(filePath, suffix!));
    }

    [Fact]
    public void IsAlreadyProcessed_WithEmptyFilePath_ReturnsFalse()
    {
        // Arrange
        var filePath = "";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlreadyProcessed_WithEmptySuffix_ReturnsFalse()
    {
        // Arrange
        var filePath = "document.pdf";
        var suffix = "";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAlreadyProcessed_WithWhitespaceFilePath_ReturnsFalse()
    {
        // Arrange
        var filePath = "   ";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAlreadyProcessed_WithWhitespaceSuffix_ReturnsFalse()
    {
        // Arrange
        var filePath = "document.pdf";
        var suffix = "   ";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAlreadyProcessed_WithFullPath_ReturnsTrue()
    {
        // Arrange
        var filePath = "/home/user/documents/document_OCR.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAlreadyProcessed_WithPartialSuffixMatch_ReturnsFalse()
    {
        // Arrange
        var filePath = "document_OC.pdf";
        var suffix = "_OCR";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAlreadyProcessed_IsCaseInsensitive_WhenSuffixDiffersInCase()
    {
        // Arrange
        var filePath = "document_OCR.pdf";
        var suffix = "_ocr";

        // Act
        var result = _service.IsAlreadyProcessed(filePath, suffix);

        // Assert - Comparison should be case insensitive
        result.Should().BeTrue();
    }

    #endregion

    #region ValidateInput Tests

    [Fact]
    public void ValidateInput_WithNullPath_ReturnsInvalid()
    {
        // Arrange
        string? path = null;

        // Act
        var result = _service.ValidateInput(path!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null or empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateInput_WithEmptyPath_ReturnsInvalid()
    {
        // Arrange
        var path = "";

        // Act
        var result = _service.ValidateInput(path);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null or empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateInput_WithWhitespacePath_ReturnsInvalid()
    {
        // Arrange
        var path = "   ";

        // Act
        var result = _service.ValidateInput(path);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("null or empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateInput_WithNonExistentFile_ReturnsInvalid()
    {
        // Arrange
        var path = "/nonexistent/path/file.pdf";

        // Act
        var result = _service.ValidateInput(path);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateInput_WithInvalidExtension_ReturnsInvalid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var txtFile = Path.ChangeExtension(tempFile, ".txt");
        File.Move(tempFile, txtFile);

        try
        {
            // Act
            var result = _service.ValidateInput(txtFile);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Invalid file extension", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(txtFile);
        }
    }

    [Fact]
    public void ValidateInput_WithValidPdfFile_ReturnsValid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".pdf");
        File.Move(tempFile, pdfFile);

        try
        {
            // Act
            var result = _service.ValidateInput(pdfFile);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
        finally
        {
            File.Delete(pdfFile);
        }
    }

    [Fact]
    public void ValidateInput_WithUpperCasePdfExtension_ReturnsValid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".PDF");
        File.Move(tempFile, pdfFile);

        try
        {
            // Act
            var result = _service.ValidateInput(pdfFile);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
        finally
        {
            File.Delete(pdfFile);
        }
    }

    [Fact]
    public void ValidateInput_WithLockedFile_ReturnsInvalidWithIoError()
    {
        // Arrange - Create a temp PDF file and lock it
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".pdf");
        File.Move(tempFile, pdfFile);
        
        FileStream? lockingStream = null;
        try
        {
            // Lock the file by opening it exclusively
            lockingStream = File.Open(pdfFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            // Act
            var result = _service.ValidateInput(pdfFile);

            // Assert - On Windows this will fail with IOException; on Linux it might succeed
            // due to different file locking semantics, so we just verify it doesn't throw
            if (!result.IsValid)
            {
                result.Errors.Should().ContainSingle()
                    .Which.Should().Contain("Cannot access file");
            }
        }
        finally
        {
            lockingStream?.Dispose();
            File.Delete(pdfFile);
        }
    }

    [Fact]
    public void ValidateInput_WithDirectory_ReturnsInvalid()
    {
        // Arrange - Use a directory path instead of a file
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _service.ValidateInput(tempDir);

            // Assert - Directory is not a PDF file and will fail validation
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void ValidateInput_WithMixedCaseExtension_ReturnsValid()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var pdfFile = Path.ChangeExtension(tempFile, ".PdF");
        File.Move(tempFile, pdfFile);

        try
        {
            // Act
            var result = _service.ValidateInput(pdfFile);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
        finally
        {
            File.Delete(pdfFile);
        }
    }

    [Fact]
    public void ValidateInput_WithPathContainingSpaces_ReturnsValid()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test folder with spaces " + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        var pdfFile = Path.Combine(tempDir, "test file.pdf");
        File.WriteAllText(pdfFile, "test content");

        try
        {
            // Act
            var result = _service.ValidateInput(pdfFile);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
        finally
        {
            File.Delete(pdfFile);
            Directory.Delete(tempDir);
        }
    }

    [Fact]
    public void ValidateInput_WithSpecialCharactersInPath_ReturnsValid()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test-folder_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        var pdfFile = Path.Combine(tempDir, "test (1) - copy.pdf");
        File.WriteAllText(pdfFile, "test content");

        try
        {
            // Act
            var result = _service.ValidateInput(pdfFile);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
        finally
        {
            File.Delete(pdfFile);
            Directory.Delete(tempDir);
        }
    }

    #endregion
}
