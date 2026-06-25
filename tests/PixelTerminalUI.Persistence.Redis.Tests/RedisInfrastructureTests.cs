using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PixelTerminalUI.Persistence.Redis.Extensions.ServiceCollectionExtensions;
using PixelTerminalUI.Persistence.Redis.Repositories;
using PixelTerminalUI.StatelessEngine.Screens;
using StackExchange.Redis;

namespace PixelTerminalUI.Persistence.Redis.Tests;

[Collection(nameof(RedisCollection))]
public sealed class RedisInfrastructureTests(RedisTestFixture fixture)
{
    [Fact]
    public void RedisJsonTypeResolver_ShouldCorrectlyAppendPolymorphicDiscriminators()
    {
        // Arrange
        RedisJsonTypeResolver resolver = new();
        resolver.RegisterScreen<FakeWelcomeScreen>();
        JsonSerializerOptions options = resolver.CreateOptions();
        TerminalScreen screen = new FakeWelcomeScreen { Id = Guid.NewGuid(), Name = "FakeWelcomeScreen", SessionId = Guid.NewGuid() };

        // Act
        string jsonResult = JsonSerializer.Serialize(screen, options);

        // Assert
        jsonResult.Should()
            .NotBeNullOrEmpty("because serializing derived types requires active configuration context")
            .And.Contain("\"$type\":\"FakeWelcomeScreen\"", "because JSON type discriminators must accurately represent concrete domain models");
    }

    [Fact]
    public void AddTerminalRedisRepository_ShouldSuccessfullyRegisterAllRequiredIoCDependencies()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        Mock<ILogger<RedisTerminalSessionRepository>> loggerMock = new();
        services.AddSingleton(loggerMock.Object);

        // Act
        services.AddTerminalRedisRepository(fixture.ConnectionString, custom =>
        {
            custom.RegisterScreen<FakeWelcomeScreen>();
        });
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IConnectionMultiplexer>()
            .Should()
            .NotBeNull("because active connection multiplexer instance should be present in the container structure");

        provider.GetService<JsonSerializerOptions>()
            .Should()
            .NotBeNull("because global serialization options are required to process nested system models");

        provider.GetService<StatelessEngine.Repositories.ITerminalSessionRepository>()
            .Should()
            .NotBeNull("because the core abstract repository binding mapping must be successfully resolved");
    }

    [Fact]
    public async Task RedisTerminalSessionRepository_ShouldPersistAndRetrieveActiveScreenAccurately()
    {
        // Arrange
        RedisJsonTypeResolver resolver = new();
        resolver.RegisterScreen<FakeWelcomeScreen>();
        JsonSerializerOptions options = resolver.CreateOptions();

        IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(fixture.ConnectionString);
        Mock<ILogger<RedisTerminalSessionRepository>> loggerMock = new();

        RedisTerminalSessionRepository repository = new(loggerMock.Object, multiplexer, options);

        Guid sessionId = Guid.NewGuid();
        TerminalScreen initialScreen = new FakeWelcomeScreen { Id = Guid.NewGuid(), Name = "FakeWelcomeScreen", SessionId = sessionId };

        // Act
        await repository.SaveActiveScreenAsync(sessionId, initialScreen);
        TerminalScreen? restoredScreen = await repository.GetActiveScreenAsync(sessionId);

        // Assert
        restoredScreen.Should()
            .NotBeNull("because a successfully saved screen must be retrievable from the state storage layer")
            .And.BeOfType<FakeWelcomeScreen>("because type resolver should properly reconstruct the target instance runtime type");

        restoredScreen!.Id.Should()
            .Be(initialScreen.Id, "because exact unique identifier property values must remain consistent across network transfers");
    }

    [Fact]
    public async Task RedisTerminalSessionRepository_ShouldManageHistoricalBuffersDirectly()
    {
        // Arrange
        RedisJsonTypeResolver resolver = new();
        JsonSerializerOptions options = resolver.CreateOptions();
        IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(fixture.ConnectionString);
        Mock<ILogger<RedisTerminalSessionRepository>> loggerMock = new();

        RedisTerminalSessionRepository repository = new(loggerMock.Object, multiplexer, options);
        Guid sessionId = Guid.NewGuid();
        uint[] mockBuffer = [12, 14, 16];

        // Act
        await repository.SaveHistoricalBufferAsync(sessionId, mockBuffer);
        uint[]? restoredBuffer = await repository.GetHistoricalBufferAsync(sessionId);

        // Assert
        restoredBuffer.Should()
            .NotBeNull("because saved historical graphic frame array blocks should be present inside the storage structure")
            .And.HaveCount(mockBuffer.Length, "because the exact payload capacity scale should match across storage lifecycle stages")
            .And.ContainInOrder(mockBuffer, "because sequence index allocation values must remain strictly unchanged");
    }

    [Fact]
    public async Task RedisTerminalSessionRepository_ShouldHandleConcurrentWritesWithoutFailures()
    {
        // Arrange
        RedisJsonTypeResolver resolver = new();
        resolver.RegisterScreen<FakeWelcomeScreen>().RegisterScreen<FakeGameScreen>();
        JsonSerializerOptions options = resolver.CreateOptions();
        IConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(fixture.ConnectionString);
        Mock<ILogger<RedisTerminalSessionRepository>> loggerMock = new();

        RedisTerminalSessionRepository repositoryA = new(loggerMock.Object, multiplexer, options);
        RedisTerminalSessionRepository repositoryB = new(loggerMock.Object, multiplexer, options);

        Guid sessionId = Guid.NewGuid();
        TerminalScreen baseScreen = new FakeWelcomeScreen { Id = Guid.NewGuid(), Name = "FakeWelcomeScreen", SessionId = sessionId };

        // Seed initial session entry context state
        await repositoryA.SaveActiveScreenAsync(sessionId, baseScreen);

        // Act
        Func<Task> concurrentWriteAct = async () =>
        {
            List<Task> tasks = [];

            // Flood the repository with parallel requests to ensure it processes them gracefully
            for (int i = 0; i < 20; i++)
            {
                TerminalScreen concurrentScreen = new FakeGameScreen { Id = Guid.NewGuid(), Name = "FakeGameScreen", SessionId = sessionId };
                tasks.Add(repositoryA.SaveActiveScreenAsync(sessionId, concurrentScreen));
                tasks.Add(repositoryB.SaveActiveScreenAsync(sessionId, concurrentScreen));
            }

            await Task.WhenAll(tasks);
        };

        // Assert
        await concurrentWriteAct.Should()
            .NotThrowAsync("because the Redis-backed session pipeline must process rapid parallel streams atomically and gracefully");

        TerminalScreen? finalScreen = await repositoryA.GetActiveScreenAsync(sessionId);
        finalScreen.Should()
            .NotBeNull("because despite concurrent race conditions, the final state document must be securely persisted")
            .And.BeOfType<FakeGameScreen>("because the last resolved state write execution wins and overrides previous values");
    }
}
