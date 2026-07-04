namespace PixelTerminalUI.StatelessEngine.Tests.SymbolHandling.Fakes;

/// <summary>
/// A local enum designed strictly to test the framework's generic type constraints and memory packing.
/// </summary>
public enum StubSymbolCommandState
{
    Initial = 0,
    Processing = 1,
    Completed = 2,
    Failed = 99
}
