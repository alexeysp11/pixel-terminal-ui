using TheLostGrid.Server.Endpoints;

namespace TheLostGrid.Server.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddModuleEndpoints(this IServiceCollection services)
    {
        // Scan the executing web api assembly to find all classes implementing our modules contract
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

    public static void MapModuleEndpoints(this WebApplication app)
    {
        IEnumerable<IEndpointModule> modules = app.Services.GetServices<IEndpointModule>();
        foreach (IEndpointModule module in modules)
        {
            module.MapEndpoints(app);
        }
    }
}
