namespace KazoOCR.Tests.ApiIntegrationTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using KazoOCR.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Integration tests for authentication API endpoints.
/// Tests cover setup, login, and status scenarios.
/// </summary>
public class AuthEndpointTests : IDisposable
{
    private readonly string _testDataPath;
    private bool _disposed;

    public AuthEndpointTests()
    {
        _testDataPath = Path.Join(Path.GetTempPath(), $"kazoocr-auth-test-{Guid.NewGuid()}");
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

    // CA2000 is suppressed because the factory is returned and disposed by callers via 'using var factory = CreateFactory(...)'
#pragma warning disable CA2000
    private WebApplicationFactory<KazoOCR.Api.Program> CreateFactory(string? defaultPassword = null, string testId = "")
    {
        var uniqueDataPath = Path.Join(_testDataPath, testId + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(uniqueDataPath);

        var mockOcrRunner = new Mock<IOcrProcessRunner>();
        var mockFileService = new Mock<IOcrFileService>();
        var mockWatcherService = new Mock<IWatcherService>();

        return new WebApplicationFactory<KazoOCR.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing service registrations
                    RemoveService<IOcrProcessRunner>(services);
                    RemoveService<IOcrFileService>(services);
                    RemoveService<IWatcherService>(services);
                    RemoveService<IAuthService>(services);

                    // Add mocked Core services
                    services.AddSingleton(mockOcrRunner.Object);
                    services.AddSingleton(mockFileService.Object);
                    services.AddSingleton(mockWatcherService.Object);

                    // Add test configuration for auth service
                    services.AddSingleton<IAuthService>(sp =>
                    {
                        var configDict = new Dictionary<string, string?>
                        {
                            ["DATA_PATH"] = uniqueDataPath
                        };

                        if (defaultPassword != null)
                        {
                            configDict["DEFAULT_PASSWORD"] = defaultPassword;
                        }

                        var config = new ConfigurationBuilder()
                            .AddInMemoryCollection(configDict)
                            .Build();
                        var logger = sp.GetRequiredService<ILogger<AuthService>>();
                        return new AuthService(config, logger);
                    });
                });
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

    [Fact]
    public async Task GetStatus_NotConfigured_ReturnsFalse()
    {
        // Arrange
        using var factory = CreateFactory(testId: "status-not-configured");
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        content.Should().NotBeNull();
        content!.Configured.Should().BeFalse();
    }

    [Fact]
    public async Task Setup_NotConfigured_Returns200()
    {
        // Arrange
        using var factory = CreateFactory(testId: "setup-first-call");
        using var client = factory.CreateClient();
        var request = new SetupRequest("ValidPassword123");

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/setup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Setup_AlreadyConfigured_Returns409()
    {
        // Arrange
        using var factory = CreateFactory(testId: "setup-already-configured");
        using var client = factory.CreateClient();

        // First setup
        var firstRequest = new SetupRequest("FirstPassword123");
        await client.PostAsJsonAsync("/api/auth/setup", firstRequest);

        // Act - try setup again
        var secondRequest = new SetupRequest("SecondPassword456");
        var response = await client.PostAsJsonAsync("/api/auth/setup", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        using var factory = CreateFactory(testId: "login-valid");
        using var client = factory.CreateClient();
        const string password = "ValidPassword123";

        // Setup password first
        await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(password));

        // Act
        var loginRequest = new LoginRequest(password);
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        content.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        // Arrange
        using var factory = CreateFactory(testId: "login-invalid");
        using var client = factory.CreateClient();

        // Setup password first
        await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("CorrectPassword123"));

        // Act - try wrong password
        var loginRequest = new LoginRequest("WrongPassword");
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Setup_WithDefaultPassword_Returns409()
    {
        // Arrange - create factory with default password set
        using var factory = CreateFactory(defaultPassword: "DefaultPassword123", testId: "setup-with-default");
        using var client = factory.CreateClient();

        // Verify it's already configured
        var statusResponse = await client.GetAsync("/api/auth/status");
        var status = await statusResponse.Content.ReadFromJsonAsync<AuthStatusResponse>();
        status!.Configured.Should().BeTrue("default password should configure the system");

        // Act - try to setup another password
        var request = new SetupRequest("NewPassword456");
        var response = await client.PostAsJsonAsync("/api/auth/setup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithDefaultPassword_ReturnsToken()
    {
        // Arrange - create factory with default password set
        const string defaultPassword = "DefaultPassword123";
        using var factory = CreateFactory(defaultPassword: defaultPassword, testId: "login-with-default");
        using var client = factory.CreateClient();

        // Act
        var loginRequest = new LoginRequest(defaultPassword);
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_NotConfigured_ReturnsBadRequest()
    {
        // Arrange
        using var factory = CreateFactory(testId: "login-not-configured");
        using var client = factory.CreateClient();

        // Act - try to login without setup
        var loginRequest = new LoginRequest("AnyPassword123");
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_ReturnsSuccess()
    {
        // Arrange
        using var factory = CreateFactory(testId: "logout-success");
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Setup_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        using var factory = CreateFactory(testId: "setup-empty");
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Setup_ShortPassword_ReturnsBadRequest()
    {
        // Arrange
        using var factory = CreateFactory(testId: "setup-short");
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("short"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        using var factory = CreateFactory(testId: "login-empty");
        using var client = factory.CreateClient();

        // Setup first
        await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("ValidPassword123"));

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(""));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthEndpoints_BypassApiKeyMiddleware()
    {
        // Arrange - create factory with API key set
        var mockOcrRunner = new Mock<IOcrProcessRunner>();
        var mockFileService = new Mock<IOcrFileService>();
        var mockWatcherService = new Mock<IWatcherService>();
        
        var uniqueDataPath = Path.Join(_testDataPath, "api-key-bypass-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(uniqueDataPath);

#pragma warning disable CA2000 // The factory is properly disposed with 'using var'
        using var factory = new WebApplicationFactory<KazoOCR.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("API_KEY", "test-api-key");

                builder.ConfigureServices(services =>
                {
                    RemoveService<IOcrProcessRunner>(services);
                    RemoveService<IOcrFileService>(services);
                    RemoveService<IWatcherService>(services);
                    RemoveService<IAuthService>(services);

                    services.AddSingleton(mockOcrRunner.Object);
                    services.AddSingleton(mockFileService.Object);
                    services.AddSingleton(mockWatcherService.Object);

                    services.AddSingleton<IAuthService>(sp =>
                    {
                        var config = new ConfigurationBuilder()
                            .AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["DATA_PATH"] = uniqueDataPath
                            })
                            .Build();
                        var logger = sp.GetRequiredService<ILogger<AuthService>>();
                        return new AuthService(config, logger);
                    });
                });
            });
#pragma warning restore CA2000

        using var client = factory.CreateClient();
        // Note: NOT adding X-Api-Key header

        // Act - try to access auth endpoints without API key
        var statusResponse = await client.GetAsync("/api/auth/status");
        var setupResponse = await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("Password12345"));

        // Assert - auth endpoints should bypass API key middleware
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
