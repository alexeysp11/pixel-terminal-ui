using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using PixelTerminalUI.Contracts.Dto;
using PixelTerminalUI.Transport.Grpc.Configuration;
using ProtoBuf.Meta;
using System.Text.Json;

namespace PixelTerminalUi.Benchmarks;

/// <summary>
/// Evaluates the processing speed, memory allocations, and payload layout sizes 
/// comparing traditional text-based JSON against code-first Protobuf channels.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class SerializationBenchmark : IDisposable
{
    private TerminalResponse _sampleResponse = null!;
    private MemoryStream _protobufStream = null!;

    /// <summary>
    /// Initializes runtime configuration metadata contract models and allocates reusable data layouts.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // Enforce execution contract layouts compilation before starting the iteration loops
        GrpcModelConfiguration.RegisterTerminalContracts();

        // Construct standard low-density layout payload tracking exactly 5 pixel changes on 80x24 matrix bounds
        _sampleResponse = new TerminalResponse(
            SessionId: Guid.NewGuid(),
            Width: 80,
            Height: 24,
            FullFrame: null,
            Delta: new DeltaPayload(
            [
                new(10, 4294967295),
                new(11, 2147483648),
                new(12, 1024),
                new(13, 2048),
                new(14, 4096)
            ])
        );

        _protobufStream = new MemoryStream();
    }

    /// <summary>
    /// Evaluates the baseline payload capacity footprint using system reflection text serialization routines.
    /// </summary>
    [Benchmark(Baseline = true)]
    public byte[] ExecuteSystemTextJsonSerialization()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_sampleResponse);
    }

    /// <summary>
    /// Evaluates execution velocity and structural allocation profiles of binary code-first serialization layers.
    /// </summary>
    [Benchmark]
    public long ExecuteCodeFirstProtobufSerialization()
    {
        _protobufStream.SetLength(0);
        _protobufStream.Position = 0;

        RuntimeTypeModel.Default.Serialize(_protobufStream, _sampleResponse);
        return _protobufStream.Length;
    }

    /// <summary>
    /// Safely disposes allocated resources and infrastructure streams.
    /// </summary>
    public void Dispose()
    {
        _protobufStream?.Dispose();
    }
}
