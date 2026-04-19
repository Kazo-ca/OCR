namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

public class AuthServiceTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly Mock<ILogger<AuthService>> _loggerMock;

    public AuthServiceTests()
    {
        // Use a unique temp directory for each test run
        _testDataPath = Path.Join(Path.GetTempPath(), $"kazoocr-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);
        _loggerMock = new Mock<ILogger<AuthService>>();
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    private IConfiguration CreateConfiguration(string? defaultPassword = null, int? sessionExpirationHours = null)
    {
        var configDict = new Dictionary<string, string?>
        {
            ["KAZO_DATA_PATH"] = _testDataPath
        };

        if (defaultPassword != null)
        {
            configDict["KAZO_DEFAULT_PASSWORD"] = defaultPassword;
        }

        if (sessionExpirationHours.HasValue)
        {
            configDict["KAZO_SESSION_EXPIRATION_HOURS"] = sessionExpirationHours.Value.ToString();
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    [Fact]
    public void IsConfigured_WithNoPasswordSet_ReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act & Assert
        service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithDefaultPasswordEnvVar_ReturnsTrue()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "SecurePassword123");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act & Assert
        service.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task SetupPasswordAsync_WithNoExistingPassword_ReturnsTrue()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var result = await service.SetupPasswordAsync("NewPassword123");

        // Assert
        result.Should().BeTrue();
        service.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task SetupPasswordAsync_WithExistingPassword_ReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "ExistingPassword");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var result = await service.SetupPasswordAsync("NewPassword123");

        // Assert - cannot overwrite existing password
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SetupPasswordAsync_CalledTwice_SecondCallReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var firstResult = await service.SetupPasswordAsync("FirstPassword123");
        var secondResult = await service.SetupPasswordAsync("SecondPassword");

        // Assert
        firstResult.Should().BeTrue();
        secondResult.Should().BeFalse(); // 409 Conflict behavior
    }

    [Fact]
    public async Task SetupPasswordAsync_WithEmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetupPasswordAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SetupPasswordAsync("   "));
    }

    [Fact]
    public void ValidatePassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "CorrectPassword123";
        var configuration = CreateConfiguration(defaultPassword: password);
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var result = service.ValidatePassword(password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidatePassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "CorrectPassword");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var result = service.ValidatePassword("WrongPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_WithNoPasswordConfigured_ReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration();
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var result = service.ValidatePassword("AnyPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidatePassword_WithEmptyPassword_ReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "SecurePassword");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var result = service.ValidatePassword("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CreateToken_ReturnsValidToken()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "TestPassword");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var response = service.CreateToken();

        // Assert
        response.Token.Should().NotBeNullOrEmpty();
        response.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateToken_WithCustomExpiration_ReturnsCorrectExpiry()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "TestPassword", sessionExpirationHours: 1);
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var before = DateTimeOffset.UtcNow;
        var response = service.CreateToken();
        var after = DateTimeOffset.UtcNow;

        // Assert - should expire in roughly 1 hour
        response.ExpiresAt.Should().BeCloseTo(before.AddHours(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "TestPassword");
        var service = new AuthService(configuration, _loggerMock.Object);
        var loginResponse = service.CreateToken();

        // Act
        var result = service.ValidateToken(loginResponse.Token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "TestPassword");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var result = service.ValidateToken("invalid-token-value");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "TestPassword");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act & Assert
        service.ValidateToken("").Should().BeFalse();
        service.ValidateToken(null!).Should().BeFalse();
    }

    [Fact]
    public void InvalidateToken_RemovesToken()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "TestPassword");
        var service = new AuthService(configuration, _loggerMock.Object);
        var loginResponse = service.CreateToken();

        // Verify token is valid first
        service.ValidateToken(loginResponse.Token).Should().BeTrue();

        // Act
        service.InvalidateToken(loginResponse.Token);

        // Assert
        service.ValidateToken(loginResponse.Token).Should().BeFalse();
    }

    [Fact]
    public void InvalidateToken_WithInvalidToken_DoesNotThrow()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "TestPassword");
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act & Assert - should not throw
        var act = () => service.InvalidateToken("non-existent-token");
        act.Should().NotThrow();
    }

    [Fact]
    public void PasswordHash_IsPersistedToFile()
    {
        // Arrange
        var configuration = CreateConfiguration(defaultPassword: "PersistentPassword");
        var service1 = new AuthService(configuration, _loggerMock.Object);

        // Act - create a new service instance (simulating restart)
        var configuration2 = CreateConfiguration(); // No default password this time
        var service2 = new AuthService(configuration2, _loggerMock.Object);

        // Assert - password should be loaded from file
        service2.IsConfigured.Should().BeTrue();
        service2.ValidatePassword("PersistentPassword").Should().BeTrue();
    }

    [Fact]
    public void PasswordHash_IsNeverStoredInPlaintext()
    {
        // Arrange
        var password = "SecretPassword123";
        var configuration = CreateConfiguration(defaultPassword: password);
        var service = new AuthService(configuration, _loggerMock.Object);

        // Act
        var authFilePath = Path.Join(_testDataPath, "auth.json");
        var fileContent = File.ReadAllText(authFilePath);

        // Assert - password should not appear in plaintext in the file
        fileContent.Should().NotContain(password);
        fileContent.Should().Contain("$2"); // bcrypt hash prefix
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new AuthService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }
}
