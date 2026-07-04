namespace TheLostGrid.Server.Endpoints;

/// <summary>
/// Defines the architectural contract layout required to satisfy the endpoint routing discovery engine.
/// </summary>
public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
