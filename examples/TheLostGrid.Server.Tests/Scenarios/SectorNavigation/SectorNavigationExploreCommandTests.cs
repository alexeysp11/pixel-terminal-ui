using FluentAssertions;
using Moq;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Repositories;
using PixelTerminalUI.Engine.Screens;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using TheLostGrid.Server.Scenarios.PowerGridTerminal;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;

namespace TheLostGrid.Server.Tests.Scenarios.SectorNavigation;

public sealed class SectorNavigationExploreCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();

    public SectorNavigationExploreCommandTests()
    {
        _contextMock
            .SetupGet(c => c.SessionRepository)
            .Returns(_sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalseAndSetError_WhenOptionIsMismatchedOrZero()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        SectorNavigationScreen currentScreen = new(CharacterType.Hacker, energy: 100, credits: 50)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = targetSessionId
        };

        SectorNavigationExploreCommand sut = new() { CharacterType = CharacterType.Hacker };

        _contextMock.SetupGet(c => c.Screen).Returns(currentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("invalid_input_string");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because mismatched choices cannot trigger safe execution changes across our environment slices");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "INVALID OPTION! SELECT 1, 2 OR 3",
                Times.Once,
                "because an invalid command input sequence must generate transparent navigation tip blocks for the view layers");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(It.IsAny<Guid>(), It.IsAny<TerminalScreen>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "because broken transaction validation gates must halt storage updates to ensure session continuity");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToTerminalHackScreen_WhenHackerSelectsOptionOneWithSufficientEnergy()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        Guid welcomeScreenId = Guid.NewGuid();
        SectorNavigationScreen currentScreen = new(CharacterType.Hacker, energy: 20, credits: 10)
        {
            Id = welcomeScreenId,
            Name = nameof(SectorNavigationScreen),
            SessionId = targetSessionId
        };

        SectorNavigationExploreCommand sut = new() { CharacterType = CharacterType.Hacker };

        _contextMock.SetupGet(c => c.Screen).Returns(currentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because satisfying the energy boundaries allows hacker profiles to securely enter system penetration layers");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<TerminalHackScreen>(s =>
                    s.CharacterType == CharacterType.Hacker &&
                    s.Energy == 20 &&
                    s.Credits == 10 &&
                    s.AttemptsLeft == 2 &&
                    s.ActiveHashes.Length == 3 &&
                    s.ParentScreenId == welcomeScreenId),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because entering breaching layers requires the generation of a randomized cryptokey sequence database");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFailWithErrorMessage_WhenHackerSelectsOptionOneWithInsufficientEnergy()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        SectorNavigationScreen currentScreen = new(CharacterType.Hacker, energy: 5, credits: 10)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = targetSessionId
        };

        SectorNavigationExploreCommand sut = new() { CharacterType = CharacterType.Hacker };

        _contextMock.SetupGet(c => c.Screen).Returns(currentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because insufficient power metrics block terminal cracking initialization layers completely");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "NOT ENOUGH ENERGY (10 ENG REQUIRED)",
                Times.Once,
                "because the system layer must inform the user regarding minimal resource boundaries required for activation");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToDroneDeploymentScreen_WhenNonHackerSelectsOptionOneWithSufficientEnergy()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        SectorNavigationScreen currentScreen = new(CharacterType.Rigger, energy: 15, credits: 50)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = targetSessionId
        };

        SectorNavigationExploreCommand sut = new() { CharacterType = CharacterType.Rigger };

        _contextMock.SetupGet(c => c.Screen).Returns(currentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because alternative operational classes initialize localized auxiliary assets when energy requirements pass basic filters");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<DroneDeploymentScreen>(s =>
                    s.CharacterType == CharacterType.Rigger &&
                    s.Energy == 15 &&
                    s.Credits == 50),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because valid rigger action streams map directly to mechanical launch interfaces within the scenario layers");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToSectorScannerScreenWithScrapCredits_WhenOptionTwoIsChosenWithValidEnergy()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        SectorNavigationScreen currentScreen = new(CharacterType.Rigger, energy: 40, credits: 100)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = targetSessionId
        };

        SectorNavigationExploreCommand sut = new() { CharacterType = CharacterType.Rigger };

        _contextMock.SetupGet(c => c.Screen).Returns(currentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("2");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because scavenge actions are valid when operational limits map over the critical threshold criteria");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorScannerScreen>(s =>
                    s.Energy == 10 &&
                    s.Credits >= 115 && s.Credits <= 135 &&
                    s.ScanResultLog.Contains("Abandoned data cache detected")),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "the cache framework must deduct forty units of exploration fuel and increase cash bounds by the randomized yield factor");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToPowerGridTerminalScreen_WhenOptionThreeIsChosenWithValidCredits()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        SectorNavigationScreen currentScreen = new(CharacterType.Rigger, energy: 10, credits: 12)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = targetSessionId
        };

        SectorNavigationExploreCommand sut = new() { CharacterType = CharacterType.Rigger };

        _contextMock.SetupGet(c => c.Screen).Returns(currentScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("3");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because choosing generator options is permitted when capital reserves exceed entry processing criteria fees");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<PowerGridTerminalScreen>(s =>
                    s.Energy == 10 &&
                    s.Credits == 12 &&
                    s.CharacterType == CharacterType.Rigger),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because accessing city grids routes players directly to currency re-liquidation terminals without consuming active fuel stats");
    }
}
