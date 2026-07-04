using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.SymbolHandling;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Infrastructure.Interceptors;
using TheLostGrid.Server.Scenarios.Help;
using TheLostGrid.Server.Scenarios.SectorNavigation;

namespace TheLostGrid.Server.Tests.Infrastructure.Interceptors;

public sealed class GameplayInputInterceptorTests
{
    private readonly Fixture _fixture = new();
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
    private readonly Mock<IServiceScope> _scopeMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ITerminalSessionRepository> _sessionRepositoryMock = new();
    private readonly GameplayInputInterceptor _sut;

    public GameplayInputInterceptorTests()
    {
        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(_scopeMock.Object);

        _scopeMock
            .SetupGet(s => s.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        _serviceProviderMock
            .Setup(p => p.GetService(typeof(ITerminalSessionRepository)))
            .Returns(_sessionRepositoryMock.Object);

        _sut = new(_scopeFactoryMock.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenScopeFactoryIsNull()
    {
        // Act
        Action act = () => new GameplayInputInterceptor(null!);

        // Assert
        act
            .Should()
            .Throw<ArgumentNullException>("because an operational interceptor requires a valid factory abstraction instance to manage runtime lifetime segments safely");
    }

    [Fact]
    public async Task InterceptSymbolsAsync_ShouldThrowArgumentNullException_WhenScreenParameterIsNull()
    {
        // Act
        Func<Task> act = async () => await _sut.InterceptSymbolsAsync(null!, "-h");

        // Assert
        await act
            .Should()
            .ThrowAsync<ArgumentNullException>("because symbol filtering logic cannot evaluate conditions without an operational viewport container mapping reference");
    }

    [Fact]
    public async Task InterceptSymbolsAsync_ShouldSaveHelpScreenAndReturnRefreshResult_WhenInputIsHelpShortcut()
    {
        // Arrange
        Guid targetSessionId = Guid.NewGuid();
        Guid targetScreenId = Guid.NewGuid();

        SectorNavigationScreen currentScreen = new(CharacterType.Hacker, energy: 100, credits: 50)
        {
            Id = targetScreenId,
            Name = nameof(SectorNavigationScreen),
            SessionId = targetSessionId
        };

        // Act
        SymbolHandlingResult result = await _sut.InterceptSymbolsAsync(currentScreen, "-h");

        // Assert
        result
            .Should()
            .NotBeNull("because the input processing engine must always hand back a non-null handling decision descriptor");

        result.Action
            .Should()
            .Be(SymbolResultActionType.RefreshActiveScreen, "because providing the explicitly bound help shortcut token sequence must trigger defensive interface overrides");

        _scopeFactoryMock
            .Verify(f => f.CreateScope(),
                Times.Once,
                "because capturing context dependencies dynamically must process within an isolated short-lived service collection lifetime branch");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(
                targetSessionId,
                It.Is<HelpScreen>(s =>
                    s.ParentScreenId == targetScreenId &&
                    s.SessionId == targetSessionId &&
                    s.Name == nameof(HelpScreen)),
                It.IsAny<CancellationToken>()),
                Times.Once,
                "because the interceptor layer must push the active presentation matrix update down to the distributed repository cache seamlessly");
    }

    [Fact]
    public async Task InterceptSymbolsAsync_ShouldReturnNotHandledResult_WhenInputDoesNotMatchAnySpecialMacroTokens()
    {
        // Arrange
        SectorNavigationScreen currentScreen = new(CharacterType.Hacker, energy: 100, credits: 50)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = Guid.NewGuid()
        };

        // Act
        SymbolHandlingResult result = await _sut.InterceptSymbolsAsync(currentScreen, "standard_gameplay_command_input");

        // Assert
        result
            .Should()
            .NotBeNull("because the input processing engine must always hand back a non-null handling decision descriptor")
            .And.Match<SymbolHandlingResult>(r => r.Action == SymbolResultActionType.NotHandled, "because generic or standard business command values must pass through unhindered to fulfill deeper downstream domain pipeline evaluations");

        _scopeFactoryMock
            .Verify(f => f.CreateScope(),
                Times.Never,
                "because resource-intensive dynamic lookup scope frames must stay dormant unless a targeted infrastructural interaction signature fires");

        _sessionRepositoryMock
            .Verify(r => r.SaveActiveScreenAsync(It.IsAny<Guid>(), It.IsAny<TerminalScreen>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "because standard operational inputs must not alter the layout sequence or snapshot states prematurely");
    }
}
