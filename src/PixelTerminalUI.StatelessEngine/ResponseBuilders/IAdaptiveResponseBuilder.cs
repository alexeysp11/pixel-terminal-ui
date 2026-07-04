using PixelTerminalUI.Contracts.Dto;

namespace PixelTerminalUI.StatelessEngine.ResponseBuilders;

/// <summary>
/// Defines the architectural factory contract required to compile terminal presentation data packages into optimal network frames.
/// </summary>
public interface IAdaptiveResponseBuilder
{
    /// <summary>
    /// Compiles terminal presentation streams into a structured response package by evaluating modifications between buffer sequences.
    /// </summary>
    /// <param name="sessionId">The unique operational session token mapping to the active connection client.</param>
    /// <param name="currentBuffer">The uncompressed current symbol layout sequence tracking screen metrics.</param>
    /// <param name="historicalBuffer">The optional historical symbol array representing the last successfully broadcast layout state.</param>
    /// <param name="width">The total matrix column cells count defining horizontal boundary limits.</param>
    /// <param name="height">The total matrix row cells count defining vertical boundary limits.</param>
    /// <returns>A concrete terminal network response containing serialized delta mutations or a complete fallback display layout frame.</returns>
    TerminalResponse Build(Guid sessionId, uint[] currentBuffer, uint[]? historicalBuffer, int width, int height);
}
