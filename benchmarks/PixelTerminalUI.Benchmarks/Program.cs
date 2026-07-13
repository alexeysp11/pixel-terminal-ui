using BenchmarkDotNet.Running;

namespace PixelTerminalUi.Benchmarks;

/// <summary>
/// Provides the main entry point to execute the automated microbenchmarking execution suite.
/// </summary>
public static class Program
{
    /// <summary>
    /// Scans the target assembly via reflection and launches an interactive command-line benchmark switcher.
    /// </summary>
    /// <param name="args">The explicit command-line filtering and configuration flags passed down to BenchmarkDotNet.</param>
    public static void Main(string[] args)
    {
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args);
    }
}
