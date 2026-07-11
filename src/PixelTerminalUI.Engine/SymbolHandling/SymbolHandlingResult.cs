namespace PixelTerminalUI.Engine.SymbolHandling;

/// <summary>
/// Represents the immutable transaction payload envelope wrapping the outcome parameters 
/// derived from intercepting specialized console terminal user input control sequences.
/// </summary>
public readonly record struct SymbolHandlingResult
{
    /// <summary>
    /// Gets the structural engine execution routing directive assigned to this transaction processing result instance.
    /// </summary>
    public SymbolResultActionType Action { get; init; }

    /// <summary>
    /// Gets the custom descriptive diagnostic exception details message payload bound to destructive lifecycle state events.
    /// </summary>
    public string? CustomMessage { get; init; }

    /// <summary>
    /// Creates a structural outcome record declaring that no specialized macro interceptors matched the processed textual frame.
    /// </summary>
    /// <returns>A validated evaluation state configured with the default unhandled route mapping directive.</returns>
    public static SymbolHandlingResult NotHandled() =>
        new() { Action = SymbolResultActionType.NotHandled };

    /// <summary>
    /// Creates a structural outcome record instructing the processing pipeline loop to preserve the active screen context allocation.
    /// </summary>
    /// <returns>A validated evaluation state configured with the redraw sequence retention directive.</returns>
    public static SymbolHandlingResult StayOnScreen() =>
        new() { Action = SymbolResultActionType.StayOnScreen };

    /// <summary>
    /// Creates a structural outcome record directing focus mechanics to advance traversal into the next adjacent form control field.
    /// </summary>
    /// <returns>A validated evaluation state configured with the forward control tracking focus instruction.</returns>
    public static SymbolHandlingResult ShiftFocusForward() =>
        new() { Action = SymbolResultActionType.ShiftFocusForward };

    /// <summary>
    /// Creates a structural outcome record directing focus mechanics to drop back into the previous form control field.
    /// </summary>
    /// <returns>A validated evaluation state configured with the backward control tracking focus instruction.</returns>
    public static SymbolHandlingResult ShiftFocusBackward() =>
        new() { Action = SymbolResultActionType.ShiftFocusBackward };

    /// <summary>
    /// Creates a structural outcome record signaling critical fatal validation state bounds violation to terminate the session context.
    /// </summary>
    /// <param name="message">The specific descriptive breakdown notification detailing the core reasons triggering forced state teardown.</param>
    /// <returns>A validated evaluation state configured with destructive session truncation data parameters.</returns>
    public static SymbolHandlingResult TerminateSession(string message) =>
        new() { Action = SymbolResultActionType.TerminateSession, CustomMessage = message };

    /// <summary>
    /// Creates a structural outcome record commanding the processing pipeline loop to discard its current in-memory screen instance, 
    /// fetch the latest serialized snapshot from the persistence layer, and execute a comprehensive presentation redraw.
    /// </summary>
    /// <returns>A validated evaluation state configured with the persistent state reload and refresh execution routing directive.</returns>
    public static SymbolHandlingResult RefreshActiveScreen() =>
        new() { Action = SymbolResultActionType.RefreshActiveScreen };
}
