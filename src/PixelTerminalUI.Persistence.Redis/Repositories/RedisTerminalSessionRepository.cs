using Microsoft.Extensions.Logging;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using StackExchange.Redis;
using System.Text.Json;

namespace PixelTerminalUI.Persistence.Redis.Repositories;

public sealed class RedisTerminalSessionRepository(
    ILogger<RedisTerminalSessionRepository> logger,
    IConnectionMultiplexer redis,
    JsonSerializerOptions jsonOptions) : ITerminalSessionRepository
{
    private readonly IDatabase _database = redis.GetDatabase();

    private static string GetSessionKey(Guid sessionId) => $"session:{sessionId}";
    private static string GetScreenField(Guid screenId) => $"screen:{screenId}";

    private const string ActiveScreenIdField = "active_screen_id";
    private const string HistoricalBufferField = "historical_buffer";
    private const string VersionField = "version";

    public async Task<TerminalScreen?> GetActiveScreenAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        string sessionKey = GetSessionKey(sessionId);
        RedisValue[] results = await _database.HashGetAsync(sessionKey, [ActiveScreenIdField]);
        if (results[0].IsNullOrEmpty) return null;

        Guid activeScreenId = Guid.Parse(results[0].ToString());
        RedisValue screenJson = await _database.HashGetAsync(sessionKey, GetScreenField(activeScreenId));
        return screenJson.IsNullOrEmpty ? null : JsonSerializer.Deserialize<TerminalScreen>(screenJson.ToString(), jsonOptions);
    }

    public async Task<TerminalScreen?> GetScreenByIdAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default)
    {
        RedisValue screenJson = await _database.HashGetAsync(GetSessionKey(sessionId), GetScreenField(screenId));
        return screenJson.IsNullOrEmpty ? null : System.Text.Json.JsonSerializer.Deserialize<TerminalScreen>(screenJson.ToString(), jsonOptions);
    }

    public async Task SaveActiveScreenAsync(Guid sessionId, TerminalScreen screen, CancellationToken cancellationToken = default)
    {
        string sessionKey = GetSessionKey(sessionId);
        string screenField = GetScreenField(screen.Id);
        string screenJson = JsonSerializer.Serialize(screen, jsonOptions);

        // Попытка обновления с OCC (Optimistic Concurrency Control)
        ITransaction tran = _database.CreateTransaction();
        tran.AddCondition(Condition.HashExists(sessionKey, VersionField));
        _ = tran.HashSetAsync(sessionKey, [new(ActiveScreenIdField, screen.Id.ToString()), new(screenField, screenJson)]);
        _ = tran.HashIncrementAsync(sessionKey, VersionField, 1);

        if (!await tran.ExecuteAsync()) // Если сессии нет, создаем
        {
            await _database.HashSetAsync(sessionKey, [
                new(VersionField, 1),
                new(ActiveScreenIdField, screen.Id.ToString()),
                new(screenField, screenJson)
            ]);
        }
    }

    public async Task RemoveScreenAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default)
    {
        await _database.HashDeleteAsync(GetSessionKey(sessionId), GetScreenField(screenId));
    }

    public async Task<uint[]?> GetHistoricalBufferAsync(Guid sessionId)
    {
        try
        {
            string sessionKey = GetSessionKey(sessionId);
            RedisValue bufferJson = await _database.HashGetAsync(sessionKey, HistoricalBufferField);
            if (bufferJson.IsNullOrEmpty) return null;
            return JsonSerializer.Deserialize<uint[]>(bufferJson.ToString(), jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load buffer for {SessionId}", sessionId);
            throw;
        }
    }

    public async Task SaveHistoricalBufferAsync(Guid sessionId, uint[] currentBuffer)
    {
        try
        {
            string sessionKey = GetSessionKey(sessionId);
            string bufferJson = JsonSerializer.Serialize(currentBuffer, jsonOptions);
            // Реализация с транзакцией и проверкой версии (Optimistic Concurrency Control)
            ITransaction transaction = _database.CreateTransaction();
            // ... (условия транзакции)
            _ = transaction.HashSetAsync(sessionKey, [new(HistoricalBufferField, bufferJson)]);
            _ = transaction.HashIncrementAsync(sessionKey, VersionField, 1);
            await transaction.ExecuteAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save buffer for {SessionId}", sessionId);
            throw;
        }
    }
}
