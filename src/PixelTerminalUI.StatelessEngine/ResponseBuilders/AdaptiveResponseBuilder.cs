using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using System.Buffers;

namespace PixelTerminalUI.StatelessEngine.ResponseBuilders;

/// <summary>
/// Implements delta-compression optimization logic to transform buffer sequences into compact screen presentation network packages.
/// </summary>
public sealed class AdaptiveResponseBuilder : IAdaptiveResponseBuilder
{
    /// <summary>
    /// Defines the allocation percentage barrier above which delta framing scales back into an uncompressed full presentation reload.
    /// </summary>
    private const double ChangeThreshold = 0.25;

    /// <summary>
    /// Compiles terminal presentation streams into a structured response package by evaluating modifications between buffer sequences.
    /// </summary>
    /// <param name="sessionId">The unique operational session token mapping to the active connection client.</param>
    /// <param name="currentBuffer">The uncompressed current symbol layout sequence tracking screen metrics.</param>
    /// <param name="historicalBuffer">The optional historical symbol array representing the last successfully broadcast layout state.</param>
    /// <param name="width">The total matrix column cells count defining horizontal boundary limits.</param>
    /// <param name="height">The total matrix row cells count defining vertical boundary limits.</param>
    /// <returns>A concrete terminal network response containing serialized delta mutations or a complete fallback display layout frame.</returns>
    public TerminalResponse Build(
        Guid sessionId,
        uint[] currentBuffer,
        uint[]? historicalBuffer,
        int width,
        int height)
    {
        int totalCellsCount = width * height;

        if (historicalBuffer is null || historicalBuffer.Length != totalCellsCount)
        {
            return new FullFrameResponse(sessionId, currentBuffer, width, height);
        }

        PixelMutation[] pooledMutations = ArrayPool<PixelMutation>.Shared.Rent(totalCellsCount);
        int mutationCount = 0;
        try
        {
            for (int i = 0; i < totalCellsCount; i++)
            {
                if (currentBuffer[i] != historicalBuffer[i])
                {
                    pooledMutations[mutationCount++] = new PixelMutation(i, currentBuffer[i]);
                }
            }

            double changeRatio = (double)mutationCount / totalCellsCount;
            if (changeRatio > ChangeThreshold)
            {
                return new FullFrameResponse(sessionId, currentBuffer, width, height);
            }

            if (mutationCount == 0)
            {
                return new DeltaResponse(sessionId, [], width, height);
            }

            PixelMutation[] finalMutations = new PixelMutation[mutationCount];
            Array.Copy(pooledMutations, finalMutations, mutationCount);

            return new DeltaResponse(sessionId, finalMutations, width, height);
        }
        finally
        {
            ArrayPool<PixelMutation>.Shared.Return(pooledMutations);
        }
    }
}
