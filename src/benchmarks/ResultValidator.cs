using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using BdnDtos;
using Newtonsoft.Json;
using XUnitDtos;

namespace Benchmarks
{
    public class ResultValidator
    {
        public static void Compare(string xunitFolder, string bdnFolder)
        {
            var xUnitResults = GetXUnitResults(xunitFolder)
	            .SelectMany(result => result.Assembly.Collection.Test)
	            .ToDictionary(test => test.Name, test => test.Performance);

	        var bdnResults = GetBdnResults(bdnFolder)
		        .SelectMany(result => result.Benchmarks)
		        .ToDictionary(benchmark => benchmark.FullName, benchmark => benchmark);

	        PrintMissingBenchmarks(xUnitResults, bdnResults);
	        
	        WriteResultsToCSV(xUnitResults, bdnResults);
        }

	    private static IEnumerable<Assemblies> GetXUnitResults(string xunitFolder)
        {
            var serializer = new XmlSerializer(typeof(Assemblies));

            foreach (var resultFile in Directory.GetFiles(xunitFolder, searchPattern: "*.xml"))
                using (StreamReader reader = new StreamReader(resultFile))
                    yield return (Assemblies) serializer.Deserialize(reader);
        }

	    private static IEnumerable<BdnResult> GetBdnResults(string bdnFolder) 
		    => Directory.GetFiles(bdnFolder, searchPattern: "*.json")
						.Select(resultFile => JsonConvert.DeserializeObject<BdnResult>(File.ReadAllText(resultFile)));

	    private static void PrintMissingBenchmarks(Dictionary<string, Performance> xUnitResults, Dictionary<string, Benchmark> bdnResults)
	    {
		    foreach (var xUnitId in xUnitResults.Keys)
			    if (!bdnResults.ContainsKey(xUnitId))
				    Console.WriteLine($"Missing benchmark! {xUnitId}");

		    Console.WriteLine();
		    
		    foreach (var bdnId in bdnResults.Keys)
				if (!xUnitResults.ContainsKey(bdnId))
					Console.WriteLine($"But found {bdnId}");
	    }

	    private static void WriteResultsToCSV(Dictionary<string, Performance> xUnitResults, Dictionary<string, Benchmark> bdnResults)
	    {
		    using(StreamWriter writer = new StreamWriter("results.csv"))
		    {
			    writer.WriteLine("Id;XunitAllocated;BdnAllocated;GuessedScaleFactor;xUnitMin;bdntMin;xUnitAvg;bdnAvg;xUnitMax;bdnMax");
		    
				foreach (var xUnitResult in xUnitResults)
					if (bdnResults.TryGetValue(xUnitResult.Key, out var bdnResult))
					{
						long bdnAllocated = bdnResult.Memory.BytesAllocatedPerOperation;
						long xunitAllocated = xUnitResult.Value.Iterations.Iteration
							.Last() // every iteration has allocated data, but the fist one has typically some overhead so I choose the last ;)
							.GCGetAllocatedBytesForCurrentThread;
	
						long guessedScaleFactor = bdnAllocated != 0 ? xunitAllocated / bdnAllocated : -1; 
						
						writer.Write($"\"{xUnitResult.Key}\";{xunitAllocated};{bdnAllocated};{guessedScaleFactor}");
	
						var unit = xUnitResult.Value.Metrics.Duration.Unit;
						var xunitMin = ToNanoseconds(xUnitResult.Value.Iterations.Iteration.Min(r => r.Duration), unit);
						var xunitAvg = ToNanoseconds(xUnitResult.Value.Iterations.Iteration.Average(r => r.Duration), unit);
						var xunitMax = ToNanoseconds(xUnitResult.Value.Iterations.Iteration.Max(r => r.Duration), unit);
	
						var bdnMin = bdnResult.Statistics.Min;
						var bdnAvg = bdnResult.Statistics.Mean;
						var bdnMax = bdnResult.Statistics.Max;
						
						writer.WriteLine($";{xunitMin};{bdnMin};{xunitAvg};{bdnAvg};{xunitMax};{bdnMax}");
					}
			}
	    }

	    private static double ToNanoseconds(double source, string unit) 
		    => unit == "msec"
				? source * 1000000
				: throw new NotSupportedException("I expected xunit-performance to always report the results in msec");
    }
}

namespace XUnitDtos // generated with http://xmltocsharp.azurewebsites.net/
{
	[XmlRoot(ElementName="Duration")]
	public class Duration {
		[XmlAttribute(AttributeName="displayName")]
		public string DisplayName { get; set; }
		[XmlAttribute(AttributeName="unit")]
		public string Unit { get; set; }
	}

	[XmlRoot(ElementName="GC.GetAllocatedBytesForCurrentThread")]
	public class GCGetAllocatedBytesForCurrentThread {
		[XmlAttribute(AttributeName="displayName")]
		public string DisplayName { get; set; }
		[XmlAttribute(AttributeName="unit")]
		public string Unit { get; set; }
	}

	[XmlRoot(ElementName="metrics")]
	public class Metrics {
		[XmlElement(ElementName="Duration")]
		public Duration Duration { get; set; }
		[XmlElement(ElementName="GC.GetAllocatedBytesForCurrentThread")]
		public GCGetAllocatedBytesForCurrentThread GCGetAllocatedBytesForCurrentThread { get; set; }
	}

	[XmlRoot(ElementName="iteration")]
	public class Iteration {
		[XmlAttribute(AttributeName="index")]
		public string Index { get; set; }
		[XmlAttribute(AttributeName="Duration")]
		public double Duration { get; set; }
		[XmlAttribute(AttributeName="GC.GetAllocatedBytesForCurrentThread")]
		public long GCGetAllocatedBytesForCurrentThread { get; set; }
	}

	[XmlRoot(ElementName="iterations")]
	public class Iterations {
		[XmlElement(ElementName="iteration")]
		public List<Iteration> Iteration { get; set; }
	}

	[XmlRoot(ElementName="performance")]
	public class Performance {
		[XmlElement(ElementName="metrics")]
		public Metrics Metrics { get; set; }
		[XmlElement(ElementName="iterations")]
		public Iterations Iterations { get; set; }
	}

	[XmlRoot(ElementName="test")]
	public class Test {
		[XmlElement(ElementName="performance")]
		public Performance Performance { get; set; }
		[XmlAttribute(AttributeName="name")]
		public string Name { get; set; }
		[XmlAttribute(AttributeName="type")]
		public string Type { get; set; }
		[XmlAttribute(AttributeName="method")]
		public string Method { get; set; }
	}

	[XmlRoot(ElementName="collection")]
	public class Collection {
		[XmlElement(ElementName="test")]
		public List<Test> Test { get; set; }
	}

	[XmlRoot(ElementName="assembly")]
	public class Assembly {
		[XmlElement(ElementName="collection")]
		public Collection Collection { get; set; }
		[XmlAttribute(AttributeName="name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName="assemblies")]
	public class Assemblies {
		[XmlElement(ElementName="assembly")]
		public Assembly Assembly { get; set; }
	}
}

namespace BdnDtos // generated with http://json2csharp.com/#
{
	public class ChronometerFrequency
	{
		public int Hertz { get; set; }
	}
	
	public class HostEnvironmentInfo
	{
		public string BenchmarkDotNetCaption { get; set; }
		public string BenchmarkDotNetVersion { get; set; }
		public string OsVersion { get; set; }
		public string ProcessorName { get; set; }
		public int PhysicalProcessorCount { get; set; }
		public int PhysicalCoreCount { get; set; }
		public int LogicalCoreCount { get; set; }
		public string RuntimeVersion { get; set; }
		public string Architecture { get; set; }
		public bool HasAttachedDebugger { get; set; }
		public bool HasRyuJit { get; set; }
		public string Configuration { get; set; }
		public string JitModules { get; set; }
		public string DotNetCliVersion { get; set; }
		public ChronometerFrequency ChronometerFrequency { get; set; }
		public string HardwareTimerKind { get; set; }
	}
	
	public class ConfidenceInterval
	{
		public int N { get; set; }
		public double Mean { get; set; }
		public double StandardError { get; set; }
		public int Level { get; set; }
		public double Margin { get; set; }
		public double Lower { get; set; }
		public double Upper { get; set; }
	}
	
	public class Percentiles
	{
		public double P0 { get; set; }
		public double P25 { get; set; }
		public double P50 { get; set; }
		public double P67 { get; set; }
		public double P80 { get; set; }
		public double P85 { get; set; }
		public double P90 { get; set; }
		public double P95 { get; set; }
		public double P100 { get; set; }
	}
	
	public class Statistics
	{
		public int N { get; set; }
		public double Min { get; set; }
		public double LowerFence { get; set; }
		public double Q1 { get; set; }
		public double Median { get; set; }
		public double Mean { get; set; }
		public double Q3 { get; set; }
		public double UpperFence { get; set; }
		public double Max { get; set; }
		public double InterquartileRange { get; set; }
		public List<double> LowerOutliers { get; set; }
		public List<double> UpperOutliers { get; set; }
		public List<double> AllOutliers { get; set; }
		public double StandardError { get; set; }
		public double Variance { get; set; }
		public double StandardDeviation { get; set; }
		public double Skewness { get; set; }
		public double Kurtosis { get; set; }
		public ConfidenceInterval ConfidenceInterval { get; set; }
		public Percentiles Percentiles { get; set; }
	}
	
	public class Memory
	{
		public int Gen0Collections { get; set; }
		public int Gen1Collections { get; set; }
		public int Gen2Collections { get; set; }
		public int TotalOperations { get; set; }
		public long BytesAllocatedPerOperation { get; set; }
	}
	
	public class Measurement
	{
		public string IterationMode { get; set; }
		public int LaunchIndex { get; set; }
		public int IterationIndex { get; set; }
		public long Operations { get; set; }
		public double Nanoseconds { get; set; }
	}
	
	public class Benchmark
	{
		public string DisplayInfo { get; set; }
		public object Namespace { get; set; }
		public string Type { get; set; }
		public string Method { get; set; }
		public string MethodTitle { get; set; }
		public string Parameters { get; set; }
		public string FullName { get; set; }
		public Statistics Statistics { get; set; }
		public Memory Memory { get; set; }
		public List<Measurement> Measurements { get; set; }
	}
	
	public class BdnResult
	{
		public string Title { get; set; }
		public HostEnvironmentInfo HostEnvironmentInfo { get; set; }
		public List<Benchmark> Benchmarks { get; set; }
	}
}