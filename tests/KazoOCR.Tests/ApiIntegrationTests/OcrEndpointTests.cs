namespace KazoOCR.Tests.ApiIntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>
/// Integration tests for OCR API endpoints.
/// Tests cover authentication scenarios and OCR processing functionality.
/// </summary>
public class OcrEndpointTests : IDisposable
{
    private readonly string _testDataPath;
    private bool _disposed;

    public OcrEndpointTests()
    {
        _testDataPath = Path.Join(Path.GetTempPath(), $"kazoocr-api-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }

        _disposed = true;
    }

    // CA2000 is suppressed here because:
    // 1. The factory is returned and disposed by callers via 'using var factory = CreateFactory(...)'
    // 2. The ByteArrayContent instances added to MultipartFormDataContent are owned/disposed by the MultipartFormDataContent
#pragma warning disable CA2000
    private static WebApplicationFactory<KazoOCR.Api.Program> CreateFactory(string? apiKey = null)
    {
        var mockOcrRunner = new Mock<IOcrProcessRunner>();
        mockOcrRunner
            .Setup(r => r.RunAsync(It.IsAny<OcrSettings>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessResult.Success());

        var mockFileService = new Mock<IOcrFileService>();
        mockFileService.Setup(f => f.IsAlreadyProcessed(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        mockFileService.Setup(f => f.ComputeOutputPath(It.IsAny<string>(), It.IsAny<string>())).Returns((string input, string suffix) => 
            Path.GetFileNameWithoutExtension(input) + suffix + ".pdf");

        return new WebApplicationFactory<KazoOCR.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing service registrations
                    RemoveService<IOcrProcessRunner>(services);
                    RemoveService<IOcrFileService>(services);
                    RemoveService<IWatcherService>(services);

                    // Add mocked services
                    services.AddSingleton(mockOcrRunner.Object);
                    services.AddSingleton(mockFileService.Object);
                    services.AddSingleton<IWatcherService>(new Mock<IWatcherService>().Object);
                });

                // Configure API key if provided (key is "API_KEY" because of KAZO_ prefix stripping)
                if (apiKey != null)
                {
                    builder.UseSetting("API_KEY", apiKey);
                }
            });
    }
#pragma warning restore CA2000

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

#pragma warning disable CA2000 // ByteArrayContent is owned/disposed by MultipartFormDataContent
    private static MultipartFormDataContent CreatePdfFormContent()
    {
        var formContent = new MultipartFormDataContent();
        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 minimal test content");
        var content = new ByteArrayContent(pdfBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        formContent.Add(content, "file", "test.pdf");
        return formContent;
    }

    private static MultipartFormDataContent CreateNonPdfFormContent()
    {
        var formContent = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("This is not a PDF file");
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        formContent.Add(content, "file", "test.txt");
        return formContent;
    }
#pragma warning restore CA2000

    [Fact]
    public async Task ProcessPdf_NoApiKey_NoConfig_Returns202()
    {
        // Arrange - no API key configured
        using var factory = CreateFactory(apiKey: null);
        using var client = factory.CreateClient();
        using var formContent = CreatePdfFormContent();

        // Act
        var response = await client.PostAsync("/api/ocr/process", formContent);

        // Assert - should accept the request when no API key is configured
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<OcrJobResult>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessPdf_WithApiKey_MissingHeader_Returns401()
    {
        // Arrange - API key configured but no header provided
        using var factory = CreateFactory(apiKey: "test-api-key");
        using var client = factory.CreateClient();
        using var formContent = CreatePdfFormContent();

        // Act - request without X-Api-Key header
        var response = await client.PostAsync("/api/ocr/process", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProcessPdf_WithApiKey_WrongKey_Returns401()
    {
        // Arrange - API key configured with wrong header value
        using var factory = CreateFactory(apiKey: "test-api-key");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
        using var formContent = CreatePdfFormContent();

        // Act
        var response = await client.PostAsync("/api/ocr/process", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProcessPdf_WithApiKey_CorrectKey_Returns202()
    {
        // Arrange - API key configured with correct header value
        const string apiKey = "test-api-key";
        using var factory = CreateFactory(apiKey: apiKey);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        using var formContent = CreatePdfFormContent();

        // Act
        var response = await client.PostAsync("/api/ocr/process", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<OcrJobResult>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessPdf_InvalidFile_Returns400()
    {
        // Arrange - upload a non-PDF file
        using var factory = CreateFactory(apiKey: null);
        using var client = factory.CreateClient();
        using var formContent = CreateNonPdfFormContent();

        // Act
        var response = await client.PostAsync("/api/ocr/process", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetJob_UnknownId_Returns404()
    {
        // Arrange
        using var factory = CreateFactory(apiKey: null);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/ocr/jobs/unknown-job-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHealth_Returns200()
    {
        // Arrange
        using var factory = CreateFactory(apiKey: null);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSwagger_Returns200()
    {
        // Arrange
        using var factory = CreateFactory(apiKey: null);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.Should().NotBeNull();
        
        // Verify it has OpenAPI structure
        jsonDoc.RootElement.TryGetProperty("openapi", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetJobs_Returns200()
    {
        // Arrange
        using var factory = CreateFactory(apiKey: null);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/ocr/jobs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jobs = await response.Content.ReadFromJsonAsync<IEnumerable<OcrJobResult>>();
        jobs.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPdf_NoFile_Returns400()
    {
        // Arrange
        using var factory = CreateFactory(apiKey: null);
        using var client = factory.CreateClient();
        using var formContent = new MultipartFormDataContent();
        // Don't add any file

        // Act
        var response = await client.PostAsync("/api/ocr/process", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_WithApiKey_DoesNotRequireAuth()
    {
        // Arrange - API key configured
        using var factory = CreateFactory(apiKey: "test-api-key");
        using var client = factory.CreateClient();
        // Note: NOT adding X-Api-Key header

        // Act
        var response = await client.GetAsync("/health");

        // Assert - health endpoint should be accessible without API key
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_WithApiKey_DoesNotRequireAuth()
    {
        // Arrange - API key configured
        using var factory = CreateFactory(apiKey: "test-api-key");
        using var client = factory.CreateClient();
        // Note: NOT adding X-Api-Key header

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert - swagger endpoint should be accessible without API key
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
