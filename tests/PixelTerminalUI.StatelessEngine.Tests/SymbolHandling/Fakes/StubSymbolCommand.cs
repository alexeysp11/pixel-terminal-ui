using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;

namespace PixelTerminalUI.StatelessEngine.Tests.SymbolHandling.Fakes;

/// <summary>
/// A lightweight stub command implemented inside the test project 
/// to verify core framework functionality in isolation.
/// </summary>
public sealed class StubSymbolCommand : Command<StubSymbolCommandState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; } = Guid.NewGuid();
    public override StubSymbolCommandState State { get; set; } = StubSymbolCommandState.Initial;

    public bool WasExecuted { get; private set; }
    public ICommandContext? CapturedContext { get; private set; }

    public override ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        WasExecuted = true;
        CapturedContext = context;
        return ValueTask.FromResult(true);
    }
}
