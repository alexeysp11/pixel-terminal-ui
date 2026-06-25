using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.SectorScanner;
using TheLostGrid.Server.Scenarios.TerminalHack;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;
using TheLostGrid.Server.Scenarios.DroneDeployment;
using PixelTerminalUI.StatelessEngine.Screens;

namespace TheLostGrid.Server.Scenarios.SectorNavigation;

public sealed class ExploreSectorCommand : Command<SectorNavigationState>
{
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }
    public override SectorNavigationState State { get; set; } = SectorNavigationState.None;

    public required CharacterType CharacterType { get; init; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        int chosenRoute = ParseNavigationOption(context.InputValue);

        if (chosenRoute == 0)
        {
            return false;
        }

        TerminalScreen nextScreen;
        if (chosenRoute == 1)
        {
            if (CharacterType == CharacterType.Hacker)
            {
                nextScreen = new TerminalHackScreen()
                {
                    Id = Guid.NewGuid(),
                    Name = nameof(TerminalHackScreen),
                    CharacterType = CharacterType,
                    SessionId = context.SessionId,
                    ParentScreenId = context.Screen.Id
                };
            }
            else
            {
                nextScreen = new DroneDeploymentScreen()
                {
                    Id = Guid.NewGuid(),
                    Name = nameof(DroneDeploymentScreen),
                    CharacterType = CharacterType,
                    SessionId = context.SessionId,
                    ParentScreenId = context.Screen.Id
                };
            }
        }
        else
        {
            nextScreen = new SectorScannerScreen()
            {
                Id = Guid.NewGuid(),
                Name = "SectorScannerScreen",
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
        return 0;
    }
}
