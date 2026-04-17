namespace KazoOCR.Tests;

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
        var inputPath = Path.Combine("C:", "Users", "Test", "Documents", "document.pdf");
        var suffix = "_OCR";

        // Act
        var result = _service.ComputeOutputPath(inputPath, suffix);

        // Assert
        var expected = Path.Combine("C:", "Users", "Test", "Documents", "document_OCR.pdf");
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

    #endregion
}
