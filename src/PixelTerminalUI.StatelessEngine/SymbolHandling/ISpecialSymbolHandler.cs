using PixelTerminalUI.StatelessEngine.Screens;

namespace PixelTerminalUI.StatelessEngine.SymbolHandling;

public interface ISpecialSymbolHandler
{
    SymbolHandlingResult HandleSymbol(TerminalScreen screen, string userInput);
}
