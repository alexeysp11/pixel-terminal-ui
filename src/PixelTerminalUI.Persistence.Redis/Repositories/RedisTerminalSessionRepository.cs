using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.Persistence.Redis.Configuration;

namespace PixelTerminalUI.Persistence.Redis.Repositories;

/// <summary>
/// Provides a high-performance Redis Hash-backed implementation for managing stateless terminal session lifecycle states.
/// </summary>
public sealed class RedisTerminalSessionRepository(
    ILogger<RedisTerminalSessionRepository> logger,
    IConnectionMultiplexer redis,
    JsonSerializerOptions jsonOptions,
    RedisCacheOptions cacheOptions) : ITerminalSessionRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    private static string GetSessionKey(Guid sessionId) => $"session:{sessionId}";
    private static string GetScreenField(Guid screenId) => $"screen:{screenId}";

    private const string ActiveScreenIdField = "active_screen_id";
    private const string HistoricalBufferField = "historical_buffer";
    private const string VersionField = "version";

    /// <inheritdoc />
    public async Task<TerminalScreen?> GetActiveScreenAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        string sessionKey = GetSessionKey(sessionId);
        RedisValue activeScreenIdValue = await _database.HashGetAsync(sessionKey, ActiveScreenIdField);

        if (activeScreenIdValue.IsNullOrEmpty)
        {
            return null;
        }

        Guid activeScreenId = Guid.Parse(activeScreenIdValue.ToString());
        RedisValue screenJson = await _database.HashGetAsync(sessionKey, GetScreenField(activeScreenId));

        return screenJson.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TerminalScreen>(screenJson.ToString(), jsonOptions);
    }

    /// <inheritdoc />
    public async Task<TerminalScreen?> GetScreenByIdAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default)
    {
        RedisValue screenJson = await _database.HashGetAsync(GetSessionKey(sessionId), GetScreenField(screenId));
        return screenJson.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TerminalScreen>(screenJson.ToString(), jsonOptions);
    }

    /// <inheritdoc />
    public async Task SaveActiveScreenAsync(Guid sessionId, TerminalScreen screen, CancellationToken cancellationToken = default)
    {
        string sessionKey = GetSessionKey(sessionId);
        string screenField = GetScreenField(screen.Id);
        string screenJson = JsonSerializer.Serialize(screen, jsonOptions);

        ITransaction tran = _database.CreateTransaction();
        tran.AddCondition(Condition.HashExists(sessionKey, VersionField));
        _ = tran.HashSetAsync(sessionKey, [new(ActiveScreenIdField, screen.Id.ToString()), new(screenField, screenJson)]);
        _ = tran.HashIncrementAsync(sessionKey, VersionField, 1);

        if (!await tran.ExecuteAsync())
        {
            await _database.HashSetAsync(sessionKey, [
                new(VersionField, 1),
                new(ActiveScreenIdField, screen.Id.ToString()),
                new(screenField, screenJson)
            ]);
        }

        await _database.KeyExpireAsync(sessionKey, cacheOptions.SessionTtl);
    }

    /// <inheritdoc />
    public async Task RemoveScreenAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default)
    {
        await _database.HashDeleteAsync(GetSessionKey(sessionId), GetScreenField(screenId));
    }

    /// <inheritdoc />
    public async Task<uint[]?> GetHistoricalBufferAsync(Guid sessionId)
    {
        try
        {
            string sessionKey = GetSessionKey(sessionId);
            RedisValue bufferJson = await _database.HashGetAsync(sessionKey, HistoricalBufferField);
            return bufferJson.IsNullOrEmpty ? null : JsonSerializer.Deserialize<uint[]>(bufferJson.ToString(), jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load buffer for terminal session {SessionId}", sessionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveHistoricalBufferAsync(Guid sessionId, uint[] currentBuffer)
    {
        try
        {
            string sessionKey = GetSessionKey(sessionId);
            string bufferJson = JsonSerializer.Serialize(currentBuffer, jsonOptions);
            ITransaction transaction = _database.CreateTransaction();
            _ = transaction.HashSetAsync(sessionKey, [new HashEntry(HistoricalBufferField, bufferJson)]);
            _ = transaction.HashIncrementAsync(sessionKey, VersionField, 1);
            await transaction.ExecuteAsync();

            await _database.KeyExpireAsync(sessionKey, cacheOptions.SessionTtl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save historical buffer representation for terminal session {SessionId}", sessionId);
            throw;
        }
    }
}
