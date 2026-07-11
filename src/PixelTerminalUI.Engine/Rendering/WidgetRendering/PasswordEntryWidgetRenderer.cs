using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Rendering.Core;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Rendering.WidgetRendering;

public sealed class PasswordEntryWidgetRenderer : IWidgetRenderer
{
    public Type SupportedWidgetType => typeof(PasswordEntryWidget);

    public void Draw(Pixel[] buffer, TextWidget widget, Guid? focusedId, int width, int height)
    {
        // Use pattern matching to handle type mismatches gracefully by falling back to default text layout routines
        if (widget is not PasswordEntryWidget pwdWidget)
        {
            StatelessRenderer.DrawDefaultText(buffer, widget, width, height);
            return;
        }

        // Guard check for the vertical coordinate to prevent top/bottom memory out-of-bounds crashes
        int widgetTop = pwdWidget.Top;
        if (widgetTop < 0 || widgetTop >= height)
        {
            return;
        }

        bool isFocused = pwdWidget.Id == focusedId;
        string actualValue = pwdWidget.Value ?? string.Empty;
        int valueLength = actualValue.Length;

        int widgetWidth = pwdWidget.Width;
        int widgetLeft = pwdWidget.Left;

        for (int i = 0; i < widgetWidth; i++)
        {
            int targetX = widgetLeft + i;

            // Skip rendering characters that fall off the left screen margin edge
            if (targetX < 0)
            {
                continue;
            }

            // Stop rendering immediately as we passed the rightmost hardware buffer limit
            if (targetX >= width)
            {
                break;
            }

            // Evaluate mask symbols dynamically inline per iteration to eliminate redundant heap string garbage creation
            char symbol = i < valueLength ? pwdWidget.MaskChar : isFocused ? pwdWidget.EmptyEnterSymbol : ' ';

            // Map the layout using the strict flat continuous memory block indexing schema
            buffer[widgetTop * width + targetX] = new Pixel(
                symbol,
                pwdWidget.Inverted,
                pwdWidget.Foreground,
                pwdWidget.Background);
        }
    }
}
