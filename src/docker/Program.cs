using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DockerHarness
{
    class Program
    {
        public static string RepositoriesJsonPath = Environment.GetEnvironmentVariable("REPOS_JSON_PATH") ?? "repositories.json";
        public static string KustoReportPath = Environment.GetEnvironmentVariable("KUSTO_REPORT_PATH") ?? Path.Combine("reports", "kusto.csv");
        public static string BenchviewReportPath = Environment.GetEnvironmentVariable("BV_REPORT_PATH") ?? Path.Combine("reports", "benchview.csv");

        static void Main(string[] args)
        {
            Util.EnsurePathExists(KustoReportPath);
            Util.EnsurePathExists(BenchviewReportPath);
          
            using (var harness = new DockerHarness())
            {
                // Load repository urls from JSON file
                var reposJson = JArray.Parse(File.ReadAllText(RepositoriesJsonPath));
                foreach (JToken repoJson in reposJson)
                {
                    var url = (repoJson["url"] as JValue).Value as string;
                    var path = (repoJson["path"] as JValue)?.Value as string;
                    if (path != null)
                    {
                        harness.LoadManifest(url, path);
                    }
                    else
                    {
                        harness.LoadManifest(url);
                    }
                }

                File.WriteAllText(KustoReportPath, KustoReport(harness));
                File.WriteAllText(BenchviewReportPath, BenchviewReport(harness));
            }
        }

        public static string KustoReport(DockerHarness harness)
        {
            var report = new StringBuilder();

            report.AppendCsvRow("Name","Tag","ParentName","ParentTag","BaseName","BaseTag","Size","AddedSize","NumAddedPackages","AddedPackages","Inspect", "AllTags");
            foreach (var image in harness.SupportedImages())
            {
                // Pick a tag to identify this image with. They all work, but the shortest is the most stable across builds
                var id = new Identifier(image.Repository.Name, image.Tags.OrderBy(tag => tag.Length).First(), image.Platform);

                try
                {
                    var imageInfo = harness.Inspect(id)[0];
                    var parentInfo = harness.Inspect(image.Parent)[0];
                    var imageSize = Int64.Parse(imageInfo["Size"].ToString());
                    var parentSize = Int64.Parse(parentInfo["Size"].ToString());

                    var baseId = harness.Ancestors(image).Last();

                    // Get packages if we are on Linux
                    var packages = new string[]{};
                    if (image.Platform.Os == "linux")
                    {
                        packages = harness.InstalledPackages(id, baseId.Name).Except(harness.InstalledPackages(image.Parent, baseId.Name)).ToArray();
                    }

                    report.AppendCsvRow(
                        id.Name, id.Tag,
                        image.Parent.Name, image.Parent.Tag,
                        baseId.Name, baseId.Tag,
                        imageSize, imageSize - parentSize,
                        packages.Length, String.Join(" ", packages),
                        imageInfo.ToString(), String.Join(" ", image.Tags)
                    );
                }
                catch (DockerException e)
                {
                    Console.Error.WriteLine($"Failed to gather information on {id.Name}:{id.Tag} due to {e}");
                }
            }

            return report.ToString();
        }

        public static string BenchviewReport(DockerHarness harness)
        {
            var lines = new List<string>();
            var unknowns = new HashSet<Identifier>();

            Func<Identifier, Image, string> generateLine = (id, image) =>
            {
                try
                {
                    var size = harness.Inspect(id)[0]["Size"].ToString();

                    var line = new List<string>();
                    line.Add($"{id.Platform.Os}/{id.Platform.Architecture}");
                    if (image != null)
                    {
                        var ancestors = harness.Ancestors(image).Reverse().Select(i => i.Name);
                        line.AddRange(ancestors);
                    }
                    line.Add(id.Name);
                    line.Add(id.Tag);
                    line.Add(size);

                    return String.Join(",", line.Select(s => Csv.Escape(s)));
                }
                catch (DockerException e)
                {
                    Console.Error.WriteLine($"Failed to gather information on {id.Name}:{id.Tag} due to {e}");
                    return null;
                }
            };

            foreach (var image in harness.SupportedImages())
            {
                foreach (var tag in image.Tags)
                {
                    var id = new Identifier(image.Repository.Name, tag, image.Platform);
                    unknowns.UnionWith(harness.Ancestors(image).Except(harness.Images.Keys));
                    lines.Add(generateLine(id, image));
                }
            }

            foreach (var id in unknowns)
            {
                lines.Add(generateLine(id, null));
            }

            var report = new StringBuilder();
            lines.Sort();
            foreach (var line in lines)
            {
                if (line != null)
                {
                    report.AppendLine(line);
                }
            }
            return report.ToString();
        }
    }
}
