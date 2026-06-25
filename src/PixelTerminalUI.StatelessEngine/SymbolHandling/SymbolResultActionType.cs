namespace PixelTerminalUI.StatelessEngine.SymbolHandling;

public enum SymbolResultActionType
{
    NotHandled,
    StayOnScreen,
    ShiftFocusForward,
    ShiftFocusBackward,
    TerminateSession,
    NavigateToParentScreen
}
