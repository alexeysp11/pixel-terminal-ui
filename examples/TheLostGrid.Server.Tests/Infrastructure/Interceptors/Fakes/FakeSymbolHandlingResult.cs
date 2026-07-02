using PixelTerminalUI.StatelessEngine.SymbolHandling;

namespace TheLostGrid.Server.Tests.Infrastructure.Interceptors.Fakes;

public readonly record struct FakeSymbolHandlingResult
{
    public bool IsHandled { get; init; }
    public static SymbolHandlingResult RefreshActiveScreen() => new() { Action = SymbolResultActionType.RefreshActiveScreen };
    public static SymbolHandlingResult NotHandled() => new() { Action = SymbolResultActionType.NotHandled };
}
