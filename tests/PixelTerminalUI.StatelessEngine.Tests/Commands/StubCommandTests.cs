using FluentAssertions;
using Moq;
using PixelTerminalUI.StatelessEngine.Commands.CommandContexts;
using PixelTerminalUI.StatelessEngine.Tests.Commands.Fakes;

namespace PixelTerminalUI.StatelessEngine.Tests.Commands;

public sealed class StubCommandTests
{
    [Theory]
    [InlineData(StubTestingCommandState.Initial, 0)]
    [InlineData(StubTestingCommandState.Processing, 1)]
    [InlineData(StubTestingCommandState.Completed, 2)]
    [InlineData(StubTestingCommandState.Failed, 99)]
    public void RawState_PropertyBitwiseMapping_ShouldPackEnumToIntCorrectly(StubTestingCommandState sourceState, int expectedRawValue)
    {
        // Arrange
        StubTestingCommand command = new();

        // Act
        command.State = sourceState;
        int packedResult = command.RawState;

        // Assert
        packedResult.Should().Be(expectedRawValue,
            "because Unsafe.As must map the underlying enum memory bits directly to an integer representation");
    }

    [Theory]
    [InlineData(0, StubTestingCommandState.Initial)]
    [InlineData(1, StubTestingCommandState.Processing)]
    [InlineData(2, StubTestingCommandState.Completed)]
    [InlineData(99, StubTestingCommandState.Failed)]
    public void RawState_PropertyBitwiseMapping_ShouldUnpackIntToEnumCorrectly(int sourceRawValue, StubTestingCommandState expectedState)
    {
        // Arrange
        StubTestingCommand command = new();

        // Act
        command.RawState = sourceRawValue;
        StubTestingCommandState unpackedResult = command.State;

        // Assert
        unpackedResult.Should().Be(expectedState,
            "because Unsafe.As must reconstruct the strongly-typed enum from its raw integer state storage");
    }

    [Fact]
    public async Task ExecuteAsync_WhenInvoked_ShouldPassContextAndExecuteUnderlyingBusinessWorkflow()
    {
        // Arrange
        StubTestingCommand command = new();

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
