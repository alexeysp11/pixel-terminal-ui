using PixelTerminalUI.StatelessEngine.Commands.DismissError;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Factories.TerminalErrorScreen;

/// <summary>
/// Implements a standalone error screen factory to abstract away layout geometries constructions from the execution pipeline boundaries.
/// </summary>
public sealed class TerminalErrorScreenFactory : ITerminalErrorScreenFactory
{
    /// <inheritdoc/>
    public SimpleMessageScreen BuildErrorScreen(Guid sessionId, TerminalScreen parentScreen, string errorMessage)
    {
        if (parentScreen == null)
        {
            throw new ArgumentNullException(nameof(parentScreen));
        }

        Guid inputWidgetId = Guid.NewGuid();
        DismissErrorCommand dismissCommand = new()
        {
            WidgetId = inputWidgetId
        };

        TextWidget errorLabel = new()
        {
            Id = Guid.NewGuid(),
            Name = "ErrorMessageLabel",
            Left = 1,
            Top = 1,
            Width = parentScreen.Width - 2,
            Value = errorMessage ?? "Unknown Verification Fault!",
            Visible = true,
            Foreground = ConsoleColor.Red
        };

        TextEntryWidget escapeInput = new()
        {
            Id = inputWidgetId,
            Name = "ErrorAcknowledgeInput",
            Left = 1,
            Top = parentScreen.Height - 2,
            Width = 5,
            Required = true,
            EmptyEnterSymbol = '.',
            Hint = "PRESS ENTER TO RETURN",
            Visible = true,
            Command = dismissCommand,
            Value = string.Empty,
            TabIndex = 1
        };

        SimpleMessageScreen errorScreen = new()
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = "ErrorNotificationView",
            Width = parentScreen.Width,
            Height = parentScreen.Height,
            Visible = true,
            ParentScreenId = parentScreen.Id,
            Widgets = [errorLabel, escapeInput],
            FocusedEntryWidgetId = inputWidgetId
        };

        return errorScreen;
    }
}
