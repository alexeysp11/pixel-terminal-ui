using FluentAssertions;
using Moq;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;

namespace TheLostGrid.Server.Tests.Scenarios.TerminalHack;

public sealed class TerminalHackSubmitKeyCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly TerminalHackSubmitKeyCommand _sut = new();

    public TerminalHackSubmitKeyCommandTests()
    {
        _contextMock
            .SetupGet(c => c.SessionRepository)
            .Returns(_sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenScreenContextIsInvalidType()
    {
        // Arrange
        Mock<TerminalScreen> invalidScreenMock = new();
        _contextMock
            .SetupGet(c => c.Screen)
            .Returns(invalidScreenMock.Object);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because the parsing routing loop must gracefully discard incoming data frames targeting mismatched screen schemas");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNavigateToSuccessScreen_WhenUserSelectsCorrectHashOption()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        Guid targetScreenId = Guid.NewGuid();
        string[] networkHashes = ["0x00000", "0xABCDE", "0x99999"];
        TextWidget[] components = [];

        TerminalHackScreen initialScreen = new(
            CharacterType.Hacker,
            energy: 80,
            credits: 10,
            attemptsLeft: 3,
            targetHash: "0xABCDE",
            activeHashes: networkHashes)
        {
            Id = targetScreenId,
            Name = nameof(TerminalHackScreen),
            SessionId = targetSessionId,
            Widgets = components
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("2");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because identifying the correct system signature must immediately complete the hacking stage with true state changes");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorScannerScreen>(s =>
                    s.Credits == 40 &&
                    s.Energy == 70 &&
                    s.ParentScreenId == targetScreenId &&
                    s.ScanResultLog.Contains("Bypass successful")),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because the execution pipeline must persist the calculated success payload matrix back inside the transaction store space cleanly");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeductAttemptsAndUpdateWidgets_WhenSelectionIsWrongButAttemptsRemain()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        string[] networkHashes = ["0x00000", "0xABCDE", "0x99999"];

        TextWidget attemptsWidget = new() { Id = Guid.NewGuid(), Name = "HackAttemptsLabel", Value = "INITIAL_STATE" };
        TextWidget warningWidget = new() { Id = Guid.NewGuid(), Name = "WarningText", Value = "INITIAL_STATE" };
        TextWidget[] components = [attemptsWidget, warningWidget];

        TerminalHackScreen initialScreen = new(
            CharacterType.Hacker,
            energy: 100,
            credits: 50,
            attemptsLeft: 3,
            targetHash: "0xABCDE",
            activeHashes: networkHashes)
        {
            Id = Guid.NewGuid(),
            Name = nameof(TerminalHackScreen),
            SessionId = targetSessionId,
            Widgets = components
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("1");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because a single incorrect entry cannot break the current terminal frame when structural failure safety allowances still apply");

        attemptsWidget.Value
            .Should()
            .Contain("[X] [X]", "because the UI layer labels must adapt dynamically to show the reduced operational margin constraints");

        warningWidget.Value
            .Should()
            .Contain("ENG 85%", "because real-time resource exhaustion messages must render back into components transparently");

        _contextMock
            .VerifySet(c => c.ErrorMessage = It.Is<string>(s => s.Contains("ACCESS DENIED")),
                Times.Once,
                "because error diagnostic streams should transmit descriptive operational warnings to notify the view layers");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<TerminalHackScreen>(s =>
                    s.AttemptsLeft == 2 &&
                    s.Energy == 85),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because modified attempt data models should serialize back down to our caching layer context frames synchronously");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTriggerLockoutScreen_WhenLastRemainingAttemptIsExhausted()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        string[] networkHashes = ["0x00000", "0xABCDE", "0x99999"];
        TextWidget[] components = [];

        TerminalHackScreen initialScreen = new(
            CharacterType.Hacker,
            energy: 50,
            credits: 10,
            attemptsLeft: 1, // Only one attempt remains
            targetHash: "0xABCDE",
            activeHashes: networkHashes)
        {
            Id = Guid.NewGuid(),
            Name = nameof(TerminalHackScreen),
            SessionId = targetSessionId,
            Widgets = components
        };

        _contextMock.SetupGet(c => c.Screen).Returns(initialScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("3"); // Incorrect index chosen
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because terminal lockouts represent a deterministic workflow completion event forcing transition routes forward");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorScannerScreen>(s =>
                    s.Credits == 10 &&
                    s.Energy == 30 &&
                    s.ScanResultLog.Contains("Terminal locked out")),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because exceeding permission authorization constraints must swap the operational view over to lockdown result logs");
    }
}
