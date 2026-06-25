using PixelTerminalUI.StatelessEngine.Screens;

namespace PixelTerminalUI.StatelessEngine.Repositories;

/// <summary>
/// Provides a unified standalone data storage abstraction boundary separating industrial domain navigational processes 
/// and state workflows from concrete underlying storage engine technologies.
/// </summary>
public interface ITerminalSessionRepository
{
    /// <summary>
    /// Locates the originating active session envelope and retrieves the current complete viewport screen configuration instance.
    /// </summary>
    /// <param name="sessionId">The unique remote connection identity token used as the root search key.</param>
    /// <param name="cancellationToken">The technical token execution monitor used to signal structural abort operations flags.</param>
    /// <returns>A fully realized terminal view tree metadata layout, or null if no matching runtime boundary tracking checkpoint exists.</returns>
    Task<TerminalScreen?> GetActiveScreenAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a standalone inactive or historical screen layout configuration mapping tree straight from the storage indexes.
    /// </summary>
    /// <param name="sessionId">The unique remote connection identity token assigned to the tracking connection loop.</param>
    /// <param name="screenId">The targeted independent blueprint model identifier pointer.</param>
    /// <param name="cancellationToken">The technical token execution monitor used to signal structural abort operations flags.</param>
    /// <returns>The independent screen layout mapping configuration, or null if the specified checkpoint index cannot be resolved.</returns>
    Task<TerminalScreen?> GetScreenByIdAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists or replaces concrete screen models states while atomically updating the current primary active screen pointer link in the session envelope.
    /// </summary>
    /// <param name="sessionId">The unique remote connection identity token tracking the target interactive boundary environment.</param>
    /// <param name="screen">The concrete structural screen instance tree layout slated for persistent preservation paths.</param>
    /// <param name="cancellationToken">The technical token execution monitor used to signal structural abort operations flags.</param>
    Task SaveActiveScreenAsync(Guid sessionId, TerminalScreen screen, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evicts an independent transient screen blueprint configuration layout document out of persistent storage tracking collection scopes.
    /// </summary>
    /// <param name="sessionId">The unique remote connection identity token tracking the originating user interactive context.</param>
    /// <param name="screenId">The targeted structural blueprint instance checkpoint identifier slated for eviction.</param>
    /// <param name="cancellationToken">The technical token execution monitor used to signal structural abort operations flags.</param>
    Task RemoveScreenAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the low-level double buffering frame tracking cache slice containing the last successfully broadcast viewport visualization layer.
    /// </summary>
    /// <param name="sessionId">The unique connection identity token mapping onto the targeting transaction space environment.</param>
    /// <returns>The packed 32-bit unsigned primitive baseline array, or null if no historical graphic snapshot state has been saved yet.</returns>
    Task<uint[]?> GetHistoricalBufferAsync(Guid sessionId);

    /// <summary>
    /// Overwrites the technical double buffering background frame tracking cache slice with a freshly rendered canvas primitive stream layout.
    /// </summary>
    /// <param name="sessionId">The unique connection identity token tracking the target user session context envelope.</param>
    /// <param name="currentBuffer">The freshly composed flat packed 32-bit unsigned primitive stream array tracking active terminal cell codes.</param>
    Task SaveHistoricalBufferAsync(Guid sessionId, uint[] currentBuffer);
}
