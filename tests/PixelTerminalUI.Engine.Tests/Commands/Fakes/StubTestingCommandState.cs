namespace PixelTerminalUI.Engine.Tests.Commands.Fakes;

/// <summary>
/// A local enum designed strictly to test the framework's generic type constraints and memory packing.
/// </summary>
public enum StubTestingCommandState
{
    Initial = 0,
    Processing = 1,
    Completed = 2,
    Failed = 99
}
