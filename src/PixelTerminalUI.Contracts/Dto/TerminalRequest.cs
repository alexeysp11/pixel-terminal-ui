namespace PixelTerminalUI.Contracts.Dto;

/// <summary>
/// Request from client to backend.
/// </summary>
public sealed record TerminalRequest(
    Guid? SessionId,
    string UserInput);
