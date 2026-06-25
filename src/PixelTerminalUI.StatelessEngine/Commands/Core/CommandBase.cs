using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;

namespace PixelTerminalUI.StatelessEngine.Commands.Core;

/// <summary>
/// Serves as the base abstract implementation for all system commands.
/// </summary>
public abstract class CommandBase : ICommand
{
    /// <inheritdoc />
    public abstract Guid Id { get; }

    /// <inheritdoc />
    public abstract Guid WidgetId { get; set; }

    /// <inheritdoc />
    public abstract int RawState { get; set; }

    /// <inheritdoc />
    public abstract ValueTask<bool> ExecuteAsync(ICommandContext context);
}
