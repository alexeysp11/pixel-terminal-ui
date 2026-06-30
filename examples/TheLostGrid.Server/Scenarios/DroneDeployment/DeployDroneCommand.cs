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

        if (context.Screen is not DroneDeploymentScreen operationalScreen)
        {
            return false;
        }

        switch (State)
        {
            case DroneDeploymentState.AwaitingCommand:
                int actionCode = ParseDroneAction(context.InputValue);

                if (actionCode == -1)
                {
                    context.ErrorMessage = "INVALID OPTION! SELECT 1 OR 0";
                    return false;
                }

                TerminalScreen nextScreen;
                if (actionCode == 1)
                {
                    // Enforce minimum resource verification limits before spawning a physical drone hardware component
                    if (operationalScreen.Energy < 10)
                    {
                        context.ErrorMessage = "NOT ENOUGH ENERGY (10 ENG REQUIRED)";
                        return false;
                    }

                    // Simulate drone deployment risk evaluation via cryptographic network layers roll calculations
                    int successRoll = Random.Shared.Next(1, 101);
                    bool isDeploymentSuccessful = successRoll <= 70;

                    int updatedEnergy;
                    int updatedCredits;
                    string resultMessage;

                    if (isDeploymentSuccessful)
                    {
                        updatedEnergy = Math.Max(0, operationalScreen.Energy - 10);
                        updatedCredits = operationalScreen.Credits + 15;
                        resultMessage = "SUCCESS: ARTIFACT EXTRACTED! +15 CR";
                    }
                    else
                    {
                        // Total hardware collision destruction triggers increased operational energy mitigation costs
                        updatedEnergy = Math.Max(0, operationalScreen.Energy - 20);
                        updatedCredits = operationalScreen.Credits;
                        resultMessage = "CRITICAL: DRONE DESTROYED BY TURRETS!";
                    }

                    // Route to the shared SectorScannerScreen screen layout passing the calculated payload data
                    nextScreen = new SectorScannerScreen(
                        CharacterType,
                        updatedEnergy,
                        updatedCredits,
                        resultMessage)
                    {
                        Id = Guid.NewGuid(),
                        Name = nameof(SectorScannerScreen),
                        CharacterType = CharacterType,
                        SessionId = context.SessionId
                    };
                }
                else
                {
                    // Unconditional safe navigation retreat route back to the sector navigation command post
                    nextScreen = new SectorNavigationScreen(CharacterType, operationalScreen.Energy, operationalScreen.Credits)
                    {
                        Id = Guid.NewGuid(),
                        Name = nameof(SectorNavigationScreen),
                        SessionId = context.SessionId
                    };
                }

                await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
                return true;

            default:
                return false;
        }
    }

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
