using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.StatelessEngine.RequestPipeline;

namespace TheLostGrid.Server.Endpoints;

/// <summary>
/// Implements the endpoint module contract to expose terminal interaction routing channels.
/// </summary>
public sealed class TerminalEndpoints : IEndpointModule
{
    /// <summary>
    /// Registers target HTTP routing paths and maps specific terminal input processing logic handlers.
    /// </summary>
    /// <param name="app">The continuous application route boundary builder configuration instance.</param>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/terminal/input", HandleTerminalInputAsync)
           .WithName("ProcessTerminalInput")
           .WithOpenApi();
    }

    /// <summary>
    /// Processes incoming textual input streams passing state execution context to the underlying pipeline.
    /// </summary>
    /// <param name="request">The structural request frame carrying active session tokens and user inputs.</param>
    /// <param name="pipelineHandler">The operational core handling distributed backend presentation state transitions.</param>
    /// <returns>An asynchronous task wrapping the computed presentation result or an error payload container.</returns>
    public static async Task<IResult> HandleTerminalInputAsync(
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
