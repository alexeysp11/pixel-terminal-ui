## 📊 Performance Benchmarking

[English](README.md) | [Русский](README.ru.md)

The engine includes a comprehensive suite of automated microbenchmarks powered by **BenchmarkDotNet** to continuously track rendering velocity, memory traffic allocations, and binary network serialization efficiency boundaries.

### ⚠️ Pre-requisites for Accurate Metrics

To prevent thread scheduling distortions, hardware scaling spikes, or JIT compiler optimization bypasses, always adhere to the following environmental isolation constraints before executing tests:
* **Close all heavy background processes** (Docker daemons, IDE instances, browsers, or database layers).
* **Connect your machine to a stable power supply** (disable battery-saving or dynamic performance throttling profiles on laptops).
* **Never use debug attachments** or standard `dotnet run` commands without optimization flags.

### 🚀 Running the Benchmarks

Navigate to the benchmark project root directory inside your shell console and execute the runtime suite under a targeted highly optimized **Release** compilation profile:

```bash
# Navigate to the benchmark tracking directory
cd benchmarks/PixelTerminalUI.Benchmarks

# Execute the isolated target benchmarking process under Release configuration
dotnet run -c Release -- --filter *SerializationBenchmark*
```

### 🗃️ Analyzing Artifacts Reports

Once iteration sequences complete execution, BenchmarkDotNet automatically exports precise metrics dashboards under the local `./BenchmarkDotNet.Artifacts/results/` directory track. The summary outputs are distributed across multiple open structural layouts, including markdown (`.md`), comma-separated values (`.csv`), and standalone web presentation layers (`.html`).
