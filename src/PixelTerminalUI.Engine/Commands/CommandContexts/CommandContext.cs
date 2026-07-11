using PixelTerminalUI.Engine.Repositories;
using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Commands.CommandContexts;

/// <summary>
/// A concrete implementation of the command execution context used internally by the engine framework.
/// </summary>
public sealed class CommandContext : ICommandContext
{
    /// <inheritdoc/>
    public Guid SessionId { get; init; }

    /// <inheritdoc/>
    public TerminalScreen Screen { get; init; }

    /// <inheritdoc/>
    public TextEntryWidget FocusedEntryWidget { get; init; }

    /// <inheritdoc/>
    public string InputValue { get; init; }

    /// <inheritdoc/>
    public ITerminalSessionRepository SessionRepository { get; init; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandContext"/> class with the specified payload and screen state.
    /// </summary>
    public CommandContext(
        Guid sessionId,
        TerminalScreen screen,
        TextEntryWidget focusedEntryWidget,
        string inputValue,
        ITerminalSessionRepository sessionRepository)
    {
        SessionId = sessionId;
        Screen = screen;
        FocusedEntryWidget = focusedEntryWidget ?? throw new ArgumentNullException(nameof(focusedEntryWidget));
        InputValue = inputValue ?? throw new ArgumentNullException(nameof(inputValue));
        SessionRepository = sessionRepository;
    }
}
