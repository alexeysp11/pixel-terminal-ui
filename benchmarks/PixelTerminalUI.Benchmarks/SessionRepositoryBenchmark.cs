using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using PixelTerminalUI.Engine.Screens;
using PixelTerminalUI.Persistence.Redis.Repositories;
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
        _redisMultiplexer.GetDatabase().KeyDelete($"session:{_sessionId}");
        _redisMultiplexer.Dispose();
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
