using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.SymbolHandling;

/// <summary>
/// Provides the default implementation for evaluating, routing, and intercepting systemic 
/// operational console inputs, macro shortcuts, and navigational control tokens.
/// </summary>
public sealed class SpecialSymbolHandler : ISpecialSymbolHandler
{
    /// <summary>
    /// Gets or sets an optional custom asynchronous delegate to intercept specialized game or domain specific control sequences 
    /// before the core engine executes standard terminal routing operations.
    /// </summary>
    public Func<TerminalScreen, string, ValueTask<SymbolHandlingResult>>? CustomInterceptor { get; set; }

    /// <inheritdoc/>
    public async ValueTask<SymbolHandlingResult> HandleSymbolAsync(TerminalScreen screen, string userInput)
    {
        // Execute the custom app-level interceptor if registered
        if (CustomInterceptor is not null)
        {
            SymbolHandlingResult customResult = await CustomInterceptor(screen, userInput);
            if (customResult.Action != SymbolResultActionType.NotHandled)
            {
                return customResult;
            }
        }

        // Intercept global application termination request
        if (userInput == "-q")
        {
            return SymbolHandlingResult.TerminateSession("Session terminated by user request.");
        }

        // Intercept explicit cancel command to unconditionally pop screen stack layer
        if (userInput == "-x" && screen.ParentScreenId.HasValue)
        {
            return new SymbolHandlingResult { Action = SymbolResultActionType.NavigateToParentScreen };
        }

        // Intercept active widget data reset command
        if (userInput == "-r")
        {
            if (screen.FocusedEntryWidgetId.HasValue)
            {
                TextWidget? focusedWidget = screen.Widgets.FirstOrDefault(c => c.Id == screen.FocusedEntryWidgetId.Value);
                if (focusedWidget is TextEntryWidget entryWidget)
                {
                    entryWidget.Value = string.Empty;
                }
            }
            return SymbolHandlingResult.StayOnScreen();
        }

        // Intercept standard or boundary backward navigation commands
        if (userInput == "-b")
        {
            if (screen.ParentScreenId.HasValue && screen.FocusedEntryWidgetId.HasValue)
            {
                List<TextWidget> sortedWidgets = GetSortedEditableWidgets(screen);

                // If we are at the very first element of the screen, -b pops the screen back to parent screen
                if (sortedWidgets.Count > 0 && sortedWidgets.First().Id == screen.FocusedEntryWidgetId.Value)
                {
                    return new SymbolHandlingResult { Action = SymbolResultActionType.NavigateToParentScreen };
                }
            }

            // Normal fallback: just shift focus one step backward inside the current active viewport layout
            return SymbolHandlingResult.ShiftFocusBackward();
        }

        // Intercept navigation forward command (explicit or implied via empty enter string)
        if (userInput == "-n" || string.IsNullOrEmpty(userInput))
        {
            if (screen.FocusedEntryWidgetId.HasValue)
            {
                TextWidget? focusedWidget = screen.Widgets.FirstOrDefault(c => c.Id == screen.FocusedEntryWidgetId.Value);

                if (focusedWidget is TextEntryWidget entryWidget)
                {
                    // If a business command state machine is attached to this input field,
                    // we must NOT intercept the execution via a blind focus shift forward action.
                    // Bypass interception to let the pipeline handle the command invocation below.
                    if (entryWidget.Command is not null)
                    {
                        return SymbolHandlingResult.NotHandled();
                    }

                    if (entryWidget.Required && string.IsNullOrEmpty(entryWidget.Value))
                    {
                        return SymbolHandlingResult.StayOnScreen();
                    }
                }
            }
            return SymbolHandlingResult.ShiftFocusForward();
        }

        return SymbolHandlingResult.NotHandled();
    }

    private static List<TextWidget> GetSortedEditableWidgets(TerminalScreen screen)
    {
        List<TextWidget> editableWidgets = [];

        // Filter out visible inputs using single-pass iteration to protect performance
        foreach (TextWidget widget in screen.Widgets)
        {
            if (widget.Visible && (widget is TextEntryWidget || widget is PasswordEntryWidget))
            {
                editableWidgets.Add(widget);
            }
        }

        // Sort execution mapping matrix by TabIndex, then Top rows, then Left columns
        editableWidgets.Sort((TextWidget left, TextWidget right) =>
        {
            int leftTab = left.TabIndex ?? int.MaxValue;
            int rightTab = right.TabIndex ?? int.MaxValue;
            int tabCompare = leftTab.CompareTo(rightTab);
            if (tabCompare != 0)
            {
                return tabCompare;
            }

            int topCompare = left.Top.CompareTo(right.Top);
            if (topCompare != 0)
            {
                return topCompare;
            }

            return left.Left.CompareTo(right.Left);
        });

        return editableWidgets;
    }
}
