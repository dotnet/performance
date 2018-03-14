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

    public class Git : IDisposable
    {
        public string Url { get; private set; }
        public string Branch {
            get => branch;
            set {
                if (branch != value)
                {
                    branch = value;
                    if (Location != null) {
                        Fetch();
                    }
                }
            }
        }
        public string Commit {
            get => commit;
            set {
                if (commit != value)
                {
                    commit = value;
                    if (Location != null) {
                        Checkout();
                    }
                }
            }
        }
        public DirectoryInfo Location { get; private set; }

        private List<string> fetched = new List<string>();

        public Git(string url, string branch=null, string commit=null)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
            Url = url;
            Branch = branch;
            Commit = commit;
        }

        public DirectoryInfo Clone()
        {
            if (Location == null)
            {
                Location = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "docker-benchmark", Util.RandomString(16)));
                using (var command = Util.Command("git", $"clone {Url} {Location}"))
                {
                    command.WaitForExit();
                }

                if (Branch != null)
                {
                    Fetch();
                }

                if (Commit != null)
                {
                    using (var command = Util.Command("git", $"checkout {Commit}", Location))
                    {
                        command.WaitForExit();
                    }
                }
            }

            return Location;
        }

        private void Fetch()
        {
            if (Branch != null)
            {
                if (!fetched.Contains(Branch));
                using (var command = Util.Command("git", $"fetch origin {Branch}", Location))
                {
                    command.WaitForExit();
                }
                fetched.Add(Branch);
            }
        }

        private void Checkout()
        {
            if (Commit != null)
            {
                using (var command = Util.Command("git", $"checkout {Commit}", Location))
                {
                    command.WaitForExit();
                }
            }
            else if (Branch != null)
            {
                using (var command = Util.Command("git", $"checkout {Branch}", Location))
                {
                    command.WaitForExit();
                }
            }
            else
            {
                using (var command = Util.Command("git", $"checkout master", Location))
                {
                    command.WaitForExit();
                }
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
                if (Location == null)
                {
                    Util.DeleteDirectory(Location);
                    Location = null;
                }
            }
        }

        private string branch;
        private string commit;
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
        private Dictionary<string, Git> gitCache = new Dictionary<string, Git>();

        private Git GitCache(Git git)
        {
            if (gitCache.TryGetValue(git.Url, out var cached))
            {
                cached.Branch = git.Branch;
                cached.Commit = git.Commit;
                return cached;
            }
            else
            {
                gitCache.Add(git.Url, git);
                git.Clone();
                return git;
            }
        }

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
                        Console.WriteLine($"{id.Name}:{id.Tag} for {id.Platform.OS}/{id.Platform.Architecture}");
                        repo.Images.Add(id, image);
                    }
                }
            }

            return repo;
        }

        public Repository LoadRepository(string gitUrl, string manifestPath = "manifest.json")
        {
            var git = GitCache(new Git(gitUrl));

            Repository repo = null;
            var manifest = File.ReadAllText(Path.Combine(git.Location.FullName, manifestPath));
            if (Path.GetExtension(manifestPath) == ".json")
            {
                repo = ParseManifest(manifest, git);
            }
            else if (Path.GetExtension(manifestPath) == String.Empty)
            {
                repo = ParseLibrary(manifest, Path.GetFileName(manifestPath));
            }
            else
            {
                throw new InvalidOperationException("Unrecognized file format");
            }

            // Parse each Dockerfile to extract the base image tag
            // This will result in a form of tree with root nodes being a out-of-repo images (including 'scratch')
            foreach (var image in repo.Images.Values)
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
                foreach (var git in gitCache.Values)
                {
                    git.Dispose();
                }
            }
        }
    }
}
