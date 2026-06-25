namespace PixelTerminalUI.StatelessEngine.Widgets;

/// <summary>
/// Represents a masked text input field designed for secure password entry and handling within the terminal UI.
/// </summary>
public record PasswordEntryWidget : TextEntryWidget
{
    /// <summary>
    /// Default secure character mask.
    /// </summary>
    public char MaskChar { get; set; } = '*';
}
