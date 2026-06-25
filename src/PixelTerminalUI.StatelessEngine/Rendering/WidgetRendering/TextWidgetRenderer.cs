using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.StatelessEngine.Rendering.Core;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Rendering.WidgetRendering;

public sealed class TextWidgetRenderer : IWidgetRenderer
{
    public Type SupportedWidgetType => typeof(TextWidget);

    public void Draw(Pixel[] buffer, TextWidget widget, Guid? focusedId, int width, int height)
        => StatelessRenderer.DrawDefaultText(buffer, widget, width, height);
}
