```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
AMD Ryzen 7 5700U with Radeon Graphics 1.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.201
  [Host]     : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3


```
| Method                             | Width | Height | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|----------------------------------- |------ |------- |----------:|----------:|----------:|------:|--------:|-----:|--------:|----------:|------------:|
| NewRenderWithArrayPoolAndFlatArray | 40    | 12     |  1.146 μs | 0.0024 μs | 0.0021 μs |  0.27 |    0.00 |    1 |  0.9518 |   1.95 KB |        0.26 |
| OldRenderWithTwoDimensionalArray   | 40    | 12     |  4.180 μs | 0.0583 μs | 0.0517 μs |  1.00 |    0.02 |    2 |  3.7155 |   7.61 KB |        1.00 |
|                                    |       |        |           |           |           |       |         |      |         |           |             |
| NewRenderWithArrayPoolAndFlatArray | 40    | 25     |  2.337 μs | 0.0167 μs | 0.0148 μs |  0.27 |    0.00 |    1 |  1.9455 |   3.98 KB |        0.25 |
| OldRenderWithTwoDimensionalArray   | 40    | 25     |  8.648 μs | 0.1519 μs | 0.1269 μs |  1.00 |    0.02 |    2 |  7.6904 |  15.73 KB |        1.00 |
|                                    |       |        |           |           |           |       |         |      |         |           |             |
| NewRenderWithArrayPoolAndFlatArray | 80    | 12     |  2.249 μs | 0.0101 μs | 0.0090 μs |  0.28 |    0.00 |    1 |  1.8654 |   3.82 KB |        0.25 |
| OldRenderWithTwoDimensionalArray   | 80    | 12     |  8.105 μs | 0.0408 μs | 0.0362 μs |  1.00 |    0.01 |    2 |  7.3700 |  15.11 KB |        1.00 |
|                                    |       |        |           |           |           |       |         |      |         |           |             |
| NewRenderWithArrayPoolAndFlatArray | 80    | 25     |  4.666 μs | 0.0095 μs | 0.0084 μs |  0.28 |    0.00 |    1 |  3.8452 |   7.88 KB |        0.25 |
| OldRenderWithTwoDimensionalArray   | 80    | 25     | 16.948 μs | 0.0432 μs | 0.0383 μs |  1.00 |    0.00 |    2 | 15.2588 |  31.36 KB |        1.00 |
