using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Commands.Core;
using PixelTerminalUI.StatelessEngine.Screens;

namespace PixelTerminalUI.StatelessEngine.Commands.DismissError;

/// <summary>
/// Manages the operational cycle required to discard error notification overlays and restore ancestral viewport matrixes.
/// </summary>
public sealed class DismissErrorCommand : Command<OneStepCommandState>
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
    /// Executes the layout rollback pipeline sequence asynchronously using the provided environment boundaries.
    /// </summary>
    /// <param name="context">The localized communication pipeline carrying external request metadata parameters.</param>
    /// <returns>An asynchronous task wrapping a boolean indicator signifying operational pipeline state changes.</returns>
    public override async ValueTask<bool> ExecuteAsync(ICommandContext context)
    {
        if (context == null)
        {
            return false;
        }

        // 1. Fetch the active error notification view container to read structural metadata links
        TerminalScreen? currentScreen = await context.SessionRepository.GetActiveScreenAsync(context.SessionId);
        if (currentScreen == null || !currentScreen.ParentScreenId.HasValue)
        {
            // Fallback safety circuit switch: if lineage tracking metadata link is lost, reject processing execution path
            return false;
        }

        // 2. Load the historical parent screen layout model state out of the MongoDB transaction collection
        TerminalScreen? parentScreen = await context.SessionRepository.GetScreenByIdAsync(context.SessionId, currentScreen.ParentScreenId.Value);
        if (parentScreen == null)
        {
            return false;
        }

        // 3. Rollback the primary active runtime pointer to map onto the recovered parent interface layout matrix
        await context.SessionRepository.SaveActiveScreenAsync(context.SessionId, parentScreen);
        return true;
    }
}
