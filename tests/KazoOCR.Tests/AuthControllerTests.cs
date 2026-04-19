namespace KazoOCR.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<KazoOCR.Api.Program>>, IDisposable
{
    private readonly WebApplicationFactory<KazoOCR.Api.Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testDataPath;

    public AuthControllerTests(WebApplicationFactory<KazoOCR.Api.Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        
        _testDataPath = Path.Join(Path.GetTempPath(), $"kazoocr-api-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing auth service registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add test configuration
                services.AddSingleton<IAuthService>(sp =>
                {
                    var config = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["DATA_PATH"] = _testDataPath
                        })
                        .Build();
                    var logger = sp.GetRequiredService<ILogger<AuthService>>();
                    return new AuthService(config, logger);
                });
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetStatus_WithNoPasswordConfigured_ReturnsFalse()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        content.Should().NotBeNull();
        content!.Configured.Should().BeFalse();
    }

    [Fact]
    public async Task Setup_WithValidPassword_ReturnsSuccess()
    {
        // Arrange
        var request = new SetupRequest("ValidPassword123");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/setup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify status is now configured
        var statusResponse = await _client.GetAsync("/api/auth/status");
        var content = await statusResponse.Content.ReadFromJsonAsync<AuthStatusResponse>();
        content!.Configured.Should().BeTrue();
    }

    [Fact]
    public async Task Setup_WithShortPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new SetupRequest("short");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/setup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Setup_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new SetupRequest("");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/setup", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Setup_CalledTwice_ReturnsConflict()
    {
        // Arrange - first setup
        var firstRequest = new SetupRequest("FirstPassword123");
        await _client.PostAsJsonAsync("/api/auth/setup", firstRequest);

        // Act - second setup
        var secondRequest = new SetupRequest("SecondPassword");
        var response = await _client.PostAsJsonAsync("/api/auth/setup", secondRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithNotConfigured_ReturnsBadRequest()
    {
        // Act - try to login before setup
        var loginRequest = new LoginRequest("AnyPassword");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        // Arrange - setup password first
        var password = "ValidPassword123";
        await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(password));

        // Act
        var loginRequest = new LoginRequest(password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        content.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange - setup password first
        await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("CorrectPassword123"));

        // Act - try wrong password
        var loginRequest = new LoginRequest("WrongPassword");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ReturnsBadRequest()
    {
        // Arrange - setup password first
        await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest("ValidPassword123"));

        // Act
        var loginRequest = new LoginRequest("");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_ReturnsSuccess()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_SetsSessionCookie()
    {
        // Arrange - setup password first
        var password = "ValidPassword123";
        await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(password));

        // Act
        var loginRequest = new LoginRequest(password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(c => c.Contains("kazo_session="));
    }

    [Fact]
    public async Task Health_Endpoint_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
