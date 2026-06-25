namespace PixelTerminalUI.StatelessEngine.Widgets;

/// <summary>
/// Represents a basic text widget used to display static or dynamic text in the terminal UI.
/// </summary>
public record TextWidget
{
    /// <summary>
    /// Gets or sets the unique identifier of the widget.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the widget.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the text content or value displayed by the widget.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets the horizontal offset of the widget from the left edge of the parent screen, measured in characters.
    /// </summary>
    public int Left { get; set; }

    /// <summary>
    /// Gets or sets the vertical offset of the widget from the top edge of the parent screen, measured in rows.
    /// </summary>
    public int Top { get; set; }

    /// <summary>
    /// Gets or sets the width of the widget, measured in characters.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the widget, measured in rows.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the widget is rendered on the screen.
    /// </summary>
    public bool Visible { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the widget colors are inverted.
    /// </summary>
    /// <remarks>
    /// When inverted, the text color and background color swap. 
    /// For example, if the screen default is black text on a white background, 
    /// an inverted widget renders as white text on a black background.
    /// </remarks>
    public bool Inverted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user can modify the widget's value.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the widget is editable; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
    /// </value>
    public virtual bool Editable { get; set; }

    /// <summary>
    /// Gets or init the sequence priority index for input focus navigation loops.
    /// If left unassigned (<see langword="null"/>), the framework automatically resolves 
    /// the layout flow sequence using physical geometric screen coordinates (Top-to-Bottom, Left-to-Right).
    /// </summary>
    public int? TabIndex { get; set; }

    /// <summary>
    /// Gets or sets a foreground color of the widget.
    /// </summary>
    public ConsoleColor Foreground { get; set; } = ConsoleColor.White;

    /// <summary>
    /// Gets or sets a background color of the widget.
    /// </summary>
    public ConsoleColor Background { get; set; } = ConsoleColor.Black;
}
