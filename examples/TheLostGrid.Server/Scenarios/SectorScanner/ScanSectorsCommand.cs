using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.SectorScanner;

public sealed class ScanSectorsCommand : Command<OneStepCommandState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;
    public CharacterType CharacterType { get; internal set; }
    public ScannerStep CurrentStep { get; internal set; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        // Cast the polymorphic base screen into our concrete hub layout context
        if (context.Screen is not SectorNavigationScreen currentHubScreen)
        {
            return false;
        }

        // Validate basic resource validation boundaries before state transitions
        if (currentHubScreen.Energy < 30)
        {
            context.ErrorMessage = "NOT ENOUGH ENERGY (30 ENG REQUIRED)";
            return false;
        }

        // Execute resource mutations completely statelessly using fluent record with-mutations
        int discoveredCredits = Random.Shared.Next(5, 16);

        SectorNavigationScreen updatedHubScreen = currentHubScreen with
        {
            Id = Guid.NewGuid(), // Generate a new frame container lifecycle identity
            Energy = currentHubScreen.Energy - 30,
            Credits = currentHubScreen.Credits + discoveredCredits
        };

        // Persist the mutated screen directly into the session layer and notify the client
        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, updatedHubScreen);
        return true;
    }
}
