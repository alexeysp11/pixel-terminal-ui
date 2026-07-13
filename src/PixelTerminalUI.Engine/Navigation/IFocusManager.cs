using PixelTerminalUI.Engine.Screens;

namespace PixelTerminalUI.Engine.Navigation;

/// <summary>
/// Defines the sub-system responsible for calculating layout focus shifts between input widgets.
/// </summary>
public interface IFocusManager
{
    /// <summary>
    /// Determines the identifier of the next input widget to receive focus based on layout hierarchy.
    /// </summary>
    /// <param name="screen">The active screen layout tree to evaluate.</param>
    /// <returns>The <see cref="Guid"/> of the next widget, or <see langword="null"/> if no focusable targets exist.</returns>
    Guid? GetNextFocus(TerminalScreen screen);

    /// <summary>
    /// Determines the identifier of the previous input widget to regain focus based on layout hierarchy.
    /// </summary>
    /// <param name="screen">The active screen layout tree to evaluate.</param>
    /// <returns>The <see cref="Guid"/> of the previous widget, or <see langword="null"/> if no focusable targets exist.</returns>
    Guid? GetPreviousFocus(TerminalScreen screen);
}
