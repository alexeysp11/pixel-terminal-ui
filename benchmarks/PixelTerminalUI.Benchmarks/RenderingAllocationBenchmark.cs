using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Buffers;

namespace PixelTerminalUi.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class RenderingAllocationBenchmark
{
    private FakeScreen _largeScreen = null!;
    private OldFakeRenderer _oldRenderer = null!;
    private NewFakeRenderer _newRenderer = null!;

    [Params(40, 80)]
    public int Width { get; set; }

    [Params(12, 25)]
    public int Height { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _largeScreen = new FakeScreen
        {
            SessionId = Guid.NewGuid(),
            Width = Width,
            Height = Height
        };

        _oldRenderer = new OldFakeRenderer();
        _newRenderer = new NewFakeRenderer();
    }

    [Benchmark(Baseline = true)]
    public TerminalResponse OldRenderWithTwoDimensionalArray()
    {
        // 1. Old approach: Allocation of multidimensional array Pixel[,] inside the renderer
        Pixel[,] buffer = _oldRenderer.Draw(_largeScreen);

        int width = buffer.GetLength(0);
        int height = buffer.GetLength(1);
        int totalCellsCount = width * height;

        uint[] flatBuffer = new uint[totalCellsCount];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Pixel currentPixel = buffer[x, y];
                byte inversionFlag = (byte)(currentPixel.IsInverted ? 1 : 0);

                flatBuffer[y * width + x] = PixelBitPacker.Pack(
                    currentPixel.Symbol,
                    (byte)currentPixel.Foreground,
                    (byte)currentPixel.Background,
                    inversionFlag);
            }
        }

        return new TerminalResponse(_largeScreen.SessionId, flatBuffer, width, height);
    }

    [Benchmark]
    public TerminalResponse NewRenderWithArrayPoolAndFlatArray()
    {
        int width = _largeScreen.Width;
        int height = _largeScreen.Height;
        int totalCellsCount = width * height;

        // 2. New approach: Symmetrical renting of flat array from the shared pool
        Pixel[] pooledBuffer = ArrayPool<Pixel>.Shared.Rent(totalCellsCount);
        uint[] flatBuffer = new uint[totalCellsCount];

        try
        {
            _newRenderer.Draw(_largeScreen, pooledBuffer);

            for (int index = 0; index < totalCellsCount; index++)
            {
                Pixel currentPixel = pooledBuffer[index];
                byte inversionFlag = (byte)(currentPixel.IsInverted ? 1 : 0);

                flatBuffer[index] = PixelBitPacker.Pack(
                    currentPixel.Symbol,
                    (byte)currentPixel.Foreground,
                    (byte)currentPixel.Background,
                    inversionFlag);
            }
        }
        finally
        {
            ArrayPool<Pixel>.Shared.Return(pooledBuffer);
        }

        return new TerminalResponse(_largeScreen.SessionId, flatBuffer, width, height);
    }

    #region Mock In-Memory Infrastructure Components

    public sealed record FakeScreen
    {
        public Guid SessionId { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
    }

    public sealed class OldFakeRenderer
    {
        public Pixel[,] Draw(FakeScreen screen)
        {
            Pixel[,] buffer = new Pixel[screen.Width, screen.Height];
            for (int x = 0; x < screen.Width; x++)
            {
                for (int y = 0; y < screen.Height; y++)
                {
                    buffer[x, y] = new Pixel(' ', false, ConsoleColor.White, ConsoleColor.Black);
                }
            }
            return buffer;
        }
    }

    public sealed class NewFakeRenderer
    {
        public void Draw(FakeScreen screen, Pixel[] buffer)
        {
            int width = screen.Width;
            int height = screen.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    buffer[y * width + x] = new Pixel(' ', false, ConsoleColor.White, ConsoleColor.Black);
                }
            }
        }
    }

    public readonly record struct Pixel
    {
        public char Symbol { get; }
        public bool IsInverted { get; }
        public ConsoleColor Foreground { get; }
        public ConsoleColor Background { get; }

        public Pixel(char symbol, bool isInverted, ConsoleColor foreground, ConsoleColor background)
        {
            Symbol = symbol;
            IsInverted = isInverted;
            Foreground = foreground;
            Background = background;
        }
    }

    public static class PixelBitPacker
    {
        public static uint Pack(char symbol, byte fg, byte bg, byte inv)
        {
            return (uint)symbol | ((uint)fg << 16) | ((uint)bg << 24) | ((uint)inv << 31);
        }
    }

    public sealed record TerminalResponse(Guid SessionId, uint[] Buffer, int Width, int Height);

    #endregion
}
