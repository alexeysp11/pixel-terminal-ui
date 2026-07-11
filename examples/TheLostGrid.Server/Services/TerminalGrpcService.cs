using Grpc.Core;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.Engine.RequestPipeline;
using PixelTerminalUI.Transport.Grpc.Services;

namespace TheLostGrid.Server.Services;

/// <summary>
/// Provides the gRPC routing channel for terminal interaction streams.
/// </summary>
/// <param name="pipelineHandler">The operational core processing presentation state transitions.</param>
public sealed class TerminalGrpcService(IRequestPipelineHandler pipelineHandler) : ITerminalService
{
    /// <summary>
    /// Intercepts binary network request streams and executes target state mutation logic routines.
    /// </summary>
    /// <param name="request">The structural payload carrying user interactions metadata.</param>
    /// <returns>An asynchronous task wrapping the computed presentation layout matrix package.</returns>
    /// <exception cref="RpcException">Thrown when internal operation validation constraints are violated.</exception>
    public async ValueTask<TerminalResponse> ProcessTransactionAsync(TerminalRequest request)
    {
        try
        {
            TerminalResponse response = await pipelineHandler.HandleInputAsync(request);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            Status errorStatus = new(StatusCode.InvalidArgument, ex.Message);
            throw new RpcException(errorStatus);
        }
        catch (Exception ex)
        {
            Status errorStatus = new(StatusCode.Internal, ex.Message);
            throw new RpcException(errorStatus);
        }
    }
}
