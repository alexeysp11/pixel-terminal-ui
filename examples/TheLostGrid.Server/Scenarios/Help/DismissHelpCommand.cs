using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.StatelessEngine.Screens;

namespace TheLostGrid.Server.Scenarios.Help;

public sealed class DismissHelpCommand : Command<OneStepCommandState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context == null)
        {
            return false;
        }

        // 1. Fetch the active error notification view container to read structural metadata links
        TerminalScreen? currentScreen = await context.SessionRepository.GetActiveScreenAsync(context.SessionId);
        if (currentScreen == null || !currentScreen.ParentScreenId.HasValue)
        {
            // Fallback safety circuit switch: if lineage tracking metadata link is lost, reject processing execution path
            return false;
        }

        // 2. Load the historical parent screen layout model state out of the MongoDB transaction collection
        TerminalScreen? parentScreen = await context.SessionRepository.GetScreenByIdAsync(context.SessionId, currentScreen.ParentScreenId.Value);
        if (parentScreen == null)
        {
            return false;
        }

        // 3. Rollback the primary active runtime pointer to map onto the recovered parent interface layout matrix
        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, parentScreen);
        return true;
    }
}
