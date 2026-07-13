using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Rendering.WidgetRendering;

/// <summary>
/// Defines a specialized drawing processor dedicated to rendering a specific type of UI widget 
/// onto the global two-dimensional pixel canvas.
/// </summary>
public interface IWidgetRenderer
{
    /// <summary>
    /// Gets the explicit metadata runtime type of the widget supported by this renderer implementation.
    /// </summary>
    Type SupportedWidgetType { get; }

    /// <summary>
    /// Transforms and maps the specified widget layout onto the absolute coordinate system of the text pixel buffer.
    /// </summary>
    /// <param name="buffer">The shared two-dimensional matrix representing the frame rendering target.</param>
    /// <param name="widget">The target widget state instance to draw.</param>
    /// <param name="focusedId">The identifier of the widget currently holding user focus, used to apply active visual state styles.</param>
    void Draw(Pixel[] buffer, TextWidget widget, Guid? focusedId, int width, int height);
}
