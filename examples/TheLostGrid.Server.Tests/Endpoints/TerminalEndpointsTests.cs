using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Moq;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.StatelessEngine.RequestPipeline;
using TheLostGrid.Server.Endpoints;
using FluentAssertions;

namespace TheLostGrid.Server.Tests.Endpoints;

public sealed class TerminalEndpointsTests
{
    private readonly Mock<IRequestPipelineHandler> _pipelineHandlerMock = new();

    [Fact]
    public async Task HandleTerminalInputAsync_ShouldReturnOkWithResponse_WhenPipelineExecutesSuccessfully()
    {
        // Arrange
        TerminalRequest testRequest = new(Guid.NewGuid(), "0");
        uint[] bufferData = [1, 2, 3, 4];
        FullFrameResponse expectedResponse = new(Guid.NewGuid(), bufferData, 15, 8);

        _pipelineHandlerMock
            .Setup(p => p.HandleInputAsync(testRequest))
            .ReturnsAsync(expectedResponse);

        // Act
        IResult actionResult = await TerminalEndpoints.HandleTerminalInputAsync(testRequest, _pipelineHandlerMock.Object);

        // Assert
        Ok<TerminalResponse> okResult = Assert.IsType<Ok<TerminalResponse>>(actionResult);

        okResult.Value
            .Should()
            .NotBeNull("because a successful pipeline pipeline execution must provide a valid presentation interface payload frame");

        FullFrameResponse concreteResponse = Assert.IsType<FullFrameResponse>(okResult.Value);

        concreteResponse.ScreenBuffer
            .Should()
            .Equal(expectedResponse.ScreenBuffer, "because the outbound network API channel boundary must retain identical presentation layer bits layout data structures");
    }

    [Fact]
    public async Task HandleTerminalInputAsync_ShouldReturn400BadRequest_WhenBusinessValidationFails()
    {
        // Arrange
        TerminalRequest testRequest = new(Guid.NewGuid(), "INVALID_CMD");
        string failureMessage = "NOT ENOUGH ENERGY (30 ENG REQUIRED)";

        _pipelineHandlerMock
            .Setup(p => p.HandleInputAsync(testRequest))
            .ThrowsAsync(new InvalidOperationException(failureMessage));

        // Act
        IResult actionResult = await TerminalEndpoints.HandleTerminalInputAsync(testRequest, _pipelineHandlerMock.Object);

        // Assert
        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(actionResult);

        problemResult.StatusCode
            .Should()
            .Be(StatusCodes.Status400BadRequest, "because standard internal domain processing exceptions convert down to direct business input rejections");

        problemResult.ProblemDetails.Detail
            .Should()
            .Be(failureMessage, "because the specific structural processing failure parameters must guide the operational core interface feedback loop");
    }

    [Fact]
    public async Task HandleTerminalInputAsync_ShouldReturn500InternalServerError_WhenUncaughtExceptionOccurs()
    {
        // Arrange
        TerminalRequest testRequest = new(Guid.NewGuid(), "CRASH");
        string systemErrorMessage = "Redis connection timed out exception frame error.";

        _pipelineHandlerMock
            .Setup(p => p.HandleInputAsync(testRequest))
            .ThrowsAsync(new Exception(systemErrorMessage));

        // Act
        IResult actionResult = await TerminalEndpoints.HandleTerminalInputAsync(testRequest, _pipelineHandlerMock.Object);

        // Assert
        ProblemHttpResult problemResult = Assert.IsType<ProblemHttpResult>(actionResult);

        problemResult.StatusCode
            .Should()
            .Be(StatusCodes.Status500InternalServerError, "because unexpected structural infrastructure failures require fallback defensive boundary isolation mapping blocks");

        problemResult.ProblemDetails.Detail
            .Should()
            .Be(systemErrorMessage, "because deep technical diagnostics must propagate through debug pipelines to satisfy diagnostic telemetry capture targets");
    }
}
