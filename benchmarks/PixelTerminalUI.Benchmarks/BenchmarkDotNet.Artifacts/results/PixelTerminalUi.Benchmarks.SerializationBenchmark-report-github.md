```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
AMD Ryzen 7 5700U with Radeon Graphics 1.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.201
  [Host]     : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3


```
| Method                                | Mean     | Error   | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |---------:|--------:|--------:|------:|-----:|-------:|----------:|------------:|
| ExecuteCodeFirstProtobufSerialization | 519.7 ns | 1.09 ns | 0.91 ns |  0.58 |    1 |      - |         - |        0.00 |
| ExecuteSystemTextJsonSerialization    | 902.3 ns | 1.31 ns | 1.09 ns |  1.00 |    2 | 0.3557 |     744 B |        1.00 |
