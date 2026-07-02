using TheLostGrid.Server.Scenarios.SectorNavigation;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using TheLostGrid.Server.Domain.Enums;

namespace TheLostGrid.Server.Scenarios.CharacterCreation;

/// <summary>
/// Manages the submission layer during the operative profile selection and genesis sequence.
/// </summary>
public sealed class CharacterCreationSubmitCommand : Command<OneStepCommandState>
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
    /// Validates the operator archetype choice and registers the default resource metrics into the central navigation workspace.
    /// </summary>
    /// <param name="context">The localized communication pipeline carrying external request metadata parameters.</param>
    /// <returns>An asynchronous task wrapping a boolean indicator signifying operational pipeline state changes.</returns>
    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        CharacterType characterType = ParseCharacterType(context.InputValue);

        if (characterType is CharacterType.None)
        {
            context.ErrorMessage = "INVALID CLASS! ENTER 'H' OR 'R'";
            return false;
        }

        // Establish the default operational parameters for the initial system bootstrap cycle
        int initialEnergy = 100;
        int initialCredits = 50;

        // Instantiate the hub form passing the initial operational parameters directly into the constructor
        SectorNavigationScreen nextScreen = new(characterType, initialEnergy, initialCredits)
        {
            Id = Guid.NewGuid(),
            Name = nameof(SectorNavigationScreen),
            SessionId = context.SessionId
        };

        // Persist the initial screen model into the storage engine repository layer
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
        if (string.IsNullOrWhiteSpace(inputValue))
        {
            return CharacterType.None;
        }

        // The operator class selection must strictly be a single character action identifier
        ReadOnlySpan<char> inputSpan = inputValue.AsSpan().Trim();
        if (inputSpan.Length != 1)
        {
            return CharacterType.None;
        }

        char actionKey = char.ToUpperInvariant(inputSpan[0]);
        return actionKey switch
        {
            'H' => CharacterType.Hacker,
            'R' => CharacterType.Rigger,
            _ => CharacterType.None
        };
    }
}
