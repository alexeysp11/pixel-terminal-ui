using PixelTerminalUI.Engine.Screens;

namespace PixelTerminalUI.Engine.Validators;

/// <summary>
/// Encapsulates a stateless business validation statement running against the runtime layout state and the active input stream buffer.
/// </summary>
/// <param name="screen">The operational terminal viewport state containing layout properties and component configurations metadata.</param>
/// <param name="currentInput">The raw character string submitted from the remote physical keyboard or hardware scanner interface.</param>
/// <returns>A structured container object encapsulating the operational validity status flag and error diagnostics data.</returns>
public delegate ValidationResult ValidationDelegate(TerminalScreen screen, string currentInput);

