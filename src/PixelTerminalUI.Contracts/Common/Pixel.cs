namespace PixelTerminalUI.Contracts.Common;

/// <summary>
/// The structure of one screen pixel.
/// </summary>
public readonly record struct Pixel(
    char Symbol = ' ',
    bool IsInverted = false,
    ConsoleColor Foreground = ConsoleColor.White,
    ConsoleColor Background = ConsoleColor.Black);
