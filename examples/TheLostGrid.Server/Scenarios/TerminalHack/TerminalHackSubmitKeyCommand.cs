using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Commands.Core;
using TheLostGrid.Server.Scenarios.SectorScanner;
using PixelTerminalUI.Engine.Widgets;

namespace TheLostGrid.Server.Scenarios.TerminalHack;

/// <summary>
/// Manages the evaluation of user decryption inputs during active network cell breach operations.
/// </summary>
public sealed class TerminalHackSubmitKeyCommand : Command<OneStepCommandState>
{
    /// <summary>
    /// Gets or sets the internal state tracking configuration for single-step transaction boundaries.
    /// </summary>
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;

    /// <summary>
    /// Gets the unique structural identifier assigned to this runtime transaction frame instance.
    /// </summary>
    public override Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the target interaction element identifier bound to this automated processing loop.
    /// </summary>
    public override Guid WidgetId { get; set; }

    /// <summary>
    /// Validates the operator decryption attempt, altering active session parameters or navigating to scanner matrices based on accuracy.
    /// </summary>
    /// <param name="context">The localized communication pipeline carrying external request metadata parameters.</param>
    /// <returns>An asynchronous task wrapping a boolean indicator signifying operational pipeline state changes.</returns>
    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context.Screen is not TerminalHackScreen originatingScreen)
        {
            return false;
        }

        // Parse the simplified single digit option code from the user input buffer frame
        int selectedIndex = ParseOptionIndex(context.InputValue);

        // Map the selection to the corresponding hash index array location safely
        string chosenHash = string.Empty;
        if (selectedIndex >= 1 && selectedIndex <= originatingScreen.ActiveHashes.Length)
        {
            chosenHash = originatingScreen.ActiveHashes[selectedIndex - 1];
        }

        // Handle the successful bypass sequence layout route
        if (!string.IsNullOrEmpty(chosenHash) && chosenHash.Equals(originatingScreen.TargetHash, StringComparison.OrdinalIgnoreCase))
        {
            int updatedEnergy = Math.Max(0, originatingScreen.Energy - 10);
            int updatedCredits = originatingScreen.Credits + 30;

            string successLog = "Bypass successful. Credited: 30 CR";

            SectorScannerScreen successScreen = new(
                originatingScreen.CharacterType,
                updatedEnergy,
                updatedCredits,
                successLog)
            {
                Id = Guid.NewGuid(),
                Name = nameof(SectorScannerScreen),
                SessionId = context.SessionId,
                ParentScreenId = context.Screen.Id
            };

            await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, successScreen);
            return true;
        }

        int nextAttempts = originatingScreen.AttemptsLeft - 1;
        int nextEnergy = Math.Max(0, originatingScreen.Energy - 15);

        // Evaluate the failed attempt operational branch context 
        if (originatingScreen.AttemptsLeft > 1)
        {
            // Update widget text markers directly within the active instance loop to guarantee visual consistency
            TextWidget? attemptsLabel = originatingScreen.Widgets.FirstOrDefault(w => w.Name == "HackAttemptsLabel") as TextWidget;
            if (attemptsLabel is not null)
            {
                attemptsLabel.Value = $"ATTEMPTS LEFT: {(nextAttempts > 1 ? "[X] [X]" : "[X] [ ]")}";
            }

            TextWidget? warningLabel = originatingScreen.Widgets.FirstOrDefault(w => w.Name == "WarningText") as TextWidget;
            if (warningLabel is not null)
            {
                warningLabel.Value = $"CRITICAL RESOURCE NET: ENG {nextEnergy}%";
            }

            TerminalHackScreen multiAttemptScreen = originatingScreen with
            {
                Id = Guid.NewGuid(),
                Energy = nextEnergy,
                AttemptsLeft = nextAttempts
            };

            context.ErrorMessage = selectedIndex == -1
                ? "INVALID SELECTION CODE! ENTER 1, 2, OR 3"
                : $"ACCESS DENIED! ATTEMPTS LEFT: {nextAttempts}";

            await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, multiAttemptScreen);
            return false;
        }

        // Total exhaustion of system clearance credentials triggers critical failure routine redirection
        int finalFailedEnergy = Math.Max(0, originatingScreen.Energy - 20);
        string failureLog = "Terminal locked out. Critical trace detected!";

        SectorScannerScreen failureScreen = new(
            originatingScreen.CharacterType,
            finalFailedEnergy,
            originatingScreen.Credits,
            failureLog)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorScannerScreen),
            SessionId = context.SessionId,
            ParentScreenId = context.Screen.Id
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, failureScreen);
        return true;
    }

    /// <summary>
    /// Processes incoming textual representations to parse discrete numerical option codes.
    /// </summary>
    /// <param name="inputValue">The untrusted raw input value payload sequence string.</param>
    /// <returns>A concrete option identifier integer mapping to active collections or a negative boundary token.</returns>
    private static int ParseOptionIndex(string inputValue)
    {
        if (inputValue == null)
        {
            return -1;
        }

        ReadOnlySpan<char> cleanedSpan = inputValue.AsSpan().Trim();

        if (cleanedSpan.Equals("1".AsSpan(), StringComparison.Ordinal)) return 1;
        if (cleanedSpan.Equals("2".AsSpan(), StringComparison.Ordinal)) return 2;
        if (cleanedSpan.Equals("3".AsSpan(), StringComparison.Ordinal)) return 3;

        return -1;
    }
}
