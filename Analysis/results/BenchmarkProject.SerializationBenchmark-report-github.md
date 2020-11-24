``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


```
|               Method | ChaptersCount |         Mean |        Error |       StdDev | Ratio |   Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|--------------------- |-------------- |-------------:|-------------:|-------------:|------:|--------:|--------:|--------:|----------:|
| **Serialize_Newtonsoft** |             **1** |   **2,803.3 ns** |     **19.99 ns** |     **18.69 ns** |  **1.00** |  **0.4539** |       **-** |       **-** |   **2.78 KB** |
|   Serialize_MetaJson |             1 |     442.9 ns |      8.79 ns |     12.32 ns |  0.16 |  0.3543 |  0.0029 |       - |   2.17 KB |
|                      |               |              |              |              |       |         |         |         |           |
| **Serialize_Newtonsoft** |            **10** |  **11,066.7 ns** |    **158.73 ns** |    **132.54 ns** |  **1.00** |  **1.4801** |  **0.0305** |       **-** |   **9.16 KB** |
|   Serialize_MetaJson |            10 |   1,434.3 ns |     19.92 ns |     17.66 ns |  0.13 |  1.2417 |  0.0343 |       - |   7.61 KB |
|                      |               |              |              |              |       |         |         |         |           |
| **Serialize_Newtonsoft** |           **100** |  **93,287.5 ns** |  **1,253.22 ns** |  **1,172.26 ns** |  **1.00** | **11.2305** |  **1.8311** |       **-** |  **69.29 KB** |
|   Serialize_MetaJson |           100 |  10,585.8 ns |    198.05 ns |    271.10 ns |  0.11 |  9.5215 |  1.5869 |       - |  58.44 KB |
|                      |               |              |              |              |       |         |         |         |           |
| **Serialize_Newtonsoft** |          **1000** | **978,721.2 ns** | **19,553.44 ns** | **20,079.95 ns** |  **1.00** | **99.6094** | **99.6094** | **99.6094** | **674.87 KB** |
|   Serialize_MetaJson |          1000 | 107,170.3 ns |    366.17 ns |    305.76 ns |  0.11 | 83.2520 | 83.2520 | 83.2520 |  523.8 KB |
