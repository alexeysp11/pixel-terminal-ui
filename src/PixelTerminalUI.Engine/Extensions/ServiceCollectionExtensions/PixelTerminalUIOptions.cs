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

    /// <summary>
    /// The screen change threshold (from <c>0.0</c> to <c>1.0</c>) above which the engine
    /// switches from sending deltas to full frame. Default is <c>0.3</c> (30%).
    /// </summary>
    public double DoubleBufferingThreshold { get; set; } = 0.3;
}
