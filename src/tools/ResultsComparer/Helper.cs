using DataTransferContracts;
using Newtonsoft.Json;
using Perfolizer.Mathematics.Multimodality;
using System;
using System.IO;

namespace ResultsComparer
{
    internal static class Helper
    {
        internal const string FullBdnJsonFileExtension = "full.json";

        internal static string[] GetFilesToParse(string path)
        {
            if (Directory.Exists(path))
                return Directory.GetFiles(path, $"*{Helper.FullBdnJsonFileExtension}", SearchOption.AllDirectories);
            else if (File.Exists(path) || !path.EndsWith(Helper.FullBdnJsonFileExtension))
                return new[] { path };
            else
                throw new FileNotFoundException($"Provided path does NOT exist or is not a {path} file", path);
        }

        // code and magic values taken from BenchmarkDotNet.Analysers.MultimodalDistributionAnalyzer
        // See http://www.brendangregg.com/FrequencyTrails/modes.html
        internal static string GetModalInfo(Benchmark benchmark)
        {
            if (benchmark.Statistics.N < 12) // not enough data to tell
                return null;

            double mValue = MValueCalculator.Calculate(benchmark.Statistics.OriginalValues);
            if (mValue > 4.2)
                return "multimodal";
            else if (mValue > 3.2)
                return "bimodal";
            else if (mValue > 2.8)
                return "several?";

            return null;
        }

        internal static BdnResult ReadFromFile(string resultFilePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<BdnResult>(File.ReadAllText(resultFilePath));
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine($"Exception while reading the {resultFilePath} file.");

                throw;
            }
        }

        internal static BdnResult ReadFromStream(Stream stream)
        {
            try
            {
                return (BdnResult)new JsonSerializer().Deserialize(new StreamReader(stream), typeof(BdnResult));
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine($"Exception while reading the JSON file.");

                throw;
            }
        }
    }
}
