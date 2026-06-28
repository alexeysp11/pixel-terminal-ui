using PixelTerminalUI.StatelessEngine.Screens;

namespace PixelTerminalUI.StatelessEngine.SymbolHandling;

/// <summary>
/// Defines the contract for processing macro sequences, navigation shortcuts, 
/// and specialized system control prefixes within the stateless user interface execution pipeline.
/// </summary>
public interface ISpecialSymbolHandler
{
    /// <summary>
    /// Evaluates the incoming user input against registered structural terminal shortcut patterns 
    /// to determine screen routing directives or focus shift execution layout modifications.
    /// </summary>
    /// <param name="screen">The currently active presentation layer terminal screen instance tracking execution state metadata.</param>
    /// <param name="userInput">The raw text sequence captured from the thin client interaction input boundary buffer frame.</param>
    /// <returns>A validated evaluation state envelope containing structural engine execution routing instructions.</returns>
    SymbolHandlingResult HandleSymbol(TerminalScreen screen, string userInput);
}
