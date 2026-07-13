using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PixelTerminalUI.Engine.Factories.StartupScreen;
using PixelTerminalUI.Engine.Screens;

namespace PixelTerminalUI.Engine.Tests.Factories.StartupScreen;

public sealed class StartupScreenFactoryTests
{
    [Fact]
    public void CreateScreen_WhenInvokedWithValidParameters_ShouldActivateAndPopulateTargetScreenStructure()
    {
        // Arrange
        Mock<ILogger<StartupScreenFactory>> loggerMock = new();
        Guid sessionId = Guid.NewGuid();
        Type targetType = typeof(SimpleMessageScreen);

        SimpleMessageScreen instantiatedScreen = new()
        {
            Id = Guid.Empty,
            SessionId = Guid.Empty,
            Name = "InitialBlankBlueprint",
            Widgets = []
        };

        Func<Type, TerminalScreen> mockActivator = type => instantiatedScreen;

        StartupScreenFactory sut = new(loggerMock.Object, targetType, mockActivator);

        // Act
        TerminalScreen result = sut.CreateScreen(sessionId);

        // Assert
        result
            .Should()
            .NotBeNull()
            .And.BeSameAs(instantiatedScreen, "because the factory must utilize the exact screen instance supplied by the activator delegate");
        result.Id
            .Should()
            .NotBeEmpty();
        result.SessionId
            .Should()
            .Be(sessionId);
        result.Visible
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Constructor_WhenInvalidTypeIsSupplied_ShouldThrowArgumentException()
    {
        // Arrange
        Mock<ILogger<StartupScreenFactory>> loggerMock = new();
        Type invalidScreenType = typeof(string); // string is completely unrelated to TerminalScreen
        Func<Type, TerminalScreen> dummyActivator = type => null!;

        // Act
        Action act = () => new StartupScreenFactory(loggerMock.Object, invalidScreenType, dummyActivator);

        // Assert
        act
            .Should()
            .Throw<ArgumentException>("because configuration requires types inheriting from TerminalScreen")
            .WithParameterName("screenType", "because the verification targets the invalid screenType parameter");
    }
}

