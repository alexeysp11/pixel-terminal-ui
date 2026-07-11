using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Commands.Core;
using TheLostGrid.Server.Domain.Enums;

namespace TheLostGrid.Server.Scenarios.SectorScanner;

/// <summary>
/// Exposes operational routines governing the exit sequences out of the tactical radar and scanner terminal displays.
/// </summary>
public sealed class SectorScannerScanCommand : Command<OneStepCommandState>
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
    /// Processes scanning interface exit codes, routing the active player back into the central navigation hub with their resources preserved.
    /// </summary>
    /// <param name="context">The localized communication pipeline carrying external request metadata parameters.</param>
    /// <returns>An asynchronous task wrapping a boolean indicator signifying operational pipeline state changes.</returns>
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
