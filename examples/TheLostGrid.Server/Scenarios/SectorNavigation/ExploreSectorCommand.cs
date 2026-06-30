using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using PixelTerminalUI.StatelessEngine.Screens;
using TheLostGrid.Server.Scenarios.PowerGridTerminal;

namespace TheLostGrid.Server.Scenarios.SectorNavigation;

public sealed class ExploreSectorCommand : Command<SectorNavigationState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override SectorNavigationState State { get; set; } = SectorNavigationState.None;

    public required CharacterType CharacterType { get; init; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context.Screen is not SectorNavigationScreen currentHubScreen)
        {
            return false;
        }

        int chosenRoute = ParseNavigationOption(context.InputValue);

        if (chosenRoute == 0)
        {
            context.ErrorMessage = "INVALID OPTION! SELECT 1 OR 2";
            return false;
        }

        TerminalScreen nextScreen;
        if (chosenRoute == 1)
        {
            if (CharacterType == CharacterType.Hacker)
            {
                // Enforce minimum required operational baseline before triggering the network penetration link
                if (currentHubScreen.Energy < 10)
                {
                    context.ErrorMessage = "NOT ENOUGH ENERGY (10 ENG REQUIRED)";
                    return false;
                }

                // Statelessly generate three unique cybernetic cryptography keys derived from localized system strings
                string[] activeHashes = [
                    Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                    Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                    Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()
                ];

                // Designate the very first randomized matrix array sequence index as the primary access token
                string targetHash = activeHashes[0];

                // Scramble the visual presentation sequence order so the correct option index isn't deterministic
                Random.Shared.Shuffle(activeHashes);

                int defaultAttempts = 2;

                nextScreen = new TerminalHackScreen(
                    CharacterType,
                    currentHubScreen.Energy,
                    currentHubScreen.Credits,
                    defaultAttempts,
                    targetHash,
                    activeHashes)
                {
                    Id = Guid.NewGuid(),
                    Name = nameof(TerminalHackScreen),
                    SessionId = context.SessionId,
                    ParentScreenId = context.Screen.Id
                };
            }
            else
            {
                if (currentHubScreen.Energy < 10)
                {
                    context.ErrorMessage = "NOT ENOUGH ENERGY (10 ENG REQUIRED)";
                    return false;
                }
                nextScreen = new DroneDeploymentScreen(CharacterType, currentHubScreen.Energy, currentHubScreen.Credits)
                {
                    Id = Guid.NewGuid(),
                    Name = nameof(DroneDeploymentScreen),
                    CharacterType = CharacterType,
                    SessionId = context.SessionId
                };
            }
        }
        else if (chosenRoute == 2)
        {
            if (currentHubScreen.Energy < 30)
            {
                context.ErrorMessage = "NOT ENOUGH ENERGY (30 ENG REQUIRED)";
                return false;
            }

            // Calculate the randomized credit cache payload using game balance specifications
            int discoveredCredits = Random.Shared.Next(15, 36);
            int updatedEnergy = Math.Max(0, currentHubScreen.Energy - 30);
            int updatedCredits = currentHubScreen.Credits + discoveredCredits;

            string diagnosticLogMessage = $"Abandoned data cache detected. Credited: {discoveredCredits} CR";

            // Initialize the updated scanner viewport configuration injecting the computed runtime context parameters
            nextScreen = new SectorScannerScreen(
                CharacterType,
                updatedEnergy,
                updatedCredits,
                diagnosticLogMessage)
            {
                Id = Guid.NewGuid(),
                Name = nameof(SectorScannerScreen),
                SessionId = context.SessionId
            };
        }
        else
        {
            if (currentHubScreen.Credits < 10)
            {
                context.ErrorMessage = "NOT ENOUGH CREDITS (10 CR REQUIRED)";
                return false;
            }
            nextScreen = new PowerGridTerminalScreen(CharacterType, currentHubScreen.Energy, currentHubScreen.Credits)
            {
                Id = Guid.NewGuid(),
                Name = nameof(PowerGridTerminalScreen),
                CharacterType = CharacterType,
                SessionId = context.SessionId
            };
        }

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
        return true;
    }

    private static int ParseNavigationOption(string inputValue)
    {
        if (inputValue == null)
        {
            return 0;
        }

        ReadOnlySpan<char> cleanedSpan = inputValue.AsSpan().Trim();
        if (cleanedSpan.Equals("1".AsSpan(), StringComparison.Ordinal))
        {
            return 1;
        }
        if (cleanedSpan.Equals("2".AsSpan(), StringComparison.Ordinal))
        {
            return 2;
        }
        if (cleanedSpan.Equals("3".AsSpan(), StringComparison.Ordinal))
        {
            return 3;
        }
        return 0;
    }
}

