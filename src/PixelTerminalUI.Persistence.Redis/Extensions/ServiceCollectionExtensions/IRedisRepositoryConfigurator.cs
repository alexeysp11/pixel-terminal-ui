using PixelTerminalUI.Persistence.Redis.Configuration;

namespace PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Defines fluent API contract operations available during data storage initialization pipeline stages.
/// </summary>
public interface IRedisRepositoryConfigurator
{
    /// <summary>
    /// Overrides the absolute sliding expiration duration threshold layout settings for active user sessions.
    /// </summary>
    IRedisRepositoryConfigurator WithSessionTimeout(TimeSpan timeout);

    /// <summary>
    /// Invokes downstream types resolution registries registration routines to append custom components layouts.
    /// </summary>
    IRedisRepositoryConfigurator RegisterCustomScreens(Action<RedisJsonTypeResolver> configure);
}
