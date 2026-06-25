using TheLostGrid.Server.Scenarios.CharacterCreation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;

namespace TheLostGrid.Server.Scenarios.Welcome;

public sealed class ConnectNeuralLinkCommand : Command<WelcomeScreenState>
{
    public override WelcomeScreenState State { get; set; } = WelcomeScreenState.AwaitingConnection;

    public override Guid Id { get; } = Guid.NewGuid();

    public override Guid WidgetId { get; set; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        CharacterCreationScreen nextScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(CharacterCreationScreen),
            SessionId = context.SessionId,
            ParentScreenId = context.Screen.Id
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
        return true;
    }
}
