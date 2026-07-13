using FluentAssertions;
using Moq;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Repositories;
using PixelTerminalUI.Engine.Screens;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Scenarios.PowerGridTerminal;
using TheLostGrid.Server.Scenarios.SectorNavigation;

namespace TheLostGrid.Server.Tests.Scenarios.PowerGridTerminal;

public sealed class PowerGridTerminalBuyEnergyCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly PowerGridTerminalBuyEnergyCommand _sut = new() { CharacterType = CharacterType.Hacker };

    public PowerGridTerminalBuyEnergyCommandTests()
    {
        _contextMock
            .SetupGet(c => c.SessionRepository)
            .Returns(_sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenScreenContextIsMismatchedType()
    {
        // Arrange
        Mock<TerminalScreen> genericScreenMock = new();
        _contextMock
            .SetupGet(c => c.Screen)
            .Returns(genericScreenMock.Object);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because processing execution pipeline blocks must reject actions targeted at unmatched terminal layout schemas");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalseAndSetErrorMessage_WhenActionCodeIsInvalid()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        PowerGridTerminalScreen initialScreen = new(CharacterType.Hacker, energy: 50, credits: 100)
        {
            Id = Guid.NewGuid(),
            Name = nameof(PowerGridTerminalScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("invalid_choice_code");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because submitting unrecognized control options must freeze input processing loops instantly");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "INVALID OPTION! SELECT 1 OR 0",
                Times.Once,
                "because interactive layouts require informative error streams when input criteria evaluations break");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(It.IsAny<Guid>(), It.IsAny<TerminalScreen>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "because faulty player interaction sequences must never persist back into cache databases");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFailWithErrorMessage_WhenWalletBalanceIsInsufficientForPurchase()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        PowerGridTerminalScreen initialScreen = new(CharacterType.Hacker, energy: 20, credits: 5) // Not enough credits
        {
            Id = Guid.NewGuid(),
            Name = nameof(PowerGridTerminalScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because economic purchase limits cannot finalize when capital limits fail structural constraint rules");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "NOT ENOUGH CREDITS (10 CR REQUIRED)",
                Times.Once,
                "because transaction layers must communicate clear wallet deficiency details back to user view boards");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(It.IsAny<Guid>(), It.IsAny<TerminalScreen>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "because under-funded ledger settlement attempts cannot alter active persistence state models");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldChargeCreditsAndIncreaseEnergy_WhenFundingIsSufficient()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        PowerGridTerminalScreen initialScreen = new(CharacterType.Hacker, energy: 40, credits: 30)
        {
            Id = Guid.NewGuid(),
            Name = nameof(PowerGridTerminalScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1 "); // Checking extra padding to confirm Trim rules evaluate cleanly
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because valid economic parameters complete the currency-to-energy conversion process cleanly");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<PowerGridTerminalScreen>(s =>
                    s.Energy == 65 &&
                    s.Credits == 20 &&
                    s.Name == nameof(PowerGridTerminalScreen)),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because successful asset generation trades must synchronously commit refreshed value markers to our cluster storage database");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCapEnergyAtOneHundredPercent_WhenAdditionExceedsMaximumLimits()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        PowerGridTerminalScreen initialScreen = new(CharacterType.Hacker, energy: 90, credits: 20)
        {
            Id = Guid.NewGuid(),
            Name = nameof(PowerGridTerminalScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because power regeneration rules handle excess capacity values gracefully via clamping tools");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<PowerGridTerminalScreen>(s =>
                    s.Energy == 100 &&
                    s.Credits == 10),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because cellular matrices have strict physical saturation safety restrictions capping engine statistics exactly at one hundred percent");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnToNavigationHubWithoutChangingAssets_WhenUserSelectsExitCode()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        PowerGridTerminalScreen initialScreen = new(CharacterType.Hacker, energy: 55, credits: 42)
        {
            Id = Guid.NewGuid(),
            Name = nameof(PowerGridTerminalScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("0");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because selecting escape commands fires legitimate workflow rerouting sequences");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorNavigationScreen>(s =>
                    s.Energy == 55 &&
                    s.Credits == 42 &&
                    s.Name == nameof(SectorNavigationScreen)),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because exiting auxiliary generator spaces must safely return operators to the primary navigational dashboard mapping layout");
    }
}
