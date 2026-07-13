using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Commands.Core;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using PixelTerminalUI.Engine.Screens;
using TheLostGrid.Server.Scenarios.PowerGridTerminal;
using TheLostGrid.Server.Domain.Enums;

namespace TheLostGrid.Server.Scenarios.SectorNavigation;

/// <summary>
/// Handles the decision matrix routing paths when an operator interacts with the primary sector navigation grid.
/// </summary>
public sealed class SectorNavigationExploreCommand : Command<OneStepCommandState>
{
    /// <summary>
    /// Gets the unique structural identifier assigned to this runtime transaction frame instance.
    /// </summary>
    public override Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the target interaction element identifier bound to this automated processing loop.
    /// </summary>
    public override Guid WidgetId { get; set; }

    /// <summary>
    /// Gets or sets the internal state tracking configuration for single-step transaction boundaries.
    /// </summary>
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;

    /// <summary>
    /// Gets or init the unique operational archetype signature for the active session boundary frame.
    /// </summary>
    public required CharacterType CharacterType { get; init; }

    /// <summary>
    /// Evaluates the tactical choice index input, performs vital energy or credit validation, and switches to the chosen terminal screen view context.
    /// </summary>
    /// <param name="context">The localized communication pipeline carrying external request metadata parameters.</param>
    /// <returns>An asynchronous task wrapping a boolean indicator signifying operational pipeline state changes.</returns>
    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context.Screen is not SectorNavigationScreen currentHubScreen)
        {
            return false;
        }

        int chosenRoute = ParseNavigationOption(context.InputValue);

        if (chosenRoute == 0)
        {
            context.ErrorMessage = "INVALID OPTION! SELECT 1, 2 OR 3";
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

    /// <summary>
    /// Processes text characters to extract structural grid choice routing numbers.
    /// </summary>
    /// <param name="inputValue">The untrusted raw input value payload sequence string.</param>
    /// <returns>A concrete operational target option index or a default error-bound integer signature mapping.</returns>
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
