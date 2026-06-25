using PixelTerminalUI.Contracts.Common;

namespace PixelTerminalUI.Contracts.Dto;

/// <summary>
/// Defines an adaptive network response payload containing only the modified grid segments to save bandwidth.
/// </summary>
public sealed record DeltaResponse(
    Guid SessionId,
    PixelMutation[] Mutations,
    int Width,
    int Height) : TerminalResponse(SessionId, Width, Height);
