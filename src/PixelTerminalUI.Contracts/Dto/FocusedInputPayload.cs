namespace PixelTerminalUI.Contracts.Dto;

/// <summary>
/// Defines the absolute on-screen coordinates, constraints, and current text of the currently focused input widget,
/// allowing thin clients to seed a local edit buffer and render the input cursor inline within the widget instead
/// of a detached prompt line.
/// </summary>
/// <param name="X">The absolute column of the widget's left edge on the server-rendered screen matrix.</param>
/// <param name="Y">The absolute row of the widget on the server-rendered screen matrix.</param>
/// <param name="MaxLength">The widget's rendered width, used by the client to clamp local echo.</param>
/// <param name="IsMasked">Whether the client must echo a mask character instead of raw keystrokes.</param>
/// <param name="EmptyFillChar">The placeholder glyph to restore after a backspace, matching the widget's empty-enter symbol.</param>
/// <param name="InitialValue">
/// The widget's already-committed text, used to seed the client's local edit buffer so backspace can remove
/// previously entered characters. Always empty for masked widgets, since raw secret content is never sent back
/// to the client for re-editing.
/// </param>
/// <param name="Foreground">The widget's configured foreground color, so local echo matches the server-rendered frame.</param>
/// <param name="Background">The widget's configured background color, so local echo matches the server-rendered frame.</param>
/// <param name="Inverted">Whether the widget renders with foreground/background swapped, matching the server-side pixel renderer.</param>
public sealed record FocusedInputPayload(
    int X,
    int Y,
    int MaxLength,
    bool IsMasked,
    char EmptyFillChar,
    string InitialValue,
    ConsoleColor Foreground,
    ConsoleColor Background,
    bool Inverted);
