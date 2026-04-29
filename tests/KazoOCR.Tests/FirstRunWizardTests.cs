namespace KazoOCR.Tests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KazoOCR.Api.Models;
using KazoOCR.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tests for the First-Run Wizard redirect guard logic.
/// These tests verify the acceptance criteria for Issue 5.5:
/// - First startup without KAZO_DEFAULT_PASSWORD → automatic redirect to /setup (via API status)
/// - /setup page is inaccessible once configured (API returns configured=true)
/// - POST /api/auth/setup returns 409 on second call
/// </summary>
public class FirstRunWizardTests : IClassFixture<WebApplicationFactory<KazoOCR.Api.Program>>, IDisposable
{
    private readonly WebApplicationFactory<KazoOCR.Api.Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testDataPath;

    public FirstRunWizardTests(WebApplicationFactory<KazoOCR.Api.Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        
        _testDataPath = Path.Join(Path.GetTempPath(), $"kazoocr-wizard-test-{Guid.NewGuid()}");
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

    #region First-Run Detection (Acceptance Criteria: "First startup without KAZO_DEFAULT_PASSWORD → automatic redirect to /setup")

    [Fact]
    public async Task GetAuthStatus_OnFirstRun_ReturnsConfiguredFalse()
    {
        // This tests the API endpoint that the Web UI uses to determine
        // whether to redirect to /setup or /login
        
        // Act
        var response = await _client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        content.Should().NotBeNull();
        content!.Configured.Should().BeFalse("First run should have no password configured");
    }

    [Fact]
    public async Task GetAuthStatus_AfterSetup_ReturnsConfiguredTrue()
    {
        // This tests that after setup, the API returns configured=true
        // so the Web UI will redirect to /login instead of /setup
        
        // Arrange - Complete setup
        var setupRequest = new SetupRequest("ValidPassword123");
        var setupResponse = await _client.PostAsJsonAsync("/api/auth/setup", setupRequest);
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await _client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
        content.Should().NotBeNull();
        content!.Configured.Should().BeTrue("After setup, password should be configured");
    }

    #endregion

    #region Setup Page Guard (Acceptance Criteria: "/setup page is inaccessible once configured")

    [Fact]
    public async Task Setup_AfterAlreadyConfigured_ReturnsConflict()
    {
        // This tests that once a password is configured, 
        // the API returns 409 Conflict to prevent re-setup
        
        // Arrange - Complete first setup
        var firstPassword = "FirstPassword123";
        var firstSetupResponse = await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(firstPassword));
        firstSetupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try to setup again
        var secondPassword = "SecondPassword456";
        var secondSetupResponse = await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(secondPassword));

        // Assert
        secondSetupResponse.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "Second setup attempt should return 409 Conflict");
    }

    [Fact]
    public async Task Setup_AfterAlreadyConfigured_OriginalPasswordStillWorks()
    {
        // This verifies that failed re-setup doesn't affect the original password
        
        // Arrange - Complete first setup
        var firstPassword = "FirstPassword123";
        await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(firstPassword));

        // Try second setup (should fail)
        var secondPassword = "SecondPassword456";
        await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(secondPassword));

        // Act - Login with original password
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(firstPassword));

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Original password should still work after failed re-setup");
    }

    #endregion

    #region Password Validation (Acceptance Criteria: "Client-side validation enforces complexity rules")
    // Note: Client-side validation is in Blazor components, but we also test server-side validation

    [Theory]
    [InlineData("short")] // Less than 8 characters
    [InlineData("1234567")] // 7 characters, no uppercase
    [InlineData("")] // Empty
    public async Task Setup_WithWeakPassword_ReturnsBadRequest(string password)
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            $"Password '{password}' should be rejected as too weak");
    }

    [Theory]
    [InlineData("ValidPass1")] // 10 chars, uppercase, digit
    [InlineData("Password123")] // 11 chars, uppercase, digit
    [InlineData("Abcdefg1")] // 8 chars exactly, uppercase, digit
    public async Task Setup_WithStrongPassword_ReturnsOk(string password)
    {
        // Arrange - Create fresh test environment for each test
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            var testPath = Path.Join(Path.GetTempPath(), $"kazoocr-pw-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(testPath);
            
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton<IAuthService>(sp =>
                {
                    var config = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["DATA_PATH"] = testPath
                        })
                        .Build();
                    var logger = sp.GetRequiredService<ILogger<AuthService>>();
                    return new AuthService(config, logger);
                });
            });
        });
        
        using var client = factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(password));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Password '{password}' should be accepted as strong enough");
    }

    #endregion

    #region Redirect Flow Integration (Acceptance Criteria: "After successful setup → redirect to /login")
    // Note: Actual redirect is in Blazor, but we test the API flow

    [Fact]
    public async Task FullFirstRunFlow_SetupThenLogin_Succeeds()
    {
        // This tests the complete first-run wizard flow:
        // 1. Check status (configured=false)
        // 2. Setup password
        // 3. Check status (configured=true)
        // 4. Login with new password
        
        // Step 1: Verify not configured
        var statusResponse1 = await _client.GetAsync("/api/auth/status");
        var status1 = await statusResponse1.Content.ReadFromJsonAsync<AuthStatusResponse>();
        status1!.Configured.Should().BeFalse("Initial status should be not configured");

        // Step 2: Setup password
        var password = "NewSecurePassword123";
        var setupResponse = await _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(password));
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Setup should succeed");

        // Step 3: Verify now configured
        var statusResponse2 = await _client.GetAsync("/api/auth/status");
        var status2 = await statusResponse2.Content.ReadFromJsonAsync<AuthStatusResponse>();
        status2!.Configured.Should().BeTrue("After setup, should be configured");

        // Step 4: Login
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Login with new password should succeed");
        
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginContent!.Token.Should().NotBeNullOrEmpty("Login should return a session token");
    }

    #endregion
}
