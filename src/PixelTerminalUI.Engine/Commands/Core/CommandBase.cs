using PixelTerminalUI.Engine.Commands.CommandContexts;

namespace PixelTerminalUI.Engine.Commands.Core;

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
