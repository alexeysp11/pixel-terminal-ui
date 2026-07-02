using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Domain.Enums;
using TheLostGrid.Server.Scenarios.SectorNavigation;

namespace TheLostGrid.Server.Scenarios.PowerGridTerminal;

/// <summary>
/// Exposes operational parameters managing financial currency liquidation to restore vital power cell balances.
/// </summary>
public sealed class PowerGridTerminalBuyEnergyCommand : Command<OneStepCommandState>
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
    /// Executes financial checking rules before triggering power regeneration routines asynchronously.
    /// </summary>
    /// <param name="context">The localized communication pipeline carrying external request metadata parameters.</param>
    /// <returns>An asynchronous task wrapping a boolean indicator signifying operational pipeline state changes.</returns>
    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context.Screen is not PowerGridTerminalScreen currentGridScreen)
        {
            return false;
        }

        int actionCode = ParseGridAction(context.InputValue);

        if (actionCode == -1)
        {
            context.ErrorMessage = "INVALID OPTION! SELECT 1 OR 0";
            return false;
        }

        if (actionCode == 1)
        {
            // Enforce economic constraints before triggering the resource exchange transaction
            if (currentGridScreen.Credits < 10)
            {
                context.ErrorMessage = "NOT ENOUGH CREDITS (10 CR REQUIRED)";
                return false;
            }

            // Calculate updated boundaries ensuring energy capacity caps out exactly at 100 percent
            int updatedCredits = currentGridScreen.Credits - 10;
            int updatedEnergy = Math.Min(100, currentGridScreen.Energy + 25);

            PowerGridTerminalScreen refreshedGridScreen = new(
                CharacterType,
                updatedEnergy,
                updatedCredits)
            {
                Id = Guid.NewGuid(),
                Name = nameof(PowerGridTerminalScreen),
                SessionId = context.SessionId
            };

            await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, refreshedGridScreen);
            return true;
        }

        // Action 0: Safely route the operator back to the primary navigation post carrying forward the current state
        SectorNavigationScreen returnHubScreen = new(
            CharacterType,
            currentGridScreen.Energy,
            currentGridScreen.Credits)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = context.SessionId
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, returnHubScreen);
        return true;
    }

    /// <summary>
    /// Synchronously processes incoming choice identifiers to distinguish financial commitments from system escape paths.
    /// </summary>
    /// <param name="inputValue">The unchecked raw console textual sequence captured within the view terminal.</param>
    /// <returns>A standardized identifier matching discrete transaction options or a fallback rejection token.</returns>
    private static int ParseGridAction(string inputValue)
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
