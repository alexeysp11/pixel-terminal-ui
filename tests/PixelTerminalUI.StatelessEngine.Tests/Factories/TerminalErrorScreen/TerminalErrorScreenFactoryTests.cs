using FluentAssertions;
using PixelTerminalUI.StatelessEngine.Commands.DismissError;
using PixelTerminalUI.StatelessEngine.Factories.TerminalErrorScreen;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Tests.Factories.TerminalErrorScreen;

public sealed class TerminalErrorScreenFactoryTests
{
    private readonly TerminalErrorScreenFactory _sut = new();

    [Fact]
    public void BuildErrorScreen_WhenInvokedWithValidParameters_ShouldConstructCompleteUniformErrorLayoutStructure()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        string targetErrorMessage = "Hardware Scanner Communication Timeout Event!";

        SimpleMessageScreen parentScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ActiveInventoryScreenLayout",
            Width = 80,
            Height = 25,
            Widgets = []
        };

        // Act
        SimpleMessageScreen errorScreen = _sut.BuildErrorScreen(sessionId, parentScreen, targetErrorMessage);

        // Assert: Verify structural layout properties metadata and inheritance integrity constraints
        errorScreen
            .Should()
            .NotBeNull();

        errorScreen.Name
            .Should()
            .Be("ErrorNotificationView");

        errorScreen.SessionId
            .Should()
            .Be(sessionId);

        errorScreen.ParentScreenId
            .Should()
            .Be(parentScreen.Id);

        errorScreen.Width
            .Should()
            .Be(parentScreen.Width);

        errorScreen.Height
            .Should()
            .Be(parentScreen.Height);
    }

    [Fact]
    public void BuildErrorScreen_WhenInvoked_ShouldPopulateCorrectPolymorphicWidgetsWithDismissCommandAttached()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        string targetErrorMessage = "Database Constraint Fault";

        SimpleMessageScreen parentScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "DataEntryScreen",
            Width = 40,
            Height = 10,
            Widgets = []
        };

        // Act
        SimpleMessageScreen errorScreen = _sut.BuildErrorScreen(sessionId, parentScreen, targetErrorMessage);
        List<TextWidget> widgetList = errorScreen.Widgets.ToList();

        // Assert: Verify individual components definitions and embedded command objects bindings
        widgetList
            .Should()
            .HaveCount(2, "because a uniform error viewport modal must comprise exactly one text display label and one acknowledgment input receiver");

        TextWidget? errorLabel = widgetList.FirstOrDefault(w => w.Name == "ErrorMessageLabel");
        errorLabel
            .Should()
            .NotBeNull()
            .And.BeOfType<TextWidget>();

        errorLabel!.Value
            .Should()
            .Be(targetErrorMessage);

        errorLabel.Foreground
            .Should()
            .Be(ConsoleColor.Red);

        TextEntryWidget? escapeInput = widgetList.FirstOrDefault(w => w.Name == "ErrorAcknowledgeInput") as TextEntryWidget;
        escapeInput
            .Should()
            .NotBeNull();

        escapeInput!.Command
            .Should()
            .NotBeNull()
            .And.BeOfType<DismissErrorCommand>();

        escapeInput.Command!.WidgetId
            .Should()
            .Be(escapeInput.Id);

        errorScreen.FocusedEntryWidgetId
            .Should()
            .Be(escapeInput.Id);
    }

    [Fact]
    public void BuildErrorScreen_WhenErrorMessageIsNull_ShouldGracefullyFallbackToDefaultSystemFaultStringText()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();

        SimpleMessageScreen parentScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "AnyWorkingLayoutView",
            Width = 30,
            Height = 8,
            Widgets = []
        };

        // Act
        SimpleMessageScreen errorScreen = _sut.BuildErrorScreen(sessionId, parentScreen, null!);
        TextWidget? errorLabel = errorScreen.Widgets.FirstOrDefault(w => w.Name == "ErrorMessageLabel");

        // Assert: Verify defensive null parameters handling and safe processing fallbacks
        errorLabel
            .Should()
            .NotBeNull("because missing parameter inputs strings must never cause the factory pipeline execution loops to crash");

        errorLabel!.Value
            .Should()
            .Be("Unknown Verification Fault!", "the system framework layer must inject a recognizable placeholder failure string descriptive token when raw message arguments map onto null variables");
    }
}
