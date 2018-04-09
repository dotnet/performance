using System;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using Benchmarks.Serializers;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher
                .FromTypes
                (
                    GetOpenGenericBenchmarks()
                        .SelectMany(openGeneric =>
                            GetViewModels().Select(viewModel => openGeneric.MakeGenericType(viewModel)))
                        .ToArray()
                )
                .Run(args, new SimpleConfig());
        }

        static Type[] GetOpenGenericBenchmarks()
            => new[]
            {
                typeof(Json_ToString<>),
                typeof(Json_ToStream<>),
                typeof(Json_FromString<>),
                typeof(Json_FromStream<>),
                typeof(Xml_ToStream<>),
                typeof(Xml_FromStream<>),
                typeof(Binary_ToStream<>),
                typeof(Binary_FromStream<>)
            };

        static Type[] GetViewModels()
            => new[]
            {
                typeof(LoginViewModel),
                typeof(Location),
                typeof(IndexViewModel),
                typeof(MyEventsListerViewModel)
            };
    }

    public class SimpleConfig : ManualConfig
    {
        public SimpleConfig()
        {
            Add(Job.ShortRun); // let's use the Short Run for better first user experience

            Add(MemoryDiagnoser.Default);

            Add(DefaultConfig.Instance.GetValidators().ToArray());
            Add(DefaultConfig.Instance.GetLoggers().ToArray());
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray());

            Add(new CsvMeasurementsExporter(CsvSeparator.Semicolon)); // you can use Excel to load and merge all the data together
            //Add(RPlotExporter.Default); // it produces nice plots but requires R to be installed
            Add(MarkdownExporter.GitHub);
            Add(HtmlExporter.Default);
            //Add(StatisticColumn.AllStatistics); // uncomment it if you want to see more statistics

            Set(new BenchmarkDotNet.Reports.SummaryStyle
            {
                PrintUnitsInHeader = true,
                PrintUnitsInContent = false,
                TimeUnit = TimeUnit.Microsecond,
                SizeUnit = SizeUnit.B
            });
        }
    }

    /// <summary>
    /// this config allows you to run benchmarks for multiple runtimes
    /// </summary>
    public class MultipleRuntimesConfig : ManualConfig
    {
        public MultipleRuntimesConfig()
        {
            Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp20)).AsBaseline().WithId("2.0"));
            Add(Job.Default.With(Runtime.Core).With(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21)).WithId("2.1"));

            Add(Job.Default.With(Runtime.Clr).WithId("Clr"));
            Add(Job.Default.With(Runtime.Mono).WithId("Mono")); // you can comment this if you don't have Mono installed

            Add(MemoryDiagnoser.Default);

            Add(DefaultConfig.Instance.GetValidators().ToArray());
            Add(DefaultConfig.Instance.GetLoggers().ToArray());
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray());

            Add(new CsvMeasurementsExporter(CsvSeparator.Semicolon));
            //Add(RPlotExporter.Default); // it produces nice plots but requires R to be installed
            Add(MarkdownExporter.GitHub);
            Add(HtmlExporter.Default);
            //Add(StatisticColumn.AllStatistics);

            Set(new BenchmarkDotNet.Reports.SummaryStyle
            {
                PrintUnitsInHeader = true,
                PrintUnitsInContent = false,
                TimeUnit = TimeUnit.Microsecond,
                SizeUnit = SizeUnit.B
            });
        }
    }
}
