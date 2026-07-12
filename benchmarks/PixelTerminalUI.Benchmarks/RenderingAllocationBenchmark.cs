using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Buffers;

namespace PixelTerminalUi.Benchmarks;

/// <summary>
/// Evaluates the rendering velocity and memory allocation footprints comparing legacy 
/// multidimensional matrix layouts against highly optimized flat arrays managed by a shared object pool.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RenderingAllocationBenchmark
{
    private FakeScreen _largeScreen = null!;
    private OldFakeRenderer _oldRenderer = null!;
    private NewFakeRenderer _newRenderer = null!;

    /// <summary>
    /// Gets or sets the target structural matrix width layout parameter bound.
    /// </summary>
    [Params(40, 80)]
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the target structural matrix height layout parameter bound.
    /// </summary>
    [Params(12, 25)]
    public int Height { get; set; }

    /// <summary>
    /// Pre-allocates reference screen states and initializes target rendering subsystem frameworks.
    /// </summary>
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

    /// <summary>
    /// Evaluates performance baselines using legacy multidimensional array allocations inside the execution thread.
    /// </summary>
    /// <returns>A comprehensive transport data state representation frame package.</returns>
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

    /// <summary>
    /// Evaluates memory traffic suppression and execution velocity using a symmetrical array pool renting strategy.
    /// </summary>
    /// <returns>A comprehensive transport data state representation frame package.</returns>
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

    /// <summary>
    /// Defines a simulation metadata layout structure for target viewport canvas metrics tracking.
    /// </summary>
    public sealed record FakeScreen
    {
        public Guid SessionId { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
    }

    /// <summary>
    /// Simulates a legacy display generation engine relying on persistent internal heap allocations.
    /// </summary>
    public sealed class OldFakeRenderer
    {
        /// <summary>
        /// Generates a newly allocated multidimensional buffer array on every standalone draw invocation.
        /// </summary>
        /// <param name="screen">The logical viewport model specification criteria tracking frame bounds.</param>
        /// <returns>A multidimensional reference tracking matrix containing structural cell properties.</returns>
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

    /// <summary>
    /// Simulates a modern high-velocity display generation engine utilizing external block buffers memory buffers.
    /// </summary>
    public sealed class NewFakeRenderer
    {
        /// <summary>
        /// Populates an externally provided flat memory block without generating downstream garbage collections.
        /// </summary>
        /// <param name="screen">The logical viewport model specification criteria tracking frame bounds.</param>
        /// <param name="buffer">The pooled flat memory target destination block mapping coordinate indexes.</param>
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

    /// <summary>
    /// Defines foundational graphical tokens mapped directly to targeted standalone console cell coordinates.
    /// </summary>
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

    /// <summary>
    /// Provides atomic bit-shifting routines to optimize network payloads into single primitives.
    /// </summary>
    public static class PixelBitPacker
    {
        /// <summary>
        /// Compresses discrete representation metadata into a single compact unsigned integer field.
        /// </summary>
        /// <param name="symbol">The visual character symbol presentation token.</param>
        /// <param name="fg">The foreground byte identifier value.</param>
        /// <param name="bg">The background byte identifier value.</param>
        /// <param name="inv">The cell state transformation inversion byte flag.</param>
        /// <returns>A bit-packed representation layout sequence primitive.</returns>
        public static uint Pack(char symbol, byte fg, byte bg, byte inv)
        {
            return (uint)symbol | ((uint)fg << 16) | ((uint)bg << 24) | ((uint)inv << 31);
        }
    }

    /// <summary>
    /// Represents the immutable composite data transfer block forwarded across remote execution channels.
    /// </summary>
    public sealed record TerminalResponse(Guid SessionId, uint[] Buffer, int Width, int Height);

    #endregion
}
