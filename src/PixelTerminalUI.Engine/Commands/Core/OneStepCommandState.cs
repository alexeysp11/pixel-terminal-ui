namespace PixelTerminalUI.Engine.Commands.Core;

/// <summary>
/// Defines the available transactional execution lifecycle state boundaries for single-step terminal tracking operations.
/// </summary>
public enum OneStepCommandState
{
    /// <summary>
    /// Specifies the entry or initial setup state before processing transactional input blocks.
    /// </summary>
    Initial = 0,
}
