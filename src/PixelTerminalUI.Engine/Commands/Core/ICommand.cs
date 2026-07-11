using PixelTerminalUI.Engine.Commands.CommandContexts;

namespace PixelTerminalUI.Engine.Commands.Core;

/// <summary>
/// Defines a serializable, atomic business operation triggered by a UI widget event.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the unique identifier of this specific command instance.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets or sets the unique identifier of the UI widget bound to this command.
    /// </summary>
    Guid WidgetId { get; set; }

    /// <summary>
    /// Gets or sets the raw, integer-mapped execution state of the internal finite state machine.
    /// Used for seamless database serialization.
    /// </summary>
    int RawState { get; set; }

    /// <summary>
    /// Executes the business logic asynchronously within the provided context.
    /// </summary>
    /// <param name="context">The runtime execution context containing session data and input.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> indicating whether the operation succeeded and mutated the UI state.</returns>
    ValueTask<bool> ExecuteAsync(ICommandContext context);
}
