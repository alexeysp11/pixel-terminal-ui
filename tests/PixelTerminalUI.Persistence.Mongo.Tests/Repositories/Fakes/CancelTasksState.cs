namespace PixelTerminalUI.Persistence.Mongo.Tests.Repositories.Fakes;

/// <summary>
/// A dummy state enum created strictly to test the custom polymorphic command lifecycle inside MongoDB.
/// </summary>
public enum CancelTasksState
{
    Undefined = 0,
    AwaitingUserInput = 1,
    Finalized = 2
}

