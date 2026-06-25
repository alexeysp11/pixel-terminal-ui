using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.StatelessEngine.Screens;
using TheLostGrid.Server.Enums;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using TheLostGrid.Server.Scenarios.SectorScanner;

namespace TheLostGrid.Server.Scenarios.DroneDeployment;

public sealed class DeployDroneCommand : Command<DroneDeploymentState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override DroneDeploymentState State { get; set; } = DroneDeploymentState.AwaitingCommand;

    public required CharacterType CharacterType { get; init; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context == null)
        {
            return false;
        }

        switch (State)
        {
            case DroneDeploymentState.AwaitingCommand:
                int actionCode = ParseDroneAction(context.InputValue);

                if (actionCode == -1)
                {
                    // Invalid telemetry input directive triggers validation layout fallback
                    return false;
                }

                TerminalScreen nextScreen;

                if (actionCode == 1)
                {
                    // Route to the shared SectorScannerScreen screen layout
                    nextScreen = new SectorScannerScreen()
                    {
                        Id = Guid.NewGuid(),
                        Name = "SectorScannerScreen",
                        CharacterType = CharacterType,
                        SessionId = context.SessionId,
                        ParentScreenId = context.Screen.Id
                    };
                }
                else
                {
                    // Route 0: Safe disconnection, return back to SectorNavigationScreen (Hub)
                    nextScreen = new SectorNavigationScreen(CharacterType)
                    {
                        Id = Guid.NewGuid(),
                        Name = nameof(SectorNavigationScreen),
                        SessionId = context.SessionId,
                        ParentScreenId = context.Screen.Id
                    };

                    // Access the context's current screen securely via state mapping to re-inject character type properties
                    if (context.Screen is DroneDeploymentScreen operationalScreen)
                    {
                        // In real flow, exploreCommand inside nextScreen would accept this property to maintain loop state consistency
                    }
                }

                await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Evaluates raw stream signals using zero-allocation ref-struct processing windows.
    /// </summary>
    private static int ParseDroneAction(string inputValue)
    {
        if (inputValue == null)
        {
            return -1;
        }

        ReadOnlySpan<char> inputSpan = inputValue.AsSpan().Trim();
        if (inputSpan.Equals("1".AsSpan(), StringComparison.Ordinal))
        {
            return 1;
        }
        if (inputSpan.Equals("0".AsSpan(), StringComparison.Ordinal))
        {
            return 0;
        }
        return -1;
    }
}
