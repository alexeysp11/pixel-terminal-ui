using TheLostGrid.Server.Endpoints;

namespace TheLostGrid.Server.Extensions;

/// <summary>
/// Provides extension methods for automated registration and routing of endpoint modules within the application pipeline.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Scans internal assembly structures to locate and register all non-abstract types implementing the endpoint modules contract.
    /// </summary>
    /// <param name="services">The core service collection instance container configured within the host builder.</param>
    /// <returns>The modified service collection framework instance to sustain continuous initialization chaining.</returns>
    public static IServiceCollection AddModuleEndpoints(this IServiceCollection services)
    {
        Type[] moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpointModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToArray();

        foreach (Type type in moduleTypes)
        {
            services.AddSingleton(typeof(IEndpointModule), type);
        }

        return services;
    }

    /// <summary>
    /// Resolves all instantiated endpoint modules from the application service provider container to map their respective routing frames.
    /// </summary>
    /// <param name="app">The active web application execution host instance driving the runtime context pipelines.</param>
    public static void MapModuleEndpoints(this WebApplication app)
    {
        IEnumerable<IEndpointModule> modules = app.Services.GetServices<IEndpointModule>();
        foreach (IEndpointModule module in modules)
        {
            module.MapEndpoints(app);
        }
    }
}
