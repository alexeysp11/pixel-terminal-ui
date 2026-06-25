using PixelTerminalUI.Contracts.Dto;

namespace PixelTerminalUI.StatelessEngine.ResponseBuilders;

public interface IAdaptiveResponseBuilder
{
    TerminalResponse Build(Guid sessionId, uint[] currentBuffer, uint[]? historicalBuffer, int width, int height);
}
