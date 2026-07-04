using FluentAssertions;
using Moq;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using TheLostGrid.Server.Scenarios.Help;
using TheLostGrid.Server.Tests.Scenarios.Help.Fakes;

namespace TheLostGrid.Server.Tests.Scenarios.Help;

public sealed class HelpDismissCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly HelpDismissCommand _sut = new();

    public HelpDismissCommandTests()
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
            .BeFalse("because processing execution pipeline must fail immediately if the ambient communication channel state object is absent");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenActiveScreenCannotBeResolvedFromSessionCache()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();

        _contextMock
            .SetupGet(c => c.SessionId)
            .Returns(targetSessionId);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TerminalScreen?)null);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because returning to a previous interface frame requires a valid active viewport context descriptor to extract ancestral markers");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenActiveScreenDoesNotContainParentScreenLink()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        FakeHelpScreen detachedScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(FakeHelpScreen),
            SessionId = targetSessionId,
            ParentScreenId = null // Simulated broken structural lineage tree marker
        };

        _contextMock
            .SetupGet(c => c.SessionId)
            .Returns(targetSessionId);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detachedScreen);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because a view layer fallback action must terminate if the structural relationship linking the child screen to a parent context is missing");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRestoreAndSaveAncestralScreen_WhenValidParentLinkageIsPresent()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        Guid parentScreenId = Guid.NewGuid();

        FakeHelpScreen activeScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(FakeHelpScreen),
            SessionId = targetSessionId,
            ParentScreenId = parentScreenId
        };

        FakeGameMenuScreen parentScreen = new()
        {
            Id = parentScreenId,
            Name = nameof(FakeGameMenuScreen),
            SessionId = targetSessionId
        };

        _contextMock
            .SetupGet(c => c.SessionId)
            .Returns(targetSessionId);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeScreen);

        _sessionRepositoryMock
            .Setup(r => r.GetScreenByIdAsync(targetSessionId, parentScreenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentScreen);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because identifying and loading a historically accurate parent matrix frame completes the presentation roll-back workflow safely");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<TerminalScreen>(s => s.Id == parentScreenId),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because the active pointer reference within the persistent caching cluster layer must map back onto the historical ancestral screen view state");
    }
}
