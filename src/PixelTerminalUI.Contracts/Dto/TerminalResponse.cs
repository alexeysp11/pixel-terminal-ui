namespace PixelTerminalUI.Contracts.Dto;

/// <summary>
/// Response from the backend to the client.
/// </summary>
public sealed record TerminalResponse(
    Guid SessionId,
    int Width,
    int Height,
    FullFramePayload? FullFrame = null,
    DeltaPayload? Delta = null);
