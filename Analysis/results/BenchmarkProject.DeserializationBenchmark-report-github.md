``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


```
|                 Method | ChaptersCount |           Mean |        Error |       StdDev | Ratio |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------------- |-------------- |---------------:|-------------:|-------------:|------:|--------:|--------:|------:|----------:|
| **Deserialize_Newtonsoft** |             **1** |     **4,015.9 ns** |     **65.58 ns** |     **64.40 ns** |  **1.00** |  **0.8621** |       **-** |     **-** |    **5456 B** |
|   Deserialize_MetaJson |             1 |       857.7 ns |     11.07 ns |      9.81 ns |  0.21 |  0.1373 |       - |     - |     864 B |
|                        |               |                |              |              |       |         |         |       |           |
| **Deserialize_Newtonsoft** |            **10** |    **14,375.2 ns** |    **137.49 ns** |    **128.61 ns** |  **1.00** |  **1.8005** |  **0.0153** |     **-** |   **11328 B** |
|   Deserialize_MetaJson |            10 |     3,306.5 ns |     52.05 ns |     67.68 ns |  0.23 |  0.6905 |  0.0153 |     - |    4336 B |
|                        |               |                |              |              |       |         |         |       |           |
| **Deserialize_Newtonsoft** |           **100** |   **111,751.2 ns** |  **1,234.88 ns** |  **1,094.69 ns** |  **1.00** |  **4.3945** |  **0.3662** |     **-** |   **28312 B** |
|   Deserialize_MetaJson |           100 |    27,722.9 ns |    121.78 ns |    113.92 ns |  0.25 |  6.1035 |  1.0071 |     - |   38368 B |
|                        |               |                |              |              |       |         |         |       |           |
| **Deserialize_Newtonsoft** |          **1000** | **1,097,418.5 ns** | **17,416.19 ns** | **15,439.00 ns** |  **1.00** | **29.2969** |  **9.7656** |     **-** |  **193920 B** |
|   Deserialize_MetaJson |          1000 |   289,422.7 ns |  2,012.73 ns |  1,882.71 ns |  0.26 | 59.5703 | 29.7852 |     - |  375280 B |
