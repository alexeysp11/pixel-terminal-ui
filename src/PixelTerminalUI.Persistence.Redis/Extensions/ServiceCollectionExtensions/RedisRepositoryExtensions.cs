using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Text.Json;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.Persistence.Redis.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;

namespace PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;

public static class RedisRepositoryExtensions
{
    public static IServiceCollection AddTerminalRedisRepository(
        this IServiceCollection services,
        string connectionString,
        Action<RedisJsonTypeResolver>? configureCustomScreens = null)
    {
        RedisJsonTypeResolver typeResolver = new();

        // Register system-level base screens polymorphic architecture hierarchy
        typeResolver.RegisterScreen<SimpleMessageScreen>();

        // Register system-level widgets polymorphic architecture hierarchy
        typeResolver.RegisterWidget<PasswordEntryWidget>();
        typeResolver.RegisterWidget<TextEntryWidget>();

        // Allow consumer application layer to append downstream concrete entities
        if (configureCustomScreens != null)
        {
            configureCustomScreens(typeResolver);
        }

        // Build and register global immutable serializer configurations
        JsonSerializerOptions jsonOptions = typeResolver.CreateOptions();
        services.AddSingleton(jsonOptions);

        // Standardize high-performance low-level connection multiplexing lifecycles
        IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(connectionString);
        services.AddSingleton(multiplexer);

        // Core persistence worker mapping binding layer
        services.AddScoped<ITerminalSessionRepository, RedisTerminalSessionRepository>();

        return services;
    }
}
