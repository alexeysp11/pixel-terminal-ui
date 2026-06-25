using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;

namespace PixelTerminalUI.Persistence.Mongo.Tests.Repositories.Fakes;

/// <summary>
/// The concrete command class that will be embedded inside the TextEntryWidget to verify mapping behavior.
/// </summary>
public sealed class CancelTasksCommand : Command<CancelTasksState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override CancelTasksState State { get; set; } = CancelTasksState.Undefined;
    public string? LastAttemptValue { get; set; }

    public override ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        // Implementation stub for mapping validation
        return ValueTask.FromResult(true);
    }
}

