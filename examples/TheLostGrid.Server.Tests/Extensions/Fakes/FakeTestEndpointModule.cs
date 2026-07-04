using Microsoft.AspNetCore.Routing;
using TheLostGrid.Server.Endpoints;

namespace TheLostGrid.Server.Tests.Extensions.Fakes;

/// <summary>
/// Concrete test dummy implementation to fulfill scanning criteria during dependency injection reflection discovery cycles.
/// </summary>
public class FakeTestEndpointModule : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // Concrete test stub intentionally left empty to serve reflection scanning targets
    }
}
