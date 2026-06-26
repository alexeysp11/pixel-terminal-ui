using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Service collection extension entries for streamlined and readable Redis infrastructure bootstrap.
/// </summary>
public static class RedisRepositoryExtensions
{
    /// <summary>
    /// Starts the configuration pipeline for the high-performance Redis state persistence layer.
    /// </summary>
    public static IRedisRepositoryConfigurator AddTerminalRedisRepository(this IServiceCollection services, string connectionString)
    {
        IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(connectionString);
        services.AddSingleton(multiplexer);

        RedisTerminalRepositoryConfigurator configurator = new(services);
        return configurator;
    }
}
