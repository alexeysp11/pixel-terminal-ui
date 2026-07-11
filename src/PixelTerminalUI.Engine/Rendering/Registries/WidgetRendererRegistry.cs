using PixelTerminalUI.Engine.Rendering.WidgetRendering;

namespace PixelTerminalUI.Engine.Rendering.Registries;

/// <summary>
/// A registry container that encapsulates a dictionary of renderers for O(1) lookup.
/// </summary>
public sealed class WidgetRendererRegistry(IEnumerable<IWidgetRenderer> renderers) : IWidgetRendererRegistry
{
    private readonly Dictionary<Type, IWidgetRenderer> _renderers = renderers.ToDictionary(r => r.SupportedWidgetType, r => r);

    public IWidgetRenderer? GetRenderer(Type widgetType)
        => _renderers.TryGetValue(widgetType, out IWidgetRenderer? renderer) ? renderer : null;
}
