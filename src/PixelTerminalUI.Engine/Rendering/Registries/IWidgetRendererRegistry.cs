using PixelTerminalUI.Engine.Rendering.WidgetRendering;

namespace PixelTerminalUI.Engine.Rendering.Registries;

/// <summary>
/// Maintains a central thread-safe lookup directory structure mapping strongly typed layout widgets onto specialized display rendering strategies.
/// </summary>
public interface IWidgetRendererRegistry
{
    /// <summary>
    /// Resolves the concrete rendering component execution instance matching the structural type mapping parameters provided.
    /// </summary>
    /// <param name="widgetType">The underlying system type metadata signature tracking the targeting interactive form component.</param>
    /// <returns>A specialized stateless display renderer implementation block, or null if no strategy matches the provided parameter data.</returns>
    IWidgetRenderer? GetRenderer(Type widgetType);
}
