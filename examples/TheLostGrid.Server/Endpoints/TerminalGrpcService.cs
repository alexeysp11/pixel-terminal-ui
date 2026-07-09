using Grpc.Core;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.StatelessEngine.RequestPipeline;
using PixelTerminalUI.Transport.Grpc;

namespace TheLostGrid.Server.Endpoints;

public sealed class TerminalGrpcService(IRequestPipelineHandler pipelineHandler) : ITerminalService
{
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
