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
    public required CharacterType CharacterType { get; init; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context.Screen is not SectorScannerScreen currentScannerScreen)
        {
            return false;
        }

        // Invoke the isolated synchronous frame helper to prevent async ref-struct compilation conflicts
        if (!IsValidReturnSignal(context.InputValue))
        {
            context.ErrorMessage = "INVALID COMMAND! ENTER 0 TO RETURN";
            return false;
        }

        // Seamlessly route the operative back to the navigation station carrying over updated resource boundaries
        SectorNavigationScreen returnHubScreen = new(
            CharacterType,
            currentScannerScreen.Energy,
            currentScannerScreen.Credits)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = context.SessionId
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, returnHubScreen);

        return true;
    }

    /// <summary>
    /// Validates the raw text payload stream against the structural exit sequence within a synchronous thread boundary.
    /// </summary>
    /// <param name="inputValue">The unchecked raw textual sequence captured from the active terminal interaction boundary.</param>
    /// <returns><c>true</c> if the processed input directly equates to the standard operational exit sequence token; otherwise, <c>false</c>.</returns>
    private static bool IsValidReturnSignal(string inputValue)
    {
        if (inputValue == null)
        {
            return false;
        }
        ReadOnlySpan<char> inputSpan = inputValue.AsSpan().Trim();
        return inputSpan.Equals("0".AsSpan(), StringComparison.Ordinal);
    }
}
