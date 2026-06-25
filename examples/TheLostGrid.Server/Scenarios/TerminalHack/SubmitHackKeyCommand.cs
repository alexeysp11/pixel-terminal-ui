using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.TerminalHack;

public sealed class SubmitHackKeyCommand : Command<OneStepCommandState>
{
    public override OneStepCommandState State { get; set; } = OneStepCommandState.Initial;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        // Execute the allocation-free key validation inside a synchronous stack context block
        if (!IsCorrectHackKey(context.InputValue))
        {
            return false;
        }

        // Map structural state context out of the active running interface layout frame
        CharacterType activeClass = CharacterType.Hacker;
        if (context.Screen is TerminalHackScreen originatingScreen)
        {
            activeClass = originatingScreen.CharacterType;
        }

        // Successfully hacked! Route active terminal pointer back to sector navigation station hub
        SectorNavigationScreen successHubScreen = new(activeClass)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = context.SessionId,
            ParentScreenId = context.Screen.Id
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, successHubScreen);
        return true;
    }

    private static bool IsCorrectHackKey(string inputValue)
    {
        if (inputValue == null)
        {
            return false;
        }

        ReadOnlySpan<char> inputSpan = inputValue.AsSpan().Trim();
        ReadOnlySpan<char> correctKey = "OVERRIDE".AsSpan();
        return inputSpan.Equals(correctKey, StringComparison.OrdinalIgnoreCase);
    }
}
