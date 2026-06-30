using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;
using TheLostGrid.Server.Scenarios.SectorNavigation;

namespace TheLostGrid.Server.Scenarios.PowerGridTerminal;

public sealed class BuyEnergyCommand : Command<OneStepCommandState>
{
    public override Guid Id { get; } = Guid.NewGuid();

    public override Guid WidgetId { get; set; }

    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;

    public required CharacterType CharacterType { get; init; }

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
