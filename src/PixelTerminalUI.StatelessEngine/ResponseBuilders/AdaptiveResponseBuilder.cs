using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Dto;
using System.Buffers;

namespace PixelTerminalUI.StatelessEngine.ResponseBuilders;

public sealed class AdaptiveResponseBuilder : IAdaptiveResponseBuilder
{
    private const double ChangeThreshold = 0.25;

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
