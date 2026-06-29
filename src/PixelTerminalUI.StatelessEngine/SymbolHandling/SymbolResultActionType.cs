namespace PixelTerminalUI.StatelessEngine.SymbolHandling;

/// <summary>
/// Specifies the structural execution action routes available for the core 
/// input pipeline processing infrastructure upon evaluation of special terminal symbols.
/// </summary>
public enum SymbolResultActionType
{
    /// <summary>
    /// Indicates that the examined character or prefix sequence was not intercepted 
    /// by the active processing layer and should fall back to standard widget commands execution.
    /// </summary>
    NotHandled,

    /// <summary>
    /// Commands the request execution pipeline to retain the currently active layout form index 
    /// and perform a complete presentation layer redraw sequence.
    /// </summary>
    StayOnScreen,

    /// <summary>
    /// Evaluates structural form parameters to dynamically shift the terminal interaction input 
    /// focus segment to the next available chronological widget container.
    /// </summary>
    ShiftFocusForward,

    /// <summary>
    /// Evaluates structural form parameters to dynamically shift the terminal interaction input 
    /// focus segment to the previous chronological widget container.
    /// </summary>
    ShiftFocusBackward,

    /// <summary>
    /// Forces immediate destruction of the active state allocation scope inside the cache persistence provider 
    /// and breaks the active network transaction connection link.
    /// </summary>
    TerminateSession,

    /// <summary>
    /// Triggers historical state trace traversal tracking records to revert presentation layout context 
    /// back to the parent form container node index.
    /// </summary>
    NavigateToParentScreen,

    /// <summary>
    /// Commands the request execution pipeline to bypass further global structural macro handling 
    /// but immediately invoke the concrete business logic payload execution bound to the currently focused text input widget.
    /// </summary>
    ExecuteCommand,

    /// <summary>
    /// Instructs the request execution pipeline that the active layout state has been externally mutated 
    /// within the persistence repository layer, forcing a complete reload of the screen from cache before redrawing the viewport.
    /// </summary>
    RefreshActiveScreen
}
