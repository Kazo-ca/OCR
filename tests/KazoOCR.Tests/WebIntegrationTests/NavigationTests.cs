namespace KazoOCR.Tests.WebIntegrationTests;

using System.Net;
using FluentAssertions;
using KazoOCR.Web.Models;
using KazoOCR.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

/// <summary>
/// Integration tests for navigation and authentication guards in the web UI.
/// </summary>
public class NavigationTests : IDisposable
{
    private bool _disposed;

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

        _disposed = true;
    }

    private static Mock<IKazoApiClient> CreateMockApiClient(bool isConfigured, bool isAuthenticated = false)
    {
        var mock = new Mock<IKazoApiClient>();
        mock.Setup(c => c.GetAuthStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthStatus { Configured = isConfigured, Authenticated = isAuthenticated });
        mock.Setup(c => c.GetJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OcrJob>());
        mock.Setup(c => c.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse { Token = "test-token" });
        mock.Setup(c => c.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OcrSettings());
        return mock;
    }

    // CA2000 is suppressed because the factory is returned and disposed by callers via 'using var factory = CreateFactory(...)'
#pragma warning disable CA2000
    private static WebApplicationFactory<KazoOCR.Web.Program> CreateFactory(
        bool isConfigured = false,
        bool isAuthenticated = false)
    {
        var mockApiClient = CreateMockApiClient(isConfigured, isAuthenticated);

        return new WebApplicationFactory<KazoOCR.Web.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureServices(services =>
                {
                    // Remove existing HTTP client factory registration
                    var httpClientDescriptor = services.FirstOrDefault(d =>
                        d.ServiceType == typeof(IHttpClientFactory) ||
                        d.ServiceType == typeof(HttpClient) ||
                        d.ServiceType == typeof(IKazoApiClient));
                    if (httpClientDescriptor != null)
                    {
                        services.Remove(httpClientDescriptor);
                    }

                    // Remove all IKazoApiClient registrations
                    var apiClientDescriptors = services.Where(d =>
                        d.ServiceType == typeof(IKazoApiClient) ||
                        d.ImplementationType == typeof(KazoApiClient)).ToList();
                    foreach (var descriptor in apiClientDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    // Add mocked API client
                    services.AddScoped(_ => mockApiClient.Object);
                });
            });
    }
#pragma warning restore CA2000

    [Fact]
    public async Task Root_Unauthenticated_ReturnsPage()
    {
        // Arrange - not authenticated, but configured
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert - The page should return OK (Blazor handles redirects client-side via the AuthGuard component)
        // For server-side rendering, we check that the page loads without error
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Found);
    }

    [Fact]
    public async Task Root_NotConfigured_ReturnsPage()
    {
        // Arrange - not configured (first run)
        using var factory = CreateFactory(isConfigured: false, isAuthenticated: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert - The page should return OK (Blazor handles redirects client-side via the AuthGuard component)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Found);
    }

    [Fact]
    public async Task Upload_ReturnsPage()
    {
        // Arrange - authenticated
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/upload");

        // Assert - The page should return OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Setup_ReturnsPage()
    {
        // Arrange - not configured (should show setup page)
        using var factory = CreateFactory(isConfigured: false, isAuthenticated: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/setup");

        // Assert - The page should return OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ReturnsPage()
    {
        // Arrange - configured but not authenticated
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/login");

        // Assert - The page should return OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Settings_ReturnsPage()
    {
        // Arrange - authenticated
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/settings");

        // Assert - The page should return OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NotFound_ReturnsPage()
    {
        // Arrange
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/nonexistent-page-12345");

        // Assert - Should return NotFound status or the not-found page
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StaticAssets_AppCss_Returns200()
    {
        // Arrange
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: true);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/app.css");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task StaticAssets_Favicon_Returns200()
    {
        // Arrange
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: true);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/favicon.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Pages_ContainExpectedContent()
    {
        // Arrange
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: false);
        using var client = factory.CreateClient();

        // Act
        var loginResponse = await client.GetAsync("/login");
        var loginContent = await loginResponse.Content.ReadAsStringAsync();

        // Assert - Login page should contain sign in form elements
        loginContent.Should().Contain("KazoOCR");
    }

    [Fact]
    public async Task SetupPage_WhenNotConfigured_ShowsSetupForm()
    {
        // Arrange
        using var factory = CreateFactory(isConfigured: false, isAuthenticated: false);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/setup");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Setup");
    }

    [Fact]
    public async Task HomePage_WhenAuthenticated_ShowsDashboard()
    {
        // Arrange
        using var factory = CreateFactory(isConfigured: true, isAuthenticated: true);
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Dashboard");
    }
}
