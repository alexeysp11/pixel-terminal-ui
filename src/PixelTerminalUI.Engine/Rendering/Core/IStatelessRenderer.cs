using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Engine.Screens;

namespace PixelTerminalUI.Engine.Rendering.Core;

/// <summary>
/// Defines the rendering engine responsible for transforming a declarative screen layout 
/// into a raw, character pixel array.
/// </summary>
public interface IStatelessRenderer
{
    /// <summary>
    /// Evaluates the screen layout and draws layout tokens directly into the provided memory buffer.
    /// </summary>
    /// <param name="screen">The stateful screen abstraction containing structural UI component hierarchies.</param>
    /// <param name="buffer">The flat destination array allocated or rented by the caller to store screen state.</param>
    void Draw(TerminalScreen screen, Pixel[] buffer);
}
