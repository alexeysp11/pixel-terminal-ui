namespace PixelTerminalUI.Engine.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Provides configuration settings and performance optimization toggles utilized to calibrate the runtime core terminal engine behaviors.
/// </summary>
public sealed class PixelTerminalUIOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the engine should calculate frame differences 
    /// and emit delta updates instead of always broadcasting full frames.
    /// </summary>
    public bool EnableDoubleBuffering { get; set; } = true;
}
