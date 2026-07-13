using PixelTerminalUI.Engine.Commands.Core;

namespace PixelTerminalUI.Engine.Widgets;

/// <summary>
/// Represents an interactive text input field that allows users to enter and edit text in the terminal UI.
/// </summary>
public record TextEntryWidget : TextWidget
{
    /// <inheritdoc/>
    /// <value>
    /// <see langword="true"/> if the widget allows user input; otherwise, <see langword="false"/>. 
    /// For <see cref="TextEntryWidget"/>, the default is <see langword="true"/>.
    /// </value>
    public override bool Editable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether user input is mandatory for this widget during screen validation.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if input is required; otherwise, <see langword="false"/>. The default is <see langword="true"/>.
    /// </value>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Gets or sets the character used to fill the empty trailing space of the input field.
    /// </summary>
    /// <example>
    /// If the value is <c>"Text"</c>, the widget width is 8, and the <see cref="EmptyEnterSymbol"/> is <c>'.'</c>, 
    /// it renders as <c>"Text...."</c>.
    /// </example>
    public char EmptyEnterSymbol { get; set; } = '.';

    /// <summary>
    /// Gets or sets an optional contextual hint or tooltip displayed at the bottom of the screen when this widget is focused.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// The command that should handle the Enter key press event.
    /// </summary>
    public CommandBase? Command { get; set; }
}
