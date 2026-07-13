using FluentAssertions;
using Moq;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Commands.DismissError;
using PixelTerminalUI.Engine.Repositories;
using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Tests.Commands.Fakes;

namespace PixelTerminalUI.Engine.Tests.Commands;

public sealed class DismissErrorCommandTests
{
    private readonly Mock<ICommandContext> _contextMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly DismissErrorCommand _sut = new();

    public DismissErrorCommandTests()
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
            .BeFalse("because the framework engine execution pipeline must fail immediately if the command environment state is absent");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenActiveScreenCannotBeFoundInSessionRepository()
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
            .BeFalse("because tearing down an error state frame requires a valid active layout context to find tracking linkages");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenCurrentScreenDoesNotContainParentScreenId()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        FakeTestingScreen orphanScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(FakeTestingScreen),
            SessionId = targetSessionId,
            ParentScreenId = null // Simulated broken structural tree marker
        };

        _contextMock
            .SetupGet(c => c.SessionId)
            .Returns(targetSessionId);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orphanScreen);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because the navigation rollback engine must halt if the structural link back to an ancestral layout is completely missing");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFalse_WhenParentScreenCannotBeResolvedFromStorageEngine()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        Guid missingParentId = Guid.NewGuid();

        FakeTestingScreen errorOverlayScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(FakeTestingScreen),
            SessionId = targetSessionId,
            ParentScreenId = missingParentId
        };

        _contextMock
            .SetupGet(c => c.SessionId)
            .Returns(targetSessionId);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorOverlayScreen);

        _sessionRepositoryMock
            .Setup(r => r.GetScreenByIdAsync(targetSessionId, missingParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TerminalScreen?)null); // Simulated cache eviction or storage failure for parent layout

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeFalse("because the command core cannot complete the rollback if the historical parent layout sequence data is unretrievable");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRestoreAndSaveParentScreen_WhenValidParentLinkageIsSuccessfullyResolved()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        Guid targetParentId = Guid.NewGuid();

        FakeTestingScreen structuralChildScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(FakeTestingScreen),
            SessionId = targetSessionId,
            ParentScreenId = targetParentId
        };

        FakeTestingScreen structuralParentScreen = new()
        {
            Id = targetParentId,
            Name = nameof(FakeTestingScreen),
            SessionId = targetSessionId
        };

        _contextMock
            .SetupGet(c => c.SessionId)
            .Returns(targetSessionId);

        _sessionRepositoryMock
            .Setup(r => r.GetActiveScreenAsync(targetSessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(structuralChildScreen);

        _sessionRepositoryMock
            .Setup(r => r.GetScreenByIdAsync(targetSessionId, targetParentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(structuralParentScreen);

        // Act
        bool result = await _sut.ExecuteAsync(_contextMock.Object);

        // Assert
        result
            .Should()
            .BeTrue("because identifying and downloading a historically accurate parent matrix frame completes the presentation roll-back workflow safely");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<TerminalScreen>(s => s.Id == targetParentId),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because the active execution pointer within the persistence cluster layer must map back onto the historical ancestral screen view state");
    }
}
