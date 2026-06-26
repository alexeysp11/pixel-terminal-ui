using Testcontainers.Redis;

namespace PixelTerminalUI.Persistence.Redis.Tests.Fixtures;

public sealed class RedisTestFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:alpine").Build();
    public string ConnectionString => _redisContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }
}
