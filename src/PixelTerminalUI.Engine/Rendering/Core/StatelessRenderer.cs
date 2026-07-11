using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Rendering.Registries;
using PixelTerminalUI.Engine.Rendering.WidgetRendering;
using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Rendering.Core;

/// <summary>
/// Implements the rendering engine responsible for transforming a declarative screen layout 
/// into a raw, character pixel array.
/// </summary>
/// <param name="registry">The operational centralized index matching explicit UI elements to their target visual layout engines.</param>
public sealed class StatelessRenderer(IWidgetRendererRegistry registry) : IStatelessRenderer
{
    /// <summary>
    /// Evaluates the screen layout and draws layout tokens directly into the provided memory buffer.
    /// </summary>
    /// <param name="screen">The stateful screen abstraction containing structural UI component hierarchies.</param>
    /// <param name="buffer">The flat destination array allocated or rented by the caller to store screen state.</param>
    public void Draw(TerminalScreen screen, Pixel[] buffer)
    {
        int width = screen.Width;
        int height = screen.Height;

        // Initialize screen with empty default space padding pixels using a flat array offset formula
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                buffer[y * width + x] = new Pixel(' ', false, ConsoleColor.White);
            }
        }

        // Render all visible elements using the specific widget registry routing loops
        foreach (TextWidget widget in screen.Widgets)
        {
            if (!widget.Visible)
            {
                continue;
            }

            IWidgetRenderer? renderer = registry.GetRenderer(widget.GetType());
            if (renderer != null)
            {
                renderer.Draw(buffer, widget, screen.FocusedEntryWidgetId, width, height);
            }
            else
            {
                DrawDefaultText(buffer, widget, width, height);
            }
        }

        // Centralized Backend-Driven Hint status bar rendering
        // If there is an active focus widget, find it and project its UPPERCASE hint to the bottom of the screen
        if (screen.FocusedEntryWidgetId.HasValue)
        {
            TextWidget? focusedWidget = screen.Widgets.FirstOrDefault(c => c.Id == screen.FocusedEntryWidgetId.Value);

            if (focusedWidget is TextEntryWidget entryWidget && !string.IsNullOrEmpty(entryWidget.Hint))
            {
                // Position the hint on the very last line of the screen viewport boundaries
                int hintY = height - 1;
                int hintLength = entryWidget.Hint.Length;

                // Center alignment calculation inside the screen Width bounds
                int startX = (width - hintLength) / 2;

                for (int i = 0; i < hintLength; i++)
                {
                    int targetX = startX + i;

                    // Safety boundaries clipping wrap checks
                    if (targetX >= 0 && targetX < width && hintY >= 0 && hintY < height)
                    {
                        buffer[hintY * width + targetX] = new Pixel(entryWidget.Hint[i], false, ConsoleColor.Gray);
                    }
                }
            }
        }
    }

    public static void DrawDefaultText(Pixel[] buffer, TextWidget widget, int width, int height)
    {
        if (string.IsNullOrEmpty(widget.Value))
        {
            return;
        }

        // Check the base bounds to avoid going outside the array bounds
        if (widget.Top < 0 || widget.Top >= height || widget.Left < 0 || widget.Left >= width)
        {
            return;
        }

        int currentX = widget.Left;
        int currentY = widget.Top;

        // Determine the available length taking into account the width of the widget and the screen borders
        int maxWidgetWidth = widget.Width > 0 ? widget.Width : width - widget.Left;
        int maxDrawableLength = Math.Min(maxWidgetWidth, width - widget.Left);

        for (int i = 0; i < widget.Value.Length && i < maxDrawableLength; i++)
        {
            int targetX = currentX + i;

            // Map the coordinates directly into the flat buffer layout array
            buffer[currentY * width + targetX] = new Pixel(
                widget.Value[i],
                widget.Inverted,
                widget.Foreground,
                widget.Background);
        }
    }
}
