```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
AMD Ryzen 7 5700U with Radeon Graphics 1.80GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.201
  [Host]     : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3


```
| Method                               | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0     | Allocated | Alloc Ratio |
|------------------------------------- |----------:|----------:|----------:|------:|--------:|-----:|---------:|----------:|------------:|
| RedisHash_FullSessionCycleSimulation |  8.102 ms | 0.1874 ms | 0.5526 ms |  0.34 |    0.02 |    1 |  31.2500 |  63.32 KB |        0.14 |
| Mongo_FullSessionCycleSimulation     | 23.760 ms | 0.3746 ms | 0.3321 ms |  1.00 |    0.02 |    2 | 218.7500 | 465.18 KB |        1.00 |
