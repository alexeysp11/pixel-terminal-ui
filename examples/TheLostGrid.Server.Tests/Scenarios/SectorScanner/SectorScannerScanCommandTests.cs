using FluentAssertions;
using Moq;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using TheLostGrid.Server.Scenarios.SectorScanner;

namespace TheLostGrid.Server.Tests.Scenarios.SectorScanner;

public sealed class SectorScannerScanCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly SectorScannerScanCommand _sut = new() { CharacterType = CharacterType.Rigger };

    public SectorScannerScanCommandTests()
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
            .BeFalse("because the scanning workflow runner cannot evaluate data models that don't match sector scanner parameters");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalseAndSetErrorMessage_WhenInputValueIsAnInvalidReturnCode()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        SectorScannerScreen scannerScreen = new(CharacterType.Rigger, energy: 50, credits: 200, "SYSTEM RUNNING")
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorScannerScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(scannerScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("invalid_input_token");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because entering unexpected terminal control codes must lock the screen and reject transactional changes");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "INVALID COMMAND! ENTER 0 TO RETURN",
                Times.Once,
                "because an invalid command input sequence must generate transparent navigation tip blocks for the view layers");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(It.IsAny<Guid>(), It.IsAny<TerminalScreen>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "because invalid navigation signals cannot alter persistent user profile tracking slots or screens inside our distributed cache store");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToNavigationScreen_WhenUserEntersValidReturnCode()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        SectorScannerScreen scannerScreen = new(CharacterType.Rigger, energy: 45, credits: 130, "SYSTEM DIAGNOSTICS COMPLETE")
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorScannerScreen),
            SessionId = targetSessionId
        };

        _contextMock.SetupGet(c => c.Screen).Returns(scannerScreen);
        _contextMock.SetupGet(c => c.InputValue).Returns("0 "); // Testing extra spacing to guarantee Trim evaluation rules pass
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because submitting the accurate terminal clear token satisfies the execution pathway cleanly");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorNavigationScreen>(s =>
                    s.Energy == 45 &&
                    s.Credits == 130 &&
                    s.Name == nameof(SectorNavigationScreen)),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because completing scanner diagnostics must safely transfer players back into the tactical navigation map view frame");
    }
}
