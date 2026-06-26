using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using StackExchange.Redis;
using PixelTerminalUI.StatelessEngine.Screens;
using PixelTerminalUI.Persistence.Redis.Repositories;
using PixelTerminalUI.Persistence.Mongo.Repositories;
using PixelTerminalUI.Persistence.Redis.Configuration;

namespace PixelTerminalUI.Benchmarks;

public abstract record BenchmarkScreenBase : TerminalScreen;
public sealed record BenchmarkWelcomeScreen : BenchmarkScreenBase;
public sealed record BenchmarkGamePlayScreen : BenchmarkScreenBase;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SessionRepositoryBenchmark
{
    private IMongoDatabase _mongoDatabase = null!;
    private MongoTerminalSessionRepository _mongoRepository = null!;

    private ConnectionMultiplexer _redisMultiplexer = null!;
    private RedisTerminalSessionRepository _redisRepository = null!;

    private Guid _sessionId;
    private BenchmarkWelcomeScreen _welcomeScreen = null!;
    private BenchmarkGamePlayScreen _gamePlayScreen = null!;
    private uint[] _frameBuffer = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sessionId = Guid.NewGuid();
        _welcomeScreen = new BenchmarkWelcomeScreen { Id = Guid.NewGuid(), Name = "WelcomeScreen", SessionId = _sessionId };
        _gamePlayScreen = new BenchmarkGamePlayScreen {Id = Guid.NewGuid(), Name = "GamePlayScreen", SessionId = _sessionId };
        _frameBuffer = new uint[80 * 25]; // Standard 80x25 terminal frame matrix layout

        // Initialize MongoDB Core Infrastructure Context Configurations
        BsonSerializer.RegisterSerializer(new GuidSerializer(MongoDB.Bson.GuidRepresentation.Standard));
        BsonClassMap.RegisterClassMap<TerminalScreen>(cm =>
        {
            cm.AutoMap();
            cm.SetIsRootClass(true);
        });
        BsonClassMap.RegisterClassMap<BenchmarkWelcomeScreen>();
        BsonClassMap.RegisterClassMap<BenchmarkGamePlayScreen>();

        MongoClient mongoClient = new("mongodb://admin:secret_password_123@localhost:27017/?authSource=admin");
        _mongoDatabase = mongoClient.GetDatabase("BenchmarkDb");
        _mongoRepository = new MongoTerminalSessionRepository(NullLogger<MongoTerminalSessionRepository>.Instance, _mongoDatabase);

        // Initialize High-Performance Redis Storage Client Settings Layout
        RedisJsonTypeResolver typeResolver = new();
        typeResolver.RegisterScreen<BenchmarkWelcomeScreen>().RegisterScreen<BenchmarkGamePlayScreen>();
        JsonSerializerOptions jsonOptions = typeResolver.CreateOptions();
        RedisCacheOptions cacheOptions = new();

        _redisMultiplexer = ConnectionMultiplexer.Connect("localhost:6379,password=secret_password_123,abortConnect=false");
        _redisRepository = new RedisTerminalSessionRepository(NullLogger<RedisTerminalSessionRepository>.Instance, _redisMultiplexer, jsonOptions, cacheOptions);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _mongoDatabase.DropCollection("terminal_sessions");
        _mongoDatabase.DropCollection("active_screens");
        _redisMultiplexer.GetDatabase().KeyDelete($"session:{_sessionId}");
        _redisMultiplexer.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task Mongo_FullSessionCycleSimulation()
    {
        // Persist initial operational base interactive layout context screen to Mongo storage 
        await _mongoRepository.SaveActiveScreenAsync(_sessionId, _welcomeScreen);

        // Append downstream structural state navigation history document records sequentially
        await _mongoRepository.SaveActiveScreenAsync(_sessionId, _gamePlayScreen);

        // Synchronize and capture heavy visual display representation matrices logs
        await _mongoRepository.SaveHistoricalBufferAsync(_sessionId, _frameBuffer);

        // Resolve and reconstruct runtime domain reference objects hierarchy maps
        await _mongoRepository.GetActiveScreenAsync(_sessionId);
        await _mongoRepository.GetHistoricalBufferAsync(_sessionId);

        // Discard expired operational states from target navigation persistence stacks
        await _mongoRepository.RemoveScreenAsync(_sessionId, _welcomeScreen.Id);
    }

    [Benchmark]
    public async Task RedisHash_FullSessionCycleSimulation()
    {
        // Persist initial operational base interactive layout context screen to Redis Hash fields
        await _redisRepository.SaveActiveScreenAsync(_sessionId, _welcomeScreen);

        // Append downstream structural state navigation history document records sequentially
        await _redisRepository.SaveActiveScreenAsync(_sessionId, _gamePlayScreen);

        // Synchronize and capture heavy visual display representation matrices logs
        await _redisRepository.SaveHistoricalBufferAsync(_sessionId, _frameBuffer);

        // Resolve and reconstruct runtime domain reference objects hierarchy maps
        await _redisRepository.GetActiveScreenAsync(_sessionId);
        await _redisRepository.GetHistoricalBufferAsync(_sessionId);

        // Discard expired operational states from target navigation persistence stacks
        await _redisRepository.RemoveScreenAsync(_sessionId, _welcomeScreen.Id);
    }
}
