using FluentAssertions;
using Moq;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using TheLostGrid.Server.Scenarios.SectorScanner;

namespace TheLostGrid.Server.Tests.Scenarios.DroneDeployment;

public sealed class DroneDeploymentDeployCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly DroneDeploymentDeployCommand _sut = new() { CharacterType = CharacterType.Rigger };

    public DroneDeploymentDeployCommandTests()
    {
        _contextMock
            .SetupGet(c => c.SessionRepository)
            .Returns(_sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenContextParameterIsNull()
    {
        // Act
        bool result = await _sut.ExecuteAsync(null!);

        // Assert
        result
            .Should()
            .BeFalse("because operational pipelines must break immediately if the environment context reference is invalid");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenScreenContextIsIncorrectType()
    {
        // Arrange
        Mock<TerminalScreen> invalidScreenMock = new();
        _contextMock.SetupGet(c => c.Screen).Returns(invalidScreenMock.Object);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because launch procedures must abort when targeting mismatched hardware display models");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalseAndSetError_WhenInputIsMalformed()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        DroneDeploymentScreen deploymentScreen = new(CharacterType.Rigger, energy: 50, credits: 100)
        {
            Id = Guid.NewGuid(),
            Name = nameof(DroneDeploymentScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(deploymentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("invalid_input_action_code");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because submitting unrecognized choice parameters triggers field validation errors");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "INVALID OPTION! SELECT 1 OR 0",
                Times.Once,
                "because interactive screens require clear feedback instructions when operator choice parsing fails");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(It.IsAny<Guid>(), It.IsAny<TerminalScreen>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "because rejected transaction attempts cannot alter persistent cache data layers");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFailWithErrorMessage_WhenDroneLaunchExceedsAvailableEnergy()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        DroneDeploymentScreen deploymentScreen = new(CharacterType.Rigger, energy: 5, credits: 100) // Lower than 10 required
        {
            Id = Guid.NewGuid(),
            Name = nameof(DroneDeploymentScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(deploymentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because depleted engine core batteries block mechanical launch assemblies completely");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "NOT ENOUGH ENERGY (10 ENG REQUIRED)",
                Times.Once,
                "because systems must transmit explicit detail reports when fuel thresholds fall below basic operating standards");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteDeploymentProbabilityTree_WhenEnergyIsSufficient()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        DroneDeploymentScreen deploymentScreen = new(CharacterType.Rigger, energy: 50, credits: 100)
        {
            Id = Guid.NewGuid(),
            Name = nameof(DroneDeploymentScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(deploymentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1 "); // Space inclusion to verify Trim parameters evaluate cleanly
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because starting deployment commands with valid attributes triggers active logic execution steps");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorScannerScreen>(s =>
                    s.Energy == 40 && s.Credits == 115 && s.ScanResultLog.Contains("SUCCESS") ||
                    s.Energy == 30 && s.Credits == 100 && s.ScanResultLog.Contains("CRITICAL")),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because both success and failure outcome branches must compile into a valid data scanner result display model inside persistence storage");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnToNavigationHubWithoutChangingAssets_WhenUserSelectsRetreatCode()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        DroneDeploymentScreen deploymentScreen = new(CharacterType.Rigger, energy: 60, credits: 250)
        {
            Id = Guid.NewGuid(),
            Name = nameof(DroneDeploymentScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(deploymentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("0");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because choosing system return indicators initiates safe workflow routing frames");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorNavigationScreen>(s =>
                    s.Energy == 60 &&
                    s.Credits == 250 &&
                    s.Name == nameof(SectorNavigationScreen)),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because retreating out of active deployment terminals must transfer users back onto the primary grid map dashboard cleanly");
    }
}
