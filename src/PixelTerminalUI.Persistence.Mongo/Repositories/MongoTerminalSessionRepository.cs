using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PixelTerminalUI.Persistence.Mongo.Entities;
using PixelTerminalUI.StatelessEngine.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;

namespace PixelTerminalUI.Persistence.Mongo.Repositories;

public sealed class MongoTerminalSessionRepository(
    ILogger<MongoTerminalSessionRepository> logger,
    IMongoDatabase database) : ITerminalSessionRepository
{
    private readonly IMongoCollection<TerminalSessionEntity> _collection
        = database.GetCollection<TerminalSessionEntity>("terminal_sessions");

    private readonly IMongoCollection<TerminalScreen> _screensCollection
        = database.GetCollection<TerminalScreen>("active_screens");

    public async Task<TerminalScreen?> GetActiveScreenAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            FilterDefinition<TerminalSessionEntity> filter = Builders<TerminalSessionEntity>.Filter.Eq(s => s.SessionId, sessionId);
            ProjectionDefinition<TerminalSessionEntity> projection = Builders<TerminalSessionEntity>.Projection.Include(s => s.ActiveScreenId);

            TerminalSessionEntity session = await _collection.Find(filter)
                .Project<TerminalSessionEntity>(projection)
                .FirstOrDefaultAsync(cancellationToken);

            if (session == null)
            {
                logger.LogDebug("Active session checkpoint not found for identifier: {SessionId}", sessionId);
                return null;
            }

            return await _screensCollection.Find(f => f.Id == session.ActiveScreenId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve active screen document for Session {SessionId} due to a database infrastructure exception", sessionId);
            throw;
        }
    }

    public async Task<TerminalScreen?> GetScreenByIdAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _screensCollection.Find(f => f.Id == screenId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch standalone screen document {ScreenId} for Session {SessionId} from the database tracking index", screenId, sessionId);
            throw;
        }
    }

    public async Task SaveActiveScreenAsync(Guid sessionId, TerminalScreen screen, CancellationToken cancellationToken = default)
    {
        try
        {
            FilterDefinitionBuilder<TerminalSessionEntity> filterBuilder = Builders<TerminalSessionEntity>.Filter;
            FilterDefinition<TerminalSessionEntity> sessionFilter = filterBuilder.Eq(s => s.SessionId, sessionId);

            // Fetch current session to know the exact version before making any mutations
            TerminalSessionEntity existingSession = await _collection.Find(sessionFilter).FirstOrDefaultAsync(cancellationToken);

            int currentVersion = existingSession?.Version ?? 0;

            ReplaceOptions upsertOptions = new() { IsUpsert = true };

            // Save the screen document first
            await _screensCollection.ReplaceOneAsync(
                f => f.Id == screen.Id,
                screen,
                upsertOptions,
                cancellationToken);

            if (existingSession == null)
            {
                // Initial cold start session creation
                TerminalSessionEntity newSession = new()
                {
                    SessionId = sessionId,
                    ActiveScreenId = screen.Id,
                    Version = 1,
                    UpdatedAt = DateTime.UtcNow
                };
                await _collection.InsertOneAsync(newSession, null, cancellationToken);
                return;
            }

            // Guard the pointer update with strict OCC version matching
            FilterDefinition<TerminalSessionEntity> updateFilter = filterBuilder.And(
                filterBuilder.Eq(s => s.SessionId, sessionId),
                filterBuilder.Eq(s => s.Version, currentVersion)
            );

            UpdateDefinition<TerminalSessionEntity> update = Builders<TerminalSessionEntity>.Update
                .Set(s => s.ActiveScreenId, screen.Id)
                .Set(s => s.UpdatedAt, DateTime.UtcNow)
                .Inc(s => s.Version, 1);

            UpdateResult result = await _collection.UpdateOneAsync(updateFilter, update, null, cancellationToken);

            if (result.MatchedCount == 0)
            {
                logger.LogWarning("Concurrency write conflict encountered inside screen pointer update for session: {SessionId}", sessionId);
                throw new InvalidOperationException($"Concurrency conflict detected during screen swap for session {sessionId}. Expected version: {currentVersion}.");
            }

            logger.LogTrace("Saved standalone screen {ScreenId} and updated active pointer for session {SessionId}", screen.Id, sessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save active screen document {ScreenId} for Session {SessionId}", screen.Id, sessionId);
            throw;
        }
    }

    public async Task RemoveScreenAsync(Guid sessionId, Guid screenId, CancellationToken cancellationToken = default)
    {
        try
        {
            DeleteResult result = await _screensCollection.DeleteOneAsync(f => f.Id == screenId, cancellationToken);

            if (result.DeletedCount > 0)
            {
                logger.LogDebug("Successfully purged standalone screen {ScreenId} out of MongoDB active_screens collection", screenId);
            }
            else
            {
                logger.LogWarning("Purge operation completed with zero deleted documents for Screen {ScreenId} within Session {SessionId}", screenId, sessionId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to completely purge screen document {ScreenId} out of the active_screens session track for Session {SessionId}", screenId, sessionId);
            throw;
        }
    }

    public async Task<uint[]?> GetHistoricalBufferAsync(Guid sessionId)
    {
        try
        {
            FilterDefinition<TerminalSessionEntity> filter = Builders<TerminalSessionEntity>.Filter.Eq(s => s.SessionId, sessionId);
            ProjectionDefinition<TerminalSessionEntity> projection = Builders<TerminalSessionEntity>.Projection.Include(s => s.HistoricalBuffer);

            TerminalSessionEntity entity = await _collection.Find(filter)
                .Project<TerminalSessionEntity>(projection)
                .FirstOrDefaultAsync();

            return entity?.HistoricalBuffer;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load cached historical buffer data stream array for Session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task SaveHistoricalBufferAsync(Guid sessionId, uint[] currentBuffer)
    {
        try
        {
            FilterDefinitionBuilder<TerminalSessionEntity> filterBuilder = Builders<TerminalSessionEntity>.Filter;
            FilterDefinition<TerminalSessionEntity> initialFilter = filterBuilder.Eq(s => s.SessionId, sessionId);
            TerminalSessionEntity existingSession = await _collection.Find(initialFilter).FirstOrDefaultAsync();

            if (existingSession == null)
            {
                TerminalSessionEntity newEntity = new()
                {
                    SessionId = sessionId,
                    HistoricalBuffer = currentBuffer,
                    Version = 1,
                    UpdatedAt = DateTime.UtcNow
                };

                await _collection.InsertOneAsync(newEntity);
                return;
            }

            int currentVersion = existingSession.Version;
            FilterDefinition<TerminalSessionEntity> updateFilter = filterBuilder.And(
                filterBuilder.Eq(s => s.SessionId, sessionId),
                filterBuilder.Eq(s => s.Version, currentVersion)
            );

            TerminalSessionEntity updatedEntity = new()
            {
                SessionId = sessionId,
                HistoricalBuffer = currentBuffer,
                ActiveScreenId = existingSession.ActiveScreenId,
                Version = currentVersion + 1,
                UpdatedAt = DateTime.UtcNow
            };

            ReplaceOneResult result = await _collection.ReplaceOneAsync(updateFilter, updatedEntity, new ReplaceOptions());

            if (result.MatchedCount == 0)
            {
                throw new InvalidOperationException($"Concurrency conflict detected for session {sessionId}. Expected version: {currentVersion}.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist historical frame buffer for Session {SessionId}", sessionId);
            throw;
        }
    }
}
