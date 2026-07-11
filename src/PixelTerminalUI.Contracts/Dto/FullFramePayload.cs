namespace PixelTerminalUI.Contracts.Dto;

/// <summary>
/// Defines a comprehensive structural payload enclosing the complete screen matrix layout metadata block.
/// </summary>
public sealed record FullFramePayload(uint[] ScreenBuffer);
