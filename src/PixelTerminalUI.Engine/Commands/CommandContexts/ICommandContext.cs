using PixelTerminalUI.Engine.Repositories;
using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Commands.CommandContexts;

/// <summary>
/// Defines the execution context provided to a command, containing the current session, 
/// state of the active screen, and the user's input payload.
/// </summary>
public interface ICommandContext
{
    /// <summary>
    /// Gets the unique identifier of the current user session.
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Gets the current stateful representation of the active screen.
    /// </summary>
    TerminalScreen Screen { get; }

    /// <summary>
    /// Gets the instance of the <see cref="TextEntryWidget"/> that currently holds input focus on the screen.
    /// </summary>
    TextEntryWidget FocusedEntryWidget { get; }

    /// <summary>
    /// Gets the raw text value submitted by the user during the current interaction.
    /// </summary>
    string InputValue { get; }

    /// <summary>
    /// Gets the repository used to persist and retrieve user session states.
    /// </summary>
    ITerminalSessionRepository SessionRepository { get; }

    /// <summary>
    /// Gets or sets the error text message.
    /// </summary>
    string? ErrorMessage { get; set; }
}
