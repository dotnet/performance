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
    public class Identifier : IEquatable<Identifier>
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public Platform Platform { get; set; }

        public Identifier(string name, string tag, Platform platform)
        {
            Name = name;
            Tag = tag;
            Platform = platform;
        }

        public bool Equals(Identifier other)
        {
            return (
                other != null &&
                other.Name == this.Name &&
                other.Tag == this.Tag &&
                (other.Platform?.Equals(this.Platform) ?? false)
            );
        }

        public override int GetHashCode()
        {
            unchecked {
                return (
                    ((Name?.GetHashCode() ?? 0) * 29) ^
                    ((Tag?.GetHashCode() ?? 0) * 83) ^
                    ((Platform?.GetHashCode() ?? 0) * 131)
                );
            }
        }
    }

    public class Platform : IEquatable<Platform>
    {
        public string OS { get; set; }
        public string Architecture { get; set; }

        public bool Equals(Platform other)
        {
            return (
                other != null &&
                other.OS == this.OS &&
                other.Architecture == this.Architecture
            );
        }

        public override int GetHashCode()
        {
            unchecked {
                return (
                    ((OS?.GetHashCode() ?? 0) * 163) ^
                    ((Architecture?.GetHashCode() ?? 0) * 197)
                );
            }
        }
    }

    public struct Reference<T> {
        public T Value { get; set; }
        public int Count { get; set; }

        public Reference(T val)
        {
            Value = val;
            Count = 1;
        }
    }

    public class Git : IDisposable, IEquatable<Git>
    {
        public string Url { get; private set; }
        public string Branch { get; private set; }
        public string Commit { get; private set; }
        public DirectoryInfo Location { get; private set; }

        private static Dictionary<Git, Reference<DirectoryInfo>> clones = new Dictionary<Git, Reference<DirectoryInfo>>();

        public Git(string url, string branch=null, string commit=null)
        {
            Url = url;
            Branch = branch;
            Commit = commit;
        }

        public DirectoryInfo Checkout()
        {
            if (Location == null)
            {
                if (clones.TryGetValue(this, out var reference))
                {
                    Location = reference.Value;
                    reference.Count++;
                }
                else
                {
                    Location = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Util.RandomString(16)));
                    using (var command = Util.Command("git", $"clone {Url} {Location}"))
                    {
                        command.WaitForExit();
                    }

                    if (Branch != null)
                    {
                        using (var command = Util.Command("git", $"fetch origin {Branch}", Location))
                        {
                            command.WaitForExit();
                        }

                        if (Commit == null) {
                            using (var command = Util.Command("git", $"checkout {Branch}", Location))
                            {
                                command.WaitForExit();
                            }
                        }
                    }

                    if (Commit != null)
                    {
                        using (var command = Util.Command("git", $"checkout {Commit}", Location))
                        {
                            command.WaitForExit();
                        }
                    }

                    clones.Add(this, new Reference<DirectoryInfo>(Location));
                }
            }

            return Location;
        }

        public void Dispose()
        {
           Dispose(true);
           GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                if (Location == null)
                {
                    if (clones.TryGetValue(this, out var reference))
                    {
                        Debug.Assert(this.Location.FullName == reference.Value.FullName);
                        reference.Count--;
                        if (reference.Count <= 0) {
                            Util.DeleteDirectory(reference.Value);
                            clones.Remove(this);
                        }
                        Location = null;
                    }
                }
            }
        }

        public bool Equals(Git other)
        {
            return (
                other != null &&
                other.Url == this.Url &&
                other.Branch == this.Branch &&
                other.Commit == this.Commit
            );
        }

        public override int GetHashCode()
        {
            unchecked {
                return (
                    ((Url?.GetHashCode() ?? 0) * 29) ^
                    ((Branch?.GetHashCode() ?? 0) * 83) ^
                    ((Commit?.GetHashCode() ?? 0) * 131)
                );
            }
        }
    }

    public class Repository
    {
        public string Name { get; set; }
        public Dictionary<Identifier, Image> Images = new Dictionary<Identifier, Image>();
    }

    public class Image
    {
        public string Dockerfile { get; set; }
        public Git Git { get; set; }
        public Repository Repository { get; set; }
        public Platform Platform { get; set; }
        public HashSet<string> Tags { get; } = new HashSet<string>();
        public Identifier Base { get; set; }
    }

    public class DockerHarness : IDisposable
    {
        private HashSet<Identifier> pulledImages = new HashSet<Identifier>();
        private List<Git> gitRepos = new List<Git>();

        private Repository ParseManifest(string manifest, Git git) {
            // Read the manifest into a JSON object and pull out the repo
            var repoJson = JObject.Parse(manifest)["repos"][0];

            var repo = new Repository {
                Name = repoJson["name"].ToString()
            };

            // Parse the manifest to pull out image information
            foreach (var imageJson in repoJson["images"])
            {
                foreach (var platformJson in imageJson["platforms"])
                {
                    // Get the platform specification
                    var plat = new Platform {
                        OS = platformJson["os"].ToString(),
                        Architecture = platformJson["architecture"]?.ToString() ?? "amd64"
                    };

                    // If there is more specification on OS or Arch add it after a ':'
                    if (platformJson["osVersion"] != null) {
                        plat.OS = String.Join(":", plat.OS, platformJson["osVersion"].ToString());
                    }
                    if (platformJson["variant"] != null) {
                        plat.Architecture = String.Join(":", plat.Architecture, platformJson["variant"].ToString());
                    }

                    var img = new Image {
                        Platform = plat,
                        Dockerfile = platformJson["dockerfile"].ToString(),
                        Repository = repo,
                        Git = git
                    };

                    // Assign all the tags that map to this image
                    foreach (var tag in (platformJson["tags"] as JObject).Properties())
                    {
                        img.Tags.Add(tag.Name);
                        repo.Images.Add(new Identifier(repo.Name, tag.Name, plat), img);
                    }

                    if (imageJson["sharedTags"] != null)
                    {
                        foreach (var tag in (imageJson["sharedTags"] as JObject).Properties())
                        {
                            img.Tags.Add(tag.Name);
                            repo.Images.Add(new Identifier(repo.Name, tag.Name, plat), img);
                        }
                    }
                }
            }

            return repo;
        }

        private string PlatformKeyCombine(Platform platform, string key)
        {
            if (platform.OS == "linux")
            {
                if (platform.Architecture == "amd64")
                {
                    return key;
                }
                else
                {
                    return String.Join("-", platform.Architecture, key);
                }
            }
            else
            {
                return String.Join("-", platform.OS, platform.Architecture, key);
            }
        }

        private Repository ParseLibrary(string library, string name)
        {
            var blocks = Rfc2822.Parse(library);

            if (!(blocks.Count > 0)) {
                throw new InvalidOperationException("Input is not a proper library. No blocks were found");
            }

            var repo = new Repository {
                Name = name,
            };

            string defaultUrl = null;
            string defaultBranch = null;
            string defaultCommit = null;
            blocks[0].TryGetValue("GitRepo", out defaultBranch);
            blocks[0].TryGetValue("GitFetch", out defaultBranch);
            blocks[0].TryGetValue("GitCommit", out defaultCommit);

            foreach (var block in blocks.Skip(1))
            {
                // Tags is mandatory
                var tags = block["Tags"].Split(new char[]{',', ' '});
                List<Platform> platforms = null;
                if (!block.TryGetValue("Architectures", out var platStrs))
                {
                    platforms = platStrs.Split(new char[]{',', ' '}).Select(platStr => {
                        var split = platStr.Split(new char[]{'-'});
                        if (split.Length == 1)
                        {
                            return new Platform {
                                OS = "linux",
                                Architecture = split[0]
                            };
                        }
                        else
                        {
                            Debug.Assert(split.Length == 2);
                            return new Platform {
                                OS = split[0],
                                Architecture = split[1]
                            };
                        }
                    }).ToList();
                }
                else
                {
                    platforms = new List<Platform> { new Platform { OS = "linux", Architecture = "arm64" } };
                }

                foreach (var platform in platforms) {
                    string url, branch, commit;
                    if (!block.TryGetValue(PlatformKeyCombine(platform, "GitRepo"), out url))
                    {
                        url = defaultUrl;
                    }
                    if (!block.TryGetValue(PlatformKeyCombine(platform, "GitFetch"), out branch))
                    {
                        branch = defaultBranch;
                    }
                    if (!block.TryGetValue(PlatformKeyCombine(platform, "GitCommit"), out commit))
                    {
                        commit = defaultCommit;
                    }

                    string dockerfile = "Dockerfile";
                    if (block.TryGetValue(PlatformKeyCombine(platform, "Directory"), out var dir))
                    {
                        dockerfile = Path.Combine(dir, "Dockerfile");
                    }

                    var image = new Image {
                        Dockerfile = dockerfile,
                        Git = new Git(url, branch, commit),
                        Repository = repo,
                        Platform = platform,
                    };

                    foreach (var tag in tags)
                    {
                        image.Tags.Add(tag);
                        repo.Images.Add(new Identifier(repo.Name, tag, platform), image);
                    }
                }
            }

            return repo;
        }

        public Repository LoadRepository(Git git, string manifestPath = "manifest.json")
        {
            var gitDir = git.Checkout();
            gitRepos.Add(git);

            // Create a Repository object from a manifest
            var repo = ParseManifest(File.ReadAllText(Path.Combine(gitDir.FullName, manifestPath)), git);

            // Parse each Dockerfile to extract the base image tag
            // This will result in a form of tree with root nodes being a out-of-repo images (including 'scratch')
            foreach (var image in repo.Images.Values)
            {
                string filename = Path.Combine(gitDir.FullName, image.Dockerfile);
                if (File.GetAttributes(filename).HasFlag(FileAttributes.Directory))
                {
                    filename = Path.Combine(filename, "Dockerfile");
                }

                using (var dockerfile = new StreamReader(filename))
                {
                    // Regex to match a 'FROM' line and pull out the image name and tag
                    // Assumption: The base image is not specified in any ARG options
                    var pattern = new Regex(@"^FROM\s+(?<name>(?:\S+/)?(?:[^:\s]+))(?::(?<tag>\S+))?");
                    string baseName = null;
                    string baseTag = null;
                    string line = null;

                    // Read every line because the last ocurrance of 'FROM' determines the base image
                    while ((line = dockerfile.ReadLine()) != null)
                    {
                        var match = pattern.Match(line);
                        if (match.Success)
                        {
                            baseName = match.Groups["name"].Value;
                            if (match.Groups["tag"].Success) {
                                baseTag = match.Groups["tag"].Value;
                            }
                            else {
                                baseTag = "latest";
                            }
                        }
                    }

                    Debug.Assert(baseName != null && baseTag != null, $"Failed to find the base image from {filename}");

                    image.Base = new Identifier(baseName, baseTag, image.Platform);
                }
            }

            return repo;
        }

        public string SupportedOS()
        {
            using (var command = Util.Command("docker", "info --format \"{{ .OSType }}\""))
            {
                return command.StandardOutput.ReadToEnd().Trim();
            }
        }

        public JArray Inspect(Identifier identifier)
        {
            if (!pulledImages.Contains(identifier))
            {
                using (var command = Util.Command("docker", $"pull {identifier.Name}:{identifier.Tag}"))
                {
                    command.WaitForExit();
                }
                pulledImages.Add(identifier);
            }

            using (var command = Util.Command("docker", $"image inspect {identifier.Name}:{identifier.Tag}"))
            {
                return JArray.Parse(command.StandardOutput.ReadToEnd());
            }
        }

        public IEnumerable<string> InstalledPackages(Identifier identifier, string baseName=null)
        {
            string listCommand;
            string listFormat;

            switch (baseName ?? identifier.Name)
            {
                case "debian":
                case "ubuntu":
                case "buildpack-deps":
                    listCommand = "dpkg -l";
                    listFormat = @"^\w{2,3}\s+(?<name>[^:\s]+)*";
                    break;
                case "alpine":
                    listCommand = "apk info";
                    listFormat = @"^(?<name>\S+)";
                    break;
                case "centos":
                    listCommand = "yum list installed";
                    listFormat = @"^(?<name>[^.\s]+).*@";
                    break;
                default:
                    throw new NotSupportedException($"Unable to list packages for {identifier.Name}");
            }

            using (var command = Util.Command("docker", $"run --rm {identifier.Name}:{identifier.Tag} {listCommand}"))
            {
                string line = null;
                bool yielded = false;

                // Read every line because the last ocurrance of 'FROM' determines the base image
                while ((line = command.StandardOutput.ReadLine()) != null)
                {
                    var match = Regex.Match(line, listFormat);
                    if (match.Success) {
                        yield return match.Groups["name"].Value;
                        yielded = true;
                    }
                }
                Debug.Assert(yielded);
            }
        }

        public void Dispose()
        {
           Dispose(true);
           GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                foreach (var git in gitRepos)
                {
                    git.Dispose();
                }
            }
        }
    }
}
