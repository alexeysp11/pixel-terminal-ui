using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;

namespace PixelTerminalUI.Persistence.Mongo.Tests.Extensions.ServiceCollectionExtensions.Fakes;

public sealed class CustomDummyCommand : CommandBase
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override int RawState { get; set; }
    public override ValueTask<bool> ExecuteAsync(ICommandContext context) => ValueTask.FromResult(true);
}
