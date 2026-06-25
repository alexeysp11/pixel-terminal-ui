using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.StatelessEngine.RequestPipeline;

namespace TheLostGrid.Server.Endpoints;

public sealed class TerminalEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/terminal/input", HandleTerminalInputAsync)
           .WithName("ProcessTerminalInput")
           .WithOpenApi();
    }

    private static async Task<IResult> HandleTerminalInputAsync(
        TerminalRequest request,
        IRequestPipelineHandler pipelineHandler)
    {
        try
        {
            TerminalResponse response = await pipelineHandler.HandleInputAsync(request);
            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
