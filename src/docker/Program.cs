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
        public static string KustoReportPath = Environment.GetEnvironmentVariable("KUSTO_REPORT_PATH") ?? "reports/kusto.csv";

        static void Main(string[] args)
        {
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
            }
        }

        public static string KustoReport(DockerHarness harness)
        {
            var report = new StringBuilder();
            var seen = new HashSet<Image>(new IdentityEqualityComparer<Image>());

            report.AppendCsvRow("Name","Tag","ParentName","ParentTag","BaseName","BaseTag","Size","AddedSize","NumAddedPackages","AddedPackages","Inspect", "AllTags");
            foreach (var repo in harness.Repositories)
            {
                foreach (var image in repo.Images.Values)
                {
                    if (seen.Add(image))
                    {
                        if (image.Platform.Equals(harness.Platform))
                        {
                            var id = new Identifier(repo.Name, image.Tags.OrderBy(tag => tag.Length).First(), image.Platform);
                            try
                            {
                                var imageInfo = harness.Inspect(id)[0];
                                var parentInfo = harness.Inspect(image.Parent)[0];
                                var imageSize = Int64.Parse(imageInfo["Size"].ToString());
                                var parentSize = Int64.Parse(parentInfo["Size"].ToString());

                                var foundation = id;
                                while (harness.Images.TryGetValue(foundation, out var img)) {
                                    foundation = img.Parent;
                                }

                                var packages = new string[]{};
                                if (image.Platform.Os == "linux")
                                {
                                    packages = harness.InstalledPackages(id, foundation.Name).Except(harness.InstalledPackages(image.Parent, foundation.Name)).ToArray();
                                }

                                report.AppendCsvRow(
                                    id.Name, id.Tag,
                                    image.Parent.Name, image.Parent.Tag,
                                    foundation.Name, foundation.Tag,
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
                    }
                }
            }

            return report.ToString();
        }
    }
}
