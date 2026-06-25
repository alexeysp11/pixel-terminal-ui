```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
AMD Ryzen 7 5700U with Radeon Graphics 1.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.201
  [Host]     : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3


```
| Method                 | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------- |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| ProcessLegacyBuffer    | 1.957 μs | 0.0119 μs | 0.0100 μs |  1.00 | 2.7618 |   5.65 KB |        1.00 |
| ProcessOptimizedBuffer | 1.807 μs | 0.0036 μs | 0.0030 μs |  0.92 | 0.9289 |    1.9 KB |        0.34 |
