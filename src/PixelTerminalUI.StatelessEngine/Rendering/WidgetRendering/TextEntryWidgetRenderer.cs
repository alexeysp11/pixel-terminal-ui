using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.StatelessEngine.Rendering.Core;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Rendering.WidgetRendering;

public sealed class TextEntryWidgetRenderer : IWidgetRenderer
{
    public Type SupportedWidgetType => typeof(TextEntryWidget);

    public void Draw(Pixel[] buffer, TextWidget widget, Guid? focusedId, int width, int height)
    {
        // Use pattern matching to handle type mismatches gracefully by falling back to default text layout routines
        if (widget is not TextEntryWidget entryWidget)
        {
            StatelessRenderer.DrawDefaultText(buffer, widget, width, height);
            return;
        }

        // Guard check for the vertical coordinate to prevent top/bottom memory out-of-bounds crashes
        if (entryWidget.Top < 0 || entryWidget.Top >= height)
        {
            return;
        }

        bool isFocused = entryWidget.Id == focusedId;

        // Text edit widget now ONLY draws its actual value or spaces/dots
        // Hint fallback logic is completely removed from the field itself
        string textToDraw = entryWidget.Value ?? string.Empty;
        int widgetWidth = entryWidget.Width;
        int widgetTop = entryWidget.Top;
        int widgetLeft = entryWidget.Left;

        for (int i = 0; i < widgetWidth; i++)
        {
            int targetX = widgetLeft + i;

            // Comprehensive horizontal clipping guards
            if (targetX < 0)
            {
                continue;
            }

            if (targetX >= width)
            {
                break;
            }

            char symbol = i < textToDraw.Length ? textToDraw[i] : isFocused ? entryWidget.EmptyEnterSymbol : ' ';

            // Map the layout using the strict flat continuous memory block indexing schema
            buffer[widgetTop * width + targetX] = new Pixel(
                symbol,
                entryWidget.Inverted,
                entryWidget.Foreground,
                entryWidget.Background);
        }
    }
}
