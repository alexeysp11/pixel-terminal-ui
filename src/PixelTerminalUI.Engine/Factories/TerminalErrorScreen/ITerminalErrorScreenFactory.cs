namespace PixelTerminalUI.Engine.Factories.TerminalErrorScreen;

using System;
using PixelTerminalUI.Engine.Screens;

/// <summary>
/// Defines a specialized factory component responsible for constructing uniform system notification error screens 
/// using preset dimensions and integrated recovery navigation commands components.
/// </summary>
public interface ITerminalErrorScreenFactory
{
    /// <summary>
    /// Constructs a fully realized error notification layout screen mapped to the parent viewport specifications.
    /// </summary>
    /// <param name="sessionId">The unique connection identity token assigned to the originating connection loop.</param>
    /// <param name="parentScreen">The parent terminal screen structure used to extract geometrical alignment constraints width and height boundaries.</param>
    /// <param name="errorMessage">The explicit business descriptive failure messaging string displayed onto the notification view layout canvas.</param>
    /// <returns>A concrete message screen framework structure complete with built-in acknowledgment escape widget controls elements.</returns>
    SimpleMessageScreen BuildErrorScreen(Guid sessionId, TerminalScreen parentScreen, string errorMessage);
}
