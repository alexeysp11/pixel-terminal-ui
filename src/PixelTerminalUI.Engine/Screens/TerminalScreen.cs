using PixelTerminalUI.Engine.Widgets;

namespace PixelTerminalUI.Engine.Screens;

/// <summary>
/// Represents the base structural contract for all screens and window containers within the terminal UI.
/// </summary>
public abstract record TerminalScreen
{
    /// <summary>
    /// Gets or sets the globally unique identifier (UUID) for this specific screen instance.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the globally unique identifier (UUID) for the session.
    /// </summary>
    public required Guid SessionId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable name or identifier of the screen.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the screen is rendered on the screen.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the screen is visible; otherwise, <see langword="false"/>. The default is <see langword="true"/>.
    /// </value>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the height of the screen, measured in rows.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the width of the screen, measured in characters.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the parent screen that spawned or owns this screen instance.
    /// </summary>
    /// <value>
    /// The <see cref="Guid"/> of the parent screen, or <see langword="null"/> if this is a root-level screen.
    /// </value>
    public Guid? ParentScreenId { get; set; }

    /// <summary>
    /// Gets or sets the collection of UI widgets owned by and displayed within this screen.
    /// </summary>
    public IEnumerable<TextWidget> Widgets { get; set; } = [];

    /// <summary>
    /// Gets or sets the reference to the <see cref="TextEntryWidget"/> that currently holds user input focus within this screen.
    /// </summary>
    /// <value>
    /// The currently focused input widget, or <see langword="null"/> if no input field is active.
    /// </value>
    public Guid? FocusedEntryWidgetId { get; set; }
}
