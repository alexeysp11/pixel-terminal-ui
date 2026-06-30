using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Scenarios.SectorScanner;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace TheLostGrid.Server.Scenarios.TerminalHack;

public sealed class SubmitHackKeyCommand : Command<OneStepCommandState>
{
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context.Screen is not TerminalHackScreen originatingScreen)
        {
            return false;
        }

        // Handle the successful bypass sequence layout route
        if (IsCorrectHackKey(context.InputValue, originatingScreen.TargetHash))
        {
            int updatedEnergy = Math.Max(0, originatingScreen.Energy - 10);
            int updatedCredits = originatingScreen.Credits + 30;

            // Use the scanner screen to vividly broadcast the successful extraction log
            string successLog = $"Bypass successful. Credited: 30 CR";

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

        // Evaluate the failed attempt operational branch context 
        // Evaluate the failed attempt operational branch context 
        if (originatingScreen.AttemptsLeft > 1)
        {
            int nextAttempts = originatingScreen.AttemptsLeft - 1;
            int nextEnergy = Math.Max(0, originatingScreen.Energy - 15);

            // Directly locate and mutate the specific widgets text values inside the active record collection
            TextWidget? attemptsLabel = originatingScreen.Widgets
                .FirstOrDefault(w => w.Name == "HackAttemptsLabel") as TextWidget;

            if (attemptsLabel is not null)
            {
                attemptsLabel.Value = $"ATTEMPTS LEFT: {(nextAttempts > 1 ? "[X] [X]" : "[X] [ ]")}";
            }

            TextWidget? warningLabel = originatingScreen.Widgets
                .FirstOrDefault(w => w.Name == "WarningText") as TextWidget;

            if (warningLabel is not null)
            {
                warningLabel.Value = $"CRITICAL RESOURCE NET: ENG {nextEnergy}%";
            }

            // Create the new screen instance with completely synchronized fields
            TerminalHackScreen multiAttemptScreen = originatingScreen with
            {
                Id = Guid.NewGuid(),
                Energy = nextEnergy,
                AttemptsLeft = nextAttempts
            };

            context.ErrorMessage = $"ACCESS DENIED! ATTEMPTS LEFT: {nextAttempts}";
            await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, multiAttemptScreen);
            return false;
        }

        // Total exhaustion of system clearance credentials: route to results screen with a descriptive event log
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

        // Complete the operation successfully with true since the state machine transitions to a valid destination
        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, failureScreen);

        return true;
    }

    private static bool IsCorrectHackKey(string userInput, string targetHash)
    {
        if (userInput == null)
        {
            return false;
        }

        ReadOnlySpan<char> inputSpan = userInput.AsSpan().Trim();
        ReadOnlySpan<char> targetSpan = targetHash.AsSpan();

        return inputSpan.Equals(targetSpan, StringComparison.OrdinalIgnoreCase);
    }
}
