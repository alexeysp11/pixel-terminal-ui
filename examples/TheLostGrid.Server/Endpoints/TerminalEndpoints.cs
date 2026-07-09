namespace TheLostGrid.Server.Endpoints;

/// <summary>
/// Implements the endpoint module contract to expose terminal interaction routing channels.
/// </summary>
public sealed class TerminalEndpoints : IEndpointModule
{
    /// <summary>
    /// Registers target gRPC service routing paths within the web application execution pipeline.
    /// </summary>
    /// <param name="app">The continuous application route boundary builder configuration instance.</param>
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGrpcService<TerminalGrpcService>();
    }
}
