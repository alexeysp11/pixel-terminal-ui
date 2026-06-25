namespace PixelTerminalUI.Contracts.Common;

/// <summary>
/// Represents a single cell visual mutation mapping a buffer array index to its new packed state value.
/// </summary>
public readonly record struct PixelMutation(int Index, uint PackedValue);
