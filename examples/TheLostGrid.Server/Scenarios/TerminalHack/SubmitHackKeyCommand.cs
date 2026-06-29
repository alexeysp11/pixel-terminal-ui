using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;

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

            SectorNavigationScreen successHubScreen = new(originatingScreen.CharacterType, updatedEnergy, updatedCredits)
            {
                Id = Guid.NewGuid(),
                Name = nameof(SectorNavigationScreen),
                SessionId = context.SessionId,
                ParentScreenId = context.Screen.Id
            };

            await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, successHubScreen);
            return true;
        }

        // Evaluate the failed attempt operational branch context 
        if (originatingScreen.AttemptsLeft > 1)
        {
            // The operative has remaining clearance attempts left within the network frame
            TerminalHackScreen multiAttemptScreen = originatingScreen with
            {
                Id = Guid.NewGuid(),
                Energy = Math.Max(0, originatingScreen.Energy - 15),
                AttemptsLeft = originatingScreen.AttemptsLeft - 1
            };

            context.ErrorMessage = $"ACCESS DENIED! ATTEMPTS LEFT: {multiAttemptScreen.AttemptsLeft}";
            await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, multiAttemptScreen);
            return false;
        }

        // Total exhaustion of system clearance credentials triggers critical failure routine redirection
        int finalFailedEnergy = Math.Max(0, originatingScreen.Energy - 20);

        SectorNavigationScreen failureHubScreen = new(originatingScreen.CharacterType, finalFailedEnergy, originatingScreen.Credits)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = context.SessionId,
            ParentScreenId = context.Screen.Id
        };

        context.ErrorMessage = "TERMINAL LOCKED: CRITICAL TRACE DETECTED!";
        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, failureHubScreen);

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
