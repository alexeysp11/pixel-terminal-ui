using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Navigation;

/// <summary>
/// Manages tab-index routing and circular input widget navigation on character-based screens.
/// </summary>
public sealed class FocusManager : IFocusManager
{
    /// <inheritdoc />
    /// <remarks>
    /// Implements a circular focus loop. If the active focus pointer reaches the bounds of 
    /// the sorted widget hierarchy, it gracefully rolls over back to the initial element.
    /// </remarks>
    public Guid? GetNextFocus(TerminalScreen screen)
    {
        List<TextWidget> editableWidgets = GetSortedEditableWidgets(screen);
        if (editableWidgets.Count == 0)
        {
            return null;
        }

        if (!screen.FocusedEntryWidgetId.HasValue)
        {
            return editableWidgets.First().Id; // Fallback to first field if focus loop was lost
        }

        int currentIndex = editableWidgets.FindIndex(c => c.Id == screen.FocusedEntryWidgetId.Value);
        if (currentIndex == -1 || currentIndex == editableWidgets.Count - 1)
        {
            return editableWidgets.First().Id; // Circular navigation shift: loop back to the top element
        }

        return editableWidgets[currentIndex + 1].Id;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Implements a reverse circular focus loop. Rolling backward past the first widget 
    /// immediately shifts the focus pointer to the very last element in the sequence.
    /// </remarks>
    public Guid? GetPreviousFocus(TerminalScreen screen)
    {
        List<TextWidget> editableWidgets = GetSortedEditableWidgets(screen);
        if (editableWidgets.Count == 0)
        {
            return null;
        }

        if (!screen.FocusedEntryWidgetId.HasValue)
        {
            return editableWidgets.Last().Id;
        }

        int currentIndex = editableWidgets.FindIndex(c => c.Id == screen.FocusedEntryWidgetId.Value);
        if (currentIndex == -1 || currentIndex == 0)
        {
            return editableWidgets.Last().Id; // Circular navigation shift: loop backward to the last element
        }

        return editableWidgets[currentIndex - 1].Id;
    }

    private static List<TextWidget> GetSortedEditableWidgets(TerminalScreen screen)
    {
        List<TextWidget> editableWidgets = [];

        // Single-pass filtering logic instead of multiple allocations inside LINQ pipelines
        foreach (TextWidget widget in screen.Widgets)
        {
            if (widget.Visible && (widget is TextEntryWidget || widget is PasswordEntryWidget))
            {
                editableWidgets.Add(widget);
            }
        }

        // Sort the filtered list in-place using a highly optimized comparison primitive algorithm
        editableWidgets.Sort((TextWidget left, TextWidget right) =>
        {
            // Compare by TabIndex priority first
            int leftTab = left.TabIndex ?? int.MaxValue;
            int rightTab = right.TabIndex ?? int.MaxValue;
            int tabCompare = leftTab.CompareTo(rightTab);
            if (tabCompare != 0)
            {
                return tabCompare;
            }

            // Fallback to geometric Top coordinate rows sorting
            int topCompare = left.Top.CompareTo(right.Top);
            if (topCompare != 0)
            {
                return topCompare;
            }

            // Fallback to geometric Left coordinate columns sorting
            return left.Left.CompareTo(right.Left);
        });

        return editableWidgets;
    }
}
