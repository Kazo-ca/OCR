namespace KazoOCR.Tests;

using FluentAssertions;
using KazoOCR.CLI;
using KazoOCR.Core;
using Microsoft.Extensions.Logging;
using Moq;

public class ServiceCommandTests
{
    private readonly Mock<IServiceManager> _serviceManagerMock;
    private readonly Mock<IPrivilegeElevator> _privilegeElevatorMock;
    private readonly Mock<ILogger<ServiceCommand>> _loggerMock;
    private readonly ServiceCommand _command;

    public ServiceCommandTests()
    {
        _serviceManagerMock = new Mock<IServiceManager>();
        _privilegeElevatorMock = new Mock<IPrivilegeElevator>();
        _loggerMock = new Mock<ILogger<ServiceCommand>>();
        _command = new ServiceCommand(_serviceManagerMock.Object, _privilegeElevatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullServiceManager_ThrowsArgumentNullException()
    {
        var action = () => new ServiceCommand(null!, _privilegeElevatorMock.Object, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("serviceManager");
    }

    [Fact]
    public void Constructor_WithNullPrivilegeElevator_ThrowsArgumentNullException()
    {
        var action = () => new ServiceCommand(_serviceManagerMock.Object, null!, _loggerMock.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("privilegeElevator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new ServiceCommand(_serviceManagerMock.Object, _privilegeElevatorMock.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task Status_OnNonWindows_ReturnsSuccessWithMessage()
    {
        // Status should always succeed, showing a message on non-Windows
        var result = await _command.Status();

        // On non-Windows, it returns Success with a message
        result.Should().Be((int)ExitCodes.Success);
    }

    [Fact]
    public async Task Status_WhenServiceNotInstalled_ReturnsSuccess()
    {
        _serviceManagerMock.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceStatus
            {
                ServiceName = "KazoOCR",
                IsInstalled = false,
                State = "Not installed"
            });

        var result = await _command.Status();

        result.Should().Be((int)ExitCodes.Success);
    }

    [Fact]
    public async Task Status_WhenServiceRunning_ReturnsSuccess()
    {
        _serviceManagerMock.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceStatus
            {
                ServiceName = "KazoOCR",
                IsInstalled = true,
                State = "RUNNING",
                StartType = "AUTO_START",
                DisplayName = "KazoOCR PDF Processing Service"
            });

        var result = await _command.Status();

        result.Should().Be((int)ExitCodes.Success);
    }
}
