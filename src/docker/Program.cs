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
                harness.LoadManifest("https://github.com/dotnet/dotnet-docker");
                harness.LoadManifest("https://github.com/aspnet/aspnet-docker");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/openjdk");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/mono");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/python");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/pypy");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/golang");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/ruby");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/buildpack-deps");
                harness.LoadManifest("https://github.com/docker-library/official-images/", "library/php");

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
                        }
                    }
                }
            }

            string reportText = report.ToString();
            Console.WriteLine(reportText);
            File.WriteAllText("reports/report.csv", reportText);
        }
    }
}
