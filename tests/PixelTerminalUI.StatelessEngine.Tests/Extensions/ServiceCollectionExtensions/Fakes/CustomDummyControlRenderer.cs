using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.StatelessEngine.Rendering.WidgetRendering;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.StatelessEngine.Tests.Extensions.ServiceCollectionExtensions.Fakes;

/// <summary>
/// A dummy custom renderer created strictly to verify plugin extension registration behavior.
/// </summary>
public sealed class CustomDummyWidgetRenderer : IWidgetRenderer
{
    public Type SupportedWidgetType => typeof(CustomDummyWidget);

    public void Draw(Pixel[] buffer, TextWidget widget, Guid? focusedId, int width, int height)
    {
        // Test stub implementation, no drawing logic required for DI verification
    }
}
