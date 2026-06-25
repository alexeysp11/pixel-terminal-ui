using FluentAssertions;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.SymbolHandling;
using PixelTerminalUI.StatelessEngine.Tests.SymbolHandling.Fakes;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Tests.SymbolHandling;

public sealed class SpecialSymbolHandlerTests
{
    [Fact]
    public void HandleSymbol_WithQuitCommand_ShouldReturnTerminateSessionAction()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        SimpleMessageScreen screen = new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Name = "SimpleMessage", Widgets = [] };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "-q");

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.TerminateSession, "because the quit special token must instantly signal container pipeline termination loops");
        result.CustomMessage
            .Should()
            .Be("Session terminated by user request.");
    }

    [Fact]
    public void HandleSymbol_WithResetCommand_ShouldWipeActiveWidgetValueAndStayOnScreen()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        Guid focusedId = Guid.NewGuid();

        TextEntryWidget activeWidget = new()
        {
            Id = focusedId,
            Name = "Test",
            Value = "PREVIOUS-STALE-DATA",
            Visible = true
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = focusedId,
            Widgets = [activeWidget]
        };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "-r");

        // Assert
        activeWidget.Value
            .Should()
            .BeEmpty("because the reset specifier token must flush targeted interactive field strings clean");

        result.Action
            .Should()
            .Be(SymbolResultActionType.StayOnScreen, "because scrubbing field values resets the local state machine context without changing focus positions");
    }

    [Fact]
    public void HandleSymbol_WithBackwardCommand_ShouldReturnShiftFocusBackwardAction()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        SimpleMessageScreen screen = new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Name = "SimpleMessage", Widgets = [] };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "-b");

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.ShiftFocusBackward, "because the backward token signals layout tracking pointer updates to previous edit items");
    }

    [Fact]
    public void HandleSymbol_WithRequiredEmptyInput_ShouldBlockTransitAndStayOnScreen()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        Guid focusedId = Guid.NewGuid();

        TextEntryWidget mandatoryWidget = new()
        {
            Id = focusedId,
            Name = "Test",
            Value = string.Empty, // Intentionally empty mandatory field
            Required = true,
            Visible = true
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = focusedId,
            Widgets = [mandatoryWidget]
        };

        // Act: Simulating user sending empty enter string over the wire
        SymbolHandlingResult result = handler.HandleSymbol(screen, string.Empty);

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.StayOnScreen, "because required fields are protected from forwarding actions if their contents evaluate to empty buffers");
    }

    [Fact]
    public void HandleSymbol_WithStandardInput_ShouldReturnNotHandledAction()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        SimpleMessageScreen screen = new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Name = "SimpleMessage", Widgets = [] };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "123456789");

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.NotHandled, "because normal scanning inputs are barcode payloads destined for core command processors logic");
    }

    [Fact]
    public void HandleSymbol_WithExplicitForwardCommand_ShouldReturnShiftFocusForwardAction()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        SimpleMessageScreen screen = new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Name = "SimpleMessage", Widgets = [] };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "-n");

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.ShiftFocusForward, "because the explicit forward specifier token must advance the focus pointer inside the viewport layout");
    }

    [Fact]
    public void HandleSymbol_WithRequiredFieldHavingValue_ShouldAllowTransitAndShiftFocusForward()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        Guid focusedId = Guid.NewGuid();

        TextEntryWidget mandatoryWidgetWithValue = new()
        {
            Id = focusedId,
            Name = "TextEdit",
            Value = "ValidBarcode123", // Field is required but already contains valid structural string data
            Required = true,
            Visible = true
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = focusedId,
            Widgets = [mandatoryWidgetWithValue]
        };

        // Act: Simulating user sending empty enter string over the wire to advance
        SymbolHandlingResult result = handler.HandleSymbol(screen, string.Empty);

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.ShiftFocusForward, "because empty inputs on required fields are perfectly acceptable if the underlying element buffer already holds payload variables");
    }

    [Fact]
    public void HandleSymbol_WhenScreenHasNoActiveFocusedWidgetId_ShouldFallbackToSafeNotHandledAction()
    {
        // Arrange
        SpecialSymbolHandler handler = new();

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "SimpleMessage",
            FocusedEntryWidgetId = null, // No focus active on screen
            Widgets = []
        };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "-r");

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.StayOnScreen, "because reset tokens targeting a screen with no focused elements cannot mutate anything but should still freeze the frame");
    }

    [Fact]
    public void HandleSymbol_WithBackwardCommand_WhenAtMiddleWidget_ShouldReturnShiftFocusBackward()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        Guid firstFieldId = Guid.NewGuid();
        Guid secondFieldId = Guid.NewGuid();

        TextEntryWidget firstWidget = new() { Id = firstFieldId, Name = "First", Value = string.Empty, Top = 1, Visible = true };
        TextEntryWidget secondWidget = new() { Id = secondFieldId, Name = "Second", Value = string.Empty, Top = 2, Visible = true };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            Name = "SimpleMessage",
            SessionId = Guid.NewGuid(),
            ParentScreenId = Guid.NewGuid(), // Has a parent screen screen stack layer
            FocusedEntryWidgetId = secondFieldId, // Currently focused on the SECOND element
            Widgets = [firstWidget, secondWidget]
        };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "-b");

        // Assert: It should stay within the screen and move focus to widget A
        result.Action
            .Should()
            .Be(SymbolResultActionType.ShiftFocusBackward, "because the backward command must navigate internally if the focus pointer is not resting on the first index item");
    }

    [Fact]
    public void HandleSymbol_WithBackwardCommand_WhenAtFirstWidget_ShouldReturnNavigateToParentScreen()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        Guid firstFieldId = Guid.NewGuid();
        Guid secondFieldId = Guid.NewGuid();
        Guid parentId = Guid.NewGuid();

        TextEntryWidget firstWidget = new() { Id = firstFieldId, Name = "First", Value = string.Empty, Top = 1, Visible = true };
        TextEntryWidget secondWidget = new() { Id = secondFieldId, Name = "Second", Value = string.Empty, Top = 2, Visible = true };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            Name = "SimpleMessage",
            SessionId = Guid.NewGuid(),
            ParentScreenId = parentId,
            FocusedEntryWidgetId = firstFieldId, // Focus is at the VERY FIRST element of the layout matrix grid
            Widgets = [firstWidget, secondWidget]
        };

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, "-b");

        // Assert: It should trigger a full pop action back onto the parent screen layout context
        result.Action
            .Should()
            .Be(SymbolResultActionType.NavigateToParentScreen, "because triggering a reverse navigation shift at index zero must step backwards out into the parent window graph structure");
    }

    [Fact]
    public void HandleSymbol_ShouldReturnNotHandled_WhenInputFieldHasAnAttachedCommand()
    {
        // Arrange
        SpecialSymbolHandler handler = new();
        Guid editWidgetId = Guid.NewGuid();
        StubCommand stubCommand = new();

        TextEntryWidget widgetWithCommand = new()
        {
            Id = editWidgetId,
            Name = "ConnectionTriggerInput",
            Value = string.Empty,
            Visible = true,
            Required = false,
            Command = stubCommand
        };

        SimpleMessageScreen screen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "WelcomeScreen",
            Width = 40,
            Height = 12,
            Visible = true,
            FocusedEntryWidgetId = editWidgetId,
            Widgets = [widgetWithCommand]
        };

        string emptyUserInput = string.Empty;

        // Act
        SymbolHandlingResult result = handler.HandleSymbol(screen, emptyUserInput);

        // Assert
        result.Action
            .Should()
            .Be(SymbolResultActionType.NotHandled,
                "because an empty enter stroke on a widget that encapsulates a business command must bypass global navigation focus shift handlers");
    }
}
