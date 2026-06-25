using BenchmarkDotNet.Attributes;
using PixelTerminalUI.Contracts.Common;
using PixelTerminalUI.Contracts.Optimizations;

namespace PixelTerminalUi.Benchmarks;

[MemoryDiagnoser]
public class BufferProcessingBenchmark
{
    private const int Width = 40;
    private const int Height = 12;
    private readonly char[] _sampleSymbols = ['S', 'E', 'S', 'S', 'I', 'O', 'N', ' ', 'A', 'C', 'T', 'I', 'V', 'E'];

    [Benchmark(Baseline = true)]
    public Pixel[] ProcessLegacyBuffer()
    {
        Pixel[] flatBuffer = new Pixel[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char symbol = _sampleSymbols[(y * Width + x) % _sampleSymbols.Length];

                // Generates garbage memory due to high-volume record struct allocation sequences
                flatBuffer[y * Width + x] = new Pixel(symbol, false, ConsoleColor.White, ConsoleColor.Black);
            }
        }

        return flatBuffer;
    }

    [Benchmark]
    public uint[] ProcessOptimizedBuffer()
    {
        uint[] flatBuffer = new uint[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char symbol = _sampleSymbols[(y * Width + x) % _sampleSymbols.Length];

                // Employs aggressive inlining bit-shifting logic yielding zero heap allocations
                flatBuffer[y * Width + x] = PixelBitPacker.Pack(symbol, 15, 0, 0);
            }
        }

        return flatBuffer;
    }
}
