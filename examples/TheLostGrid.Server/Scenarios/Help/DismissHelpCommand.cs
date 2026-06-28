using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;

namespace TheLostGrid.Server.Scenarios.Help;

public sealed class DismissHelpCommand : Command<OneStepCommandState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;

    public override ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        //string lastScreenName = await context.SessionRepository.GetMetadataAsync(context.SessionId, "PreHelpScreen");

        //var originalScreen = context.ScreenFactory.Create(lastScreenName);
        //await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, originalScreen);
        return ValueTask.FromResult(true);
    }
}
