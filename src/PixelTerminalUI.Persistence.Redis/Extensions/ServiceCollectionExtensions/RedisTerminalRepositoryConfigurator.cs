using Microsoft.Extensions.DependencyInjection;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.Persistence.Redis.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Widgets;
using PixelTerminalUI.Persistence.Redis.Configuration;

namespace PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;

/// <summary>
/// Concrete orchestration controller entity executing real-time micro-registrations within the IoC context scope.
/// </summary>
internal sealed class RedisTerminalRepositoryConfigurator : IRedisRepositoryConfigurator
{
    private readonly IServiceCollection _services;
    private readonly RedisCacheOptions _cacheOptions;
    private readonly RedisJsonTypeResolver _typeResolver;

    public RedisTerminalRepositoryConfigurator(IServiceCollection services)
    {
        _services = services;
        _cacheOptions = new RedisCacheOptions();
        _typeResolver = new RedisJsonTypeResolver();

        // Register default system-level base screens polymorphic architecture hierarchy components
        _typeResolver.RegisterScreen<SimpleMessageScreen>();

        // Register default system-level presentation layout widgets block entities
        _typeResolver.RegisterWidget<PasswordEntryWidget>();
        _typeResolver.RegisterWidget<TextEntryWidget>();

        // Immediately secure root default entities mapping context layouts
        _services.AddSingleton(_cacheOptions);
        _services.AddScoped<ITerminalSessionRepository, RedisTerminalSessionRepository>();

        // Re-register options dynamically using standard lazy factory evaluation routines
        _services.AddSingleton(provider => _typeResolver.CreateOptions());
    }

    /// <inheritdoc />
    public IRedisRepositoryConfigurator WithSessionTimeout(TimeSpan timeout)
    {
        _cacheOptions.SessionTtl = timeout;
        return this;
    }

    /// <inheritdoc />
    public IRedisRepositoryConfigurator RegisterCustomScreens(Action<RedisJsonTypeResolver> configure)
    {
        configure(_typeResolver);
        return this;
    }
}
