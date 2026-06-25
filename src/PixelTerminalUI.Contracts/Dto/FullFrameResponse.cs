namespace PixelTerminalUI.Contracts.Dto;

public sealed record FullFrameResponse(
    Guid SessionId,
    uint[] ScreenBuffer,
    int Width,
    int Height) : TerminalResponse(SessionId, Width, Height);
