using FluentAssertions;
using Moq;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Tests.SymbolHandling.Fakes;

namespace PixelTerminalUI.StatelessEngine.Tests.Commands;

public sealed class StubCommandTests
{
    [Theory]
    [InlineData(StubCommandState.Initial, 0)]
    [InlineData(StubCommandState.Processing, 1)]
    [InlineData(StubCommandState.Completed, 2)]
    [InlineData(StubCommandState.Failed, 99)]
    public void RawState_PropertyBitwiseMapping_ShouldPackEnumToIntCorrectly(StubCommandState sourceState, int expectedRawValue)
    {
        // Arrange
        StubCommand command = new();

        // Act
        command.State = sourceState;
        int packedResult = command.RawState;

        // Assert
        packedResult.Should().Be(expectedRawValue,
            "because Unsafe.As must map the underlying enum memory bits directly to an integer representation");
    }

    [Theory]
    [InlineData(0, StubCommandState.Initial)]
    [InlineData(1, StubCommandState.Processing)]
    [InlineData(2, StubCommandState.Completed)]
    [InlineData(99, StubCommandState.Failed)]
    public void RawState_PropertyBitwiseMapping_ShouldUnpackIntToEnumCorrectly(int sourceRawValue, StubCommandState expectedState)
    {
        // Arrange
        StubCommand command = new();

        // Act
        command.RawState = sourceRawValue;
        StubCommandState unpackedResult = command.State;

        // Assert
        unpackedResult.Should().Be(expectedState,
            "because Unsafe.As must reconstruct the strongly-typed enum from its raw integer state storage");
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvoked_ShouldPassContextAndExecuteUnderlyingBusinessWorkflow()
    {
        // Arrange
        StubCommand command = new();

        // Mocking the command context using Moq
        Mock<ICommandContext> contextMock = new();
        contextMock.Setup(c => c.InputValue).Returns("TEST-SCAN-123");
        contextMock.Setup(c => c.SessionId).Returns(Guid.NewGuid());

        // Act
        bool executionResult = await command.ExecuteAsync(contextMock.Object);

        // Assert
        executionResult.Should().BeTrue("because the stub command is hardcoded to return true upon completion");
        command.WasExecuted.Should().BeTrue("because the pipeline handler triggers the virtual execution loop");
        command.CapturedContext.Should().NotBeNull();
        command.CapturedContext!.InputValue.Should().Be("TEST-SCAN-123");
    }
}
