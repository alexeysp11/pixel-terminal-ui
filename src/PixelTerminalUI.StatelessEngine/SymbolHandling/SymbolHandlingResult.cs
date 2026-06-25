namespace PixelTerminalUI.StatelessEngine.SymbolHandling;

public readonly record struct SymbolHandlingResult
{
    public SymbolResultActionType Action { get; init; }
    public string? CustomMessage { get; init; }

    public static SymbolHandlingResult NotHandled() =>
        new() { Action = SymbolResultActionType.NotHandled };

    public static SymbolHandlingResult StayOnScreen() =>
        new() { Action = SymbolResultActionType.StayOnScreen };

    public static SymbolHandlingResult ShiftFocusForward() =>
        new() { Action = SymbolResultActionType.ShiftFocusForward };

    public static SymbolHandlingResult ShiftFocusBackward() =>
        new() { Action = SymbolResultActionType.ShiftFocusBackward };

    public static SymbolHandlingResult TerminateSession(string message) =>
        new() { Action = SymbolResultActionType.TerminateSession, CustomMessage = message };
}
