using TheLostGrid.Server.Scenarios.CharacterCreation;
using PixelTerminalUI.Engine.Commands.CommandContexts;
using PixelTerminalUI.Engine.Commands.Core;

namespace TheLostGrid.Server.Scenarios.Welcome;

/// <summary>
/// Manages the transitional sequence required to initialize a new game session and route the operative to character genesis.
/// </summary>
public sealed class WelcomeStartGameCommand : Command<OneStepCommandState>
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
    /// Executes the game entry pipeline, instantiating the character creation matrix and updating session persistence.
    /// </summary>
    /// <param name="context">The localized communication pipeline carrying external request metadata parameters.</param>
    /// <returns>An asynchronous task wrapping a boolean indicator signifying operational pipeline state changes.</returns>
    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        CharacterCreationScreen nextScreen = new()
        {
            Id = Guid.NewGuid(),
            Name = nameof(CharacterCreationScreen),
            SessionId = context.SessionId,
            ParentScreenId = context.Screen.Id
        };

        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, nextScreen);
        return true;
    }
}
