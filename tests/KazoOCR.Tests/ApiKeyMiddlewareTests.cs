namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

public class ApiKeyMiddlewareTests
{
    private readonly Mock<ILogger<ApiKeyMiddleware>> _loggerMock;
    private readonly DefaultHttpContext _httpContext;

    public ApiKeyMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ApiKeyMiddleware>>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    private IConfiguration CreateConfiguration(string? apiKey = null)
    {
        var configDict = new Dictionary<string, string?>();
        
        if (apiKey != null)
        {
            configDict["API_KEY"] = apiKey;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    [Fact]
    public async Task InvokeAsync_WithNoApiKeyConfigured_AllowsAllRequests()
    {
        // Arrange - no API key set (open mode)
        var configuration = CreateConfiguration();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/api/ocr/process";

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().NotBe(401);
    }

    [Fact]
    public async Task InvokeAsync_WithApiKeyConfigured_RequiresHeader()
    {
        // Arrange
        var configuration = CreateConfiguration(apiKey: "my-secret-key");
        RequestDelegate next = (ctx) => Task.CompletedTask;

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/api/ocr/process";
        // No X-Api-Key header set

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_WithCorrectApiKey_AllowsRequest()
    {
        // Arrange
        var apiKey = "my-secret-key";
        var configuration = CreateConfiguration(apiKey: apiKey);
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/api/ocr/process";
        _httpContext.Request.Headers["X-Api-Key"] = apiKey;

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().NotBe(401);
    }

    [Fact]
    public async Task InvokeAsync_WithWrongApiKey_Returns401()
    {
        // Arrange
        var configuration = CreateConfiguration(apiKey: "correct-key");
        RequestDelegate next = (ctx) => Task.CompletedTask;

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/api/ocr/process";
        _httpContext.Request.Headers["X-Api-Key"] = "wrong-key";

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_AuthEndpoints_SkipApiKeyCheck()
    {
        // Arrange
        var configuration = CreateConfiguration(apiKey: "my-secret-key");
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/api/auth/login";
        // No X-Api-Key header set

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert - auth endpoints should be accessible without API key
        nextCalled.Should().BeTrue();
        _httpContext.Response.StatusCode.Should().NotBe(401);
    }

    [Fact]
    public async Task InvokeAsync_AuthStatus_SkipApiKeyCheck()
    {
        // Arrange
        var configuration = CreateConfiguration(apiKey: "my-secret-key");
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/api/auth/status";

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_HealthEndpoint_SkipApiKeyCheck()
    {
        // Arrange
        var configuration = CreateConfiguration(apiKey: "my-secret-key");
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/health";

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_SwaggerEndpoint_SkipApiKeyCheck()
    {
        // Arrange
        var configuration = CreateConfiguration(apiKey: "my-secret-key");
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/openapi/v1.json";

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act & Assert
        var act = () => new ApiKeyMiddleware(next, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task InvokeAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var configuration = CreateConfiguration();
        RequestDelegate next = (ctx) => Task.CompletedTask;
        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);

        // Act & Assert
        var act = () => middleware.InvokeAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InvokeAsync_ApiKeyIsCaseSensitive()
    {
        // Arrange
        var configuration = CreateConfiguration(apiKey: "MySecretKey");
        RequestDelegate next = (ctx) => Task.CompletedTask;

        var middleware = new ApiKeyMiddleware(next, configuration, _loggerMock.Object);
        _httpContext.Request.Path = "/api/ocr/process";
        _httpContext.Request.Headers["X-Api-Key"] = "mysecretkey"; // lowercase

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert - should reject because key is case-sensitive
        _httpContext.Response.StatusCode.Should().Be(401);
    }
}
