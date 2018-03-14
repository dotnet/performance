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
        static void Main(string[] args)
        {
            var report = new StringBuilder();
            using (var harness = new DockerHarness())
            {
                var repos = new List<Repository> {
                    harness.LoadRepository("https://github.com/dotnet/dotnet-docker"),
                    harness.LoadRepository("https://github.com/docker-library/official-images/", "library/openjdk")
                };
                
                report.AppendCsvRow("Name","Tag","BaseName","BaseTag","Size","DiffSize","NumPackages","Packages");
                    
                var seen = new HashSet<Image>(new IdentityEqualityComparer<Image>());
                var os = harness.SupportedOS();

                foreach (var repo in repos)
                {
                    foreach (var image in repo.Images.Values)
                    {
                        if (!seen.Contains(image))
                        {
                            seen.Add(image);
                            if (image.Platform.OS == os && image.Platform.Architecture == "amd64")
                            {
                                var id = new Identifier(repo.Name, image.Tags.OrderBy(tag => tag.Length).First(), image.Platform);
                                var imageInfo = harness.Inspect(id)[0];
                                var baseInfo = harness.Inspect(image.Base)[0];
                                var imageSize = Int64.Parse(imageInfo["Size"].ToString());
                                var baseSize = Int64.Parse(baseInfo["Size"].ToString());

                                var foundation = id;
                                while (repo.Images.TryGetValue(foundation, out var img)) {
                                    foundation = img.Base;
                                }

                                var packages = harness.InstalledPackages(id, foundation.Name).Except(harness.InstalledPackages(image.Base, foundation.Name)).ToList();

                                report.AppendCsvRow(id.Name, id.Tag, image.Base.Name, image.Base.Tag, imageSize, imageSize - baseSize, packages.Count, String.Join(" ", packages));
                            }
                        }
                    }
                }
            }
            string reportText = report.ToString();
            Console.WriteLine(reportText);
            File.WriteAllText("report.csv", reportText);
        }
    }
}
