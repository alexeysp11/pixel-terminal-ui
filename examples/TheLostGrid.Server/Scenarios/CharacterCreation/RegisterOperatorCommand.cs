using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Enums;

namespace TheLostGrid.Server.Scenarios.CharacterCreation;

public sealed class RegisterOperatorCommand : Command<SectorNavigationState>
{
    public override SectorNavigationState State { get; set; } = SectorNavigationState.None;
    public override Guid Id { get; } = Guid.NewGuid();
    public override Guid WidgetId { get; set; }

    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        CharacterType characterType = ParseCharacterType(context.InputValue);
        if (characterType is CharacterType.None)
        {
            context.ErrorMessage = "Incorrect character type!";
            return false;
        }

        SectorNavigationScreen nextScreen = new(characterType)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            ParentScreenId = context.Screen.Id,
            SessionId = context.SessionId
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
        return true;
    }

    /// <summary>
    /// Parses the raw incoming user string input slice into a valid archetype profile designation.
    /// </summary>
    /// <param name="inputValue">The untrimmed command-line parameter data string from the active text widget.</param>
    /// <returns>A concrete <see cref="CharacterType"/> token if a matching signature is found; otherwise, <see cref="CharacterType.None"/>.</returns>
    private static CharacterType ParseCharacterType(string inputValue)
    {
        if (inputValue == null)
        {
            return CharacterType.None;
        }

        // Create zero-allocation structure view window over incoming text boundary context frame
        ReadOnlySpan<char> inputSpan = inputValue.AsSpan().Trim();

        ReadOnlySpan<char> hackerSignature = "HACKER".AsSpan();
        if (inputSpan.Equals(hackerSignature, StringComparison.OrdinalIgnoreCase))
        {
            return CharacterType.Hacker;
        }

        ReadOnlySpan<char> riggerSignature = "RIGGER".AsSpan();
        if (inputSpan.Equals(riggerSignature, StringComparison.OrdinalIgnoreCase))
        {
            return CharacterType.Rigger;
        }

        return CharacterType.None;
    }
}
