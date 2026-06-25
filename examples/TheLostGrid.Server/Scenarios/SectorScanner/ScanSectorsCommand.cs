using TheLostGrid.Server.Scenarios.CharacterCreation;
using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.SectorScanner;

public sealed class ScanSectorsCommand : Command<SectorNavigationState>
{
    public override SectorNavigationState State { get; set; } = SectorNavigationState.None;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public required CharacterType CharacterType { get; init; }

    public ScannerStep CurrentStep { get; set; } = ScannerStep.InitialTrigger;

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        switch (CurrentStep)
        {
            case ScannerStep.InitialTrigger:
                // Act as the entry point: project the scanner layout screen model
                SectorScannerScreen scannerScreen = new()
                {
                    Id = Guid.NewGuid(),
                    Name = nameof(SectorScannerScreen),
                    CharacterType = CharacterType,
                    SessionId = Guid.NewGuid()
                };
                await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, scannerScreen);
                return true;

            case ScannerStep.AwaitingScreenResponse:
                // Act as the resumption checkpoint handler (simulating compliance with the old delegate return)
                int commandCode = ParseScannerOption(context.InputValue);

                if (commandCode == 0)
                {
                    // User requested a rollback execution branch: restore the main navigation hub layout safely
                    SectorNavigationScreen hubScreen = new(CharacterType)
                    {
                        Id = Guid.NewGuid(),
                        Name = nameof(SectorNavigationScreen),
                        SessionId = context.SessionId
                    };

                    await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, hubScreen);
                    return true;
                }

                // Any other input parameter yields validation failure structures
                return false;

            default:
                return false;
        }
    }

    private static int ParseScannerOption(string inputValue)
    {
        if (inputValue == null)
        {
            return -1;
        }

        ReadOnlySpan<char> cleanedSpan = inputValue.AsSpan().Trim();

        if (cleanedSpan.Equals("0".AsSpan(), StringComparison.Ordinal))
        {
            return 0;
        }

        return -1;
    }
}
