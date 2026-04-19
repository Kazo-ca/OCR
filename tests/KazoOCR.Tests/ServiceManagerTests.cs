namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.Core;

public class ServiceManagerTests
{
    private readonly ServiceManager _serviceManager;

    public ServiceManagerTests()
    {
        _serviceManager = new ServiceManager();
    }

    [Fact]
    public void ServiceName_ReturnsDefaultServiceName()
    {
        _serviceManager.ServiceName.Should().Be(ServiceManager.DefaultServiceName);
    }

    [Fact]
    public void DefaultServiceName_IsKazoOCR()
    {
        ServiceManager.DefaultServiceName.Should().Be("KazoOCR");
    }

    [Fact]
    public void DefaultDisplayName_IsCorrect()
    {
        ServiceManager.DefaultDisplayName.Should().Be("KazoOCR PDF Processing Service");
    }

    [Fact]
    public void IsAdministrator_OnNonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows())
        {
            // Skip on Windows - this test is for non-Windows platforms
            return;
        }

        _serviceManager.IsAdministrator().Should().BeFalse();
    }

    [Fact]
    public async Task InstallAsync_OnNonWindows_ReturnsFailure()
    {
        if (OperatingSystem.IsWindows())
        {
            // Skip on Windows - this test is for non-Windows platforms
            return;
        }

        var result = await _serviceManager.InstallAsync("/tmp/test-config.json");

        result.IsSuccess.Should().BeFalse();
        result.StandardError.Should().Contain("only supported on Windows");
    }

    [Fact]
    public async Task UninstallAsync_OnNonWindows_ReturnsFailure()
    {
        if (OperatingSystem.IsWindows())
        {
            // Skip on Windows - this test is for non-Windows platforms
            return;
        }

        var result = await _serviceManager.UninstallAsync();

        result.IsSuccess.Should().BeFalse();
        result.StandardError.Should().Contain("only supported on Windows");
    }

    [Fact]
    public async Task GetStatusAsync_OnNonWindows_ReturnsNotApplicable()
    {
        if (OperatingSystem.IsWindows())
        {
            // Skip on Windows - this test is for non-Windows platforms
            return;
        }

        var status = await _serviceManager.GetStatusAsync();

        status.IsInstalled.Should().BeFalse();
        status.State.Should().Contain("not Windows");
    }
}
