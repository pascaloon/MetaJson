using System;
using System.Globalization;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace BenchmarkProject
{
    [MemoryDiagnoser]
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        [Benchmark]
        public byte[] Md5() => md5.ComputeHash(data);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var config = ManualConfig.Create(DefaultConfig.Instance);
            config.AddExporter(new CsvExporter(
                CsvSeparator.Comma,
                new BenchmarkDotNet.Reports.SummaryStyle(cultureInfo: CultureInfo.InvariantCulture, printUnitsInHeader: true, sizeUnit: SizeUnit.KB, timeUnit: TimeUnit.Microsecond, printUnitsInContent: false)
                ));

            var summarySerialization = BenchmarkRunner.Run<SerializationBenchmark>(config);
            var summaryDeserialization = BenchmarkRunner.Run<DeserializationBenchmark>(config);
        }
    }
}
