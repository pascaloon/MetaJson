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
