using PixelTerminalUI.Engine.Screens;

namespace PixelTerminalUI.Engine.Factories.StartupScreen;

/// <summary>
/// Defines a factory blueprint for instantiating the initial, entry-point screen 
/// when a new terminal session is established.
/// </summary>
public interface IStartupScreenFactory
{
    /// <summary>
    /// Creates and configures a new instance of the root screen for the specified session.
    /// </summary>
    /// <param name="sessionId">The unique identifier tracking the active user connection.</param>
    /// <returns>A newly allocated <see cref="TerminalScreen"/> with initialized routing metadata.</returns>
    TerminalScreen CreateScreen(Guid sessionId);
}
