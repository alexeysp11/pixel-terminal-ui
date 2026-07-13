using FluentAssertions;
using Moq;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Repositories;
using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.Welcome;

namespace TheLostGrid.Server.Tests.Scenarios.Welcome;

public sealed class WelcomeStartGameCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly WelcomeStartGameCommand _sut = new();

    public WelcomeStartGameCommandTests()
    {
        _contextMock
            .SetupGet(c => c.SessionRepository)
            .Returns(_sessionRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateAndPersistCharacterCreationScreen_WhenFiredFromWelcomeContext()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        Guid welcomeScreenId = Guid.NewGuid();

        WelcomeScreen initialScreen = new()
        {
            Id = welcomeScreenId,
            Name = nameof(WelcomeScreen),
            SessionId = targetSessionId
        };

        _contextMock
            .SetupGet(c => c.SessionId)
            .Returns(targetSessionId);

        _contextMock
            .SetupGet(c => c.Screen)
            .Returns(initialScreen);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because triggering game initialization from the main splash display acts as a deterministic state change pathway");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<CharacterCreationScreen>(s =>
                    s.SessionId == targetSessionId &&
                    s.ParentScreenId == welcomeScreenId &&
                    s.Name == nameof(CharacterCreationScreen)),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because the session layer engine must overwrite the active cache space with the subsequent operational character genesis screen layout data");
    }
}
