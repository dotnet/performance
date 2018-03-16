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
                (other.Platform?.Equals(this.Platform) ?? this.Platform == null)
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
        public string Os { get; set; }
        public string Architecture { get; set; }

        public bool Equals(Platform other)
        {
            return (
                other != null &&
                other.Os == this.Os &&
                other.Architecture == this.Architecture
            );
        }

        public override int GetHashCode()
        {
            unchecked {
                return (
                    ((Os?.GetHashCode() ?? 0) * 163) ^
                    ((Architecture?.GetHashCode() ?? 0) * 197)
                );
            }
        }
    }

    public class Repository : IEquatable<Repository>
    {
        public string Name { get; set; }
        public IDictionary<Identifier, Image> Images = new Dictionary<Identifier, Image>();

        public bool Equals(Repository other) => other != null && this.Name == other.Name;
        public override int GetHashCode() => this.Name?.GetHashCode() ?? 0;
    }

    public class Image
    {
        public string Dockerfile { get; set; }
        public Git Git { get; set; }
        public Repository Repository { get; set; }
        public Platform Platform { get; set; }
        public ICollection<string> Tags { get; } = new HashSet<string>();
        public Identifier Base { get; set; }
    }

    public class DockerHarness : IDisposable
    {
        public ICollection<Repository> Repositories = new List<Repository>();
        public IDictionary<Identifier, Image> Images = new Dictionary<Identifier, Image>();

        public Platform Platform
        {
            get {
                if (plat == null)
                {
                    var stdout = Util.Command("docker", "version --format \"{{ .Server.Arch }}\n{{ .Server.Os }}\"", block: false);

                    var arch = stdout.ReadLine().Trim();
                    var os = stdout.ReadLine().Trim();
                    plat = new Platform {
                        Os = os,
                        Architecture = arch
                    };
                }

                return plat;
            }
        }

        public DockerHarness()
        {
            foreach (var identifier in ListImages())
            {
                pulledImages.Add(identifier);
            }
        }

        public void LoadManifest(string gitUrl, string manifestPath = "manifest.json")
        {
            var git = GitCache(new Git(gitUrl));

            IEnumerable<Repository> repos = null;
            var manifest = File.ReadAllText(Path.Combine(git.Location.FullName, manifestPath));
            if (Path.GetExtension(manifestPath) == ".json")
            {
                repos = ParseManifest(manifest, git);
            }
            else if (Path.GetExtension(manifestPath) == String.Empty)
            {
                repos = new Repository[]{ ParseLibrary(manifest, Path.GetFileName(manifestPath)) };
            }
            else
            {
                throw new InvalidOperationException("Unrecognized file format");
            }

            foreach (var repo in repos)
            {
                // Parse each Dockerfile to extract the base image tag
                // This will result in a form of tree with root nodes being a out-of-repo images (including 'scratch')
                foreach (var image in repo.Images.Values.OrderBy(img => img.Git.Commit))
                {
                    var dir = GitCache(image.Git).Location;

                    string filename = Path.Combine(dir.FullName, image.Dockerfile);
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

                foreach (var kv in repo.Images)
                {
                    // Add image to harness for harness-level lookup
                    // Will throw if a repo is loaded twice
                    this.Images.Add(kv.Key, kv.Value);
                }
                Repositories.Add(repo);
            }
        }

        public JArray Inspect(Identifier identifier)
        {
            if (pulledImages.Add(identifier))
            {
                // TODO: Handle disk full with your fancy LRU cache
                Util.Command("docker", $"pull {identifier.Name}:{identifier.Tag}");
            }

            return JArray.Parse(Util.Command("docker", $"image inspect {identifier.Name}:{identifier.Tag}", block: false).ReadToEnd());
        }

        public IEnumerable<Identifier> ListImages()
        {
            var stdout = Util.Command("docker", "image list --no-trunc --format \"{{ json . }}\"", block: false);

            string line = null;
            while ((line = stdout.ReadLine()) != null)
            {
                var imageJson = JObject.Parse(line);
                var identifier = new Identifier (
                    (imageJson["Repository"] as JValue).Value as string,
                    (imageJson["Tag"] as JValue).Value as string,
                    Platform
                );
                Debug.Assert(identifier.Name != null && identifier.Tag != null);
                yield return identifier;
            }
        }

        public ICollection<string> InstalledPackages(Identifier identifier, string baseName=null)
        {
            ICollection<string> result;
            if (packageCache.TryGetValue(identifier, out result))
            {
                return result;
            }

            string pkgManager = null;
            string listCommand;
            string listFormat;
            StreamReader stdout;

            switch (baseName ?? identifier.Name)
            {
                case "debian":
                case "ubuntu":
                case "buildpack-deps":
                    pkgManager = "dpkg";
                    break;
                case "alpine":
                    pkgManager = "apk";
                    break;
                case "centos":
                    pkgManager = "yum";
                    break;
                default:
                    // Figure out which package manager this image uses
                    stdout = Util.Command("docker", $"run --rm {identifier.Name}:{identifier.Tag} sh -c \"which apk dpkg 2> /dev/null || command -v yum 2> /dev/null\"", block: false);
                    var path = stdout.ReadToEnd().Trim();

                    foreach (var cmd in new[] { "dpkg", "apk", "yum" })
                    {
                        if (path.EndsWith(cmd))
                        {
                            pkgManager = cmd;
                            break;
                        }
                    }
                    if (pkgManager == null)
                    {
                        throw new NotSupportedException($"Could not determine package manager for '{identifier.Name}:{identifier.Tag}'");
                    }
                    break;
            }

            switch (pkgManager)
            {
                case "dpkg":
                    listCommand = "dpkg -l";
                    listFormat = @"^\w{2,3}\s+(?<name>[^:\s]+)*";
                    break;
                case "apk":
                    listCommand = "apk info";
                    listFormat = @"^(?<name>\S+)";
                    break;
                case "yum":
                    listCommand = "yum list installed";
                    listFormat = @"^(?<name>[^.\s]+).*@";
                    break;
                default:
                    throw new NotSupportedException($"Unrecognized package manager '{pkgManager}'");
            }

            stdout = Util.Command("docker", $"run --rm {identifier.Name}:{identifier.Tag} {listCommand}", block: false);

            string line = null;
            result = new List<string>();
            while ((line = stdout.ReadLine()) != null)
            {
                var match = Regex.Match(line, listFormat);
                if (match.Success) {
                    result.Add(match.Groups["name"].Value);
                }
            }

            Debug.Assert(result.Count > 0);
            packageCache.Add(identifier, result);
            return result;
        }

#region private
        private LruSet<Identifier> pulledImages = new LruSet<Identifier>();
        private IDictionary<Identifier, ICollection<string>> packageCache = new Dictionary<Identifier, ICollection<string>>();
        private IDictionary<string, Git> gitCache = new Dictionary<string, Git>();
        private Platform plat = null;

        private Git GitCache(Git git)
        {
            if (gitCache.TryGetValue(git.Url, out var cached))
            {
                // The cached Git repo has been cloned,
                // but may be on the wrong branch/commit
                cached.Branch = git.Branch;
                cached.Commit = git.Commit;
                return cached;
            }
            else
            {
                // Clone this repo and add it to the cache
                git.Clone();
                gitCache.Add(git.Url, git);
                return git;
            }
        }

        private IEnumerable<Repository> ParseManifest(string manifest, Git git) {
            // Read the manifest into a JSON object and pull out the repo
            var manifestJson = JObject.Parse(manifest);
            foreach (var repoJson in manifestJson["repos"])
            {
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
                            Os = platformJson["os"].ToString(),
                            Architecture = platformJson["architecture"]?.ToString() ?? "amd64"
                        };

                        // If there is more specification on OS or Arch add it after a ':'
                        if (platformJson["osVersion"] != null) {
                            plat.Os = String.Join(":", plat.Os, platformJson["osVersion"].ToString());
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

                yield return repo;
            }
        }

        private string PlatformKeyCombine(Platform platform, string key)
        {
            if (platform.Os == "linux")
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
                return String.Join("-", platform.Os, platform.Architecture, key);
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

            string defaultUrl, defaultBranch, defaultCommit;
            blocks[0].TryGetValue("GitRepo", out defaultUrl);
            blocks[0].TryGetValue("GitFetch", out defaultBranch);
            blocks[0].TryGetValue("GitCommit", out defaultCommit);

            foreach (var block in blocks.Skip(1))
            {
                // Tags is mandatory
                var tags = block["Tags"].Split(new[]{',', ' '}, StringSplitOptions.RemoveEmptyEntries);

                // Ignore shared tags unless code is written to resolve constraints
                // This means shared tags won't apear in the final output
                /*if (block.TryGetValue("SharedTags", out var sharedTags))
                {
                    tags = tags.Union(
                        sharedTags.Split(new[]{',', ' '}, StringSplitOptions.RemoveEmptyEntries)
                    ).ToArray();
                }*/

                List<Platform> platforms = null;
                if (block.TryGetValue("Architectures", out var platStrs))
                {
                    platforms = platStrs.Split(new[]{',', ' '}, StringSplitOptions.RemoveEmptyEntries).Select(platStr => {
                        var split = platStr.Split(new[]{'-'});
                        if (split.Length == 1)
                        {
                            return new Platform {
                                Os = "linux",
                                Architecture = split[0]
                            };
                        }
                        else
                        {
                            Debug.Assert(split.Length == 2);
                            return new Platform {
                                Os = split[0],
                                Architecture = split[1]
                            };
                        }
                    }).ToList();
                }
                else
                {
                    platforms = new List<Platform> { new Platform { Os = "linux", Architecture = "arm64" } };
                }

                foreach (var platform in platforms) {
                    string url, branch, commit;
                    if (!block.TryGetValue(PlatformKeyCombine(platform, "GitRepo"), out url))
                    {
                        if (!block.TryGetValue("GitRepo", out url))
                        {
                            url = defaultUrl;
                        }
                    }
                    if (!block.TryGetValue(PlatformKeyCombine(platform, "GitFetch"), out branch))
                    {
                        if (!block.TryGetValue("GitFetch", out branch))
                        {
                            branch = defaultBranch;
                        }
                    }
                    if (!block.TryGetValue(PlatformKeyCombine(platform, "GitCommit"), out commit))
                    {
                        if (!block.TryGetValue("GitCommit", out commit))
                        {
                            commit = defaultCommit;
                        }
                    }

                    string dockerfile = "Dockerfile";
                    if (block.TryGetValue(PlatformKeyCombine(platform, "Directory"), out var dir))
                    {
                        dockerfile = Path.Combine(dir, "Dockerfile");
                    }
                    else if (block.TryGetValue("Directory", out dir))
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
                        var id = new Identifier(repo.Name, tag, platform);
                        repo.Images.Add(id, image);
                    }
                }
            }

            return repo;
        }
#endregion

        public void Dispose()
        {
           Dispose(true);
           GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                foreach (var git in gitCache.Values)
                {
                    git.Dispose();
                }
            }
        }
    }
}
