using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Rendering.Core;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Rendering.WidgetRendering;

public sealed class TextWidgetRenderer : IWidgetRenderer
{
    public Type SupportedWidgetType => typeof(TextWidget);

    public void Draw(Pixel[] buffer, TextWidget widget, Guid? focusedId, int width, int height)
        => StatelessRenderer.DrawDefaultText(buffer, widget, width, height);
}
