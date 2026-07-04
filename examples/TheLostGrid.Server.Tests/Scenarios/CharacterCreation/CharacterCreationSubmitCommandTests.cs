using FluentAssertions;
using Moq;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.SectorNavigation;

namespace TheLostGrid.Server.Tests.Scenarios.CharacterCreation;

public sealed class CharacterCreationSubmitCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly CharacterCreationSubmitCommand _sut = new();

    public CharacterCreationSubmitCommandTests()
    {
        _contextMock
            .SetupGet(c => c.SessionRepository)
            .Returns(_sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalseAndSetErrorMessage_WhenInputValueIsEmptyOrWhitespace()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();

        _contextMock.SetupGet(c => c.InputValue).Returns("   ");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because submitting an empty operator class buffer breaks the validation rules");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "INVALID CLASS! ENTER 'H' OR 'R'",
                Times.Once,
                "because a failed character parsing routine must return clear interactive input hints to the layout layer");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(It.IsAny<Guid>(), It.IsAny<TerminalScreen>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "because an invalid genesis transaction execution path can never update the persistent database store");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalseAndSetErrorMessage_WhenInputValueIsAnInvalidCharacterClassCode()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();

        _contextMock.SetupGet(c => c.InputValue).Returns("X");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because unrecognizable profile codes must stop the game initialization pipeline instantly");

        _contextMock
            .VerifySet(c => c.ErrorMessage = "INVALID CLASS! ENTER 'H' OR 'R'",
                Times.Once,
                "because an explicit instructional alert message is required when input filtering checks reject an operator choice");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalseAndSetErrorMessage_WhenInputValueIsLongerThanSingleCharacterSpec()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();

        _contextMock.SetupGet(c => c.InputValue).Returns("HACKER");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because the registration sequence accepts exactly one abbreviation code character to prevent text parsing overflows");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInitializeAndSaveNavigationScreen_WhenUserSelectsHackerClassCode()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();

        _contextMock.SetupGet(c => c.InputValue).Returns(" h "); // Spacing ensures the ReadOnlySpan Trim operation is fully tested
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because mapping a legitimate class signature concludes the profile selection phase without operational exceptions");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorNavigationScreen>(s =>
                    s.CharacterType == CharacterType.Hacker &&
                    s.Energy == 100 &&
                    s.Credits == 50 &&
                    s.Name == nameof(SectorNavigationScreen)),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because creating a new operative profile must populate the session persistence cluster with a base resource profile");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldInitializeAndSaveNavigationScreen_WhenUserSelectsRiggerClassCode()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();

        _contextMock.SetupGet(c => c.InputValue).Returns("R");
        _contextMock.SetupGet(c => c.SessionId).Returns(targetSessionId);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because registering an alternative standard class token activates the game setup sequence successfully");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<SectorNavigationScreen>(s =>
                    s.CharacterType == CharacterType.Rigger &&
                    s.Energy == 100 &&
                    s.Credits == 50),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because the initialization pipeline must map the rigger choice and allocate identical starting fuel limits");
    }
}
