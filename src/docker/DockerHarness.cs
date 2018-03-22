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
using System.Runtime.Serialization;

namespace DockerHarness
{
    /// <summary>
    ///   Identifier uniqely identifies an image which can be pulled from Dockerhub
    ///   Multiple Identifiers may point to a single image
    /// </summary>
    public class Identifier : IEquatable<Identifier>
    {
        /// <summary>
        ///   The repository name (e.g. microsoft/dotnet)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   The image tag (e.g. 2-runtime)
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        ///   The platform the identified image supports
        /// </summary>
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

        public override bool Equals(object obj)
        {
            return Equals(obj as Identifier);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (
                    ((Name?.GetHashCode() ?? 0) * 29) ^
                    ((Tag?.GetHashCode() ?? 0) * 83) ^
                    ((Platform?.GetHashCode() ?? 0) * 131)
                );
            }
        }
    }

    /// <summary>
    ///   Platform is a combination of hardware and OS combination
    ///   It is important to uniqely identify images and determine compatability
    ///   This class uses Golang conventions for OS and arch names (Docker is written in Go)
    ///   https://gist.github.com/asukakenji/f15ba7e588ac42795f421b48b8aede63
    /// </summary>
    public class Platform : IEquatable<Platform>
    {
        /// <summary>
        ///   The operating system of the host/image (e.g. linux, windows)
        /// </summary>
        public string Os { get; set; }

        /// <summary>
        ///   The architecture of the Docker host (e.g. amd64, arm)
        /// </summary>
        public string Architecture { get; set; }

        public bool Equals(Platform other)
        {
            return (
                other != null &&
                other.Os == this.Os &&
                other.Architecture == this.Architecture
            );
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Platform);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (
                    ((Os?.GetHashCode() ?? 0) * 163) ^
                    ((Architecture?.GetHashCode() ?? 0) * 197)
                );
            }
        }
    }

    /// <summary>
    ///   Repository contains information about a Dockerhub repository
    /// </summary>
    public class Repository : IEquatable<Repository>
    {
        /// <summary>
        ///   The name of the repository (e.g. microsoft/dotnet)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   A dictionary of identifies to Image objects this repository contains
        ///   Many identifies may map to each Image
        /// </summary>
        public IDictionary<Identifier, Image> Images = new Dictionary<Identifier, Image>();

        public bool Equals(Repository other) => other != null && this.Name == other.Name;
        public override bool Equals(object obj) => Equals(obj as Repository);
        public override int GetHashCode() => this.Name?.GetHashCode() ?? 0;
    }

    /// <summary>
    ///   Image is a collection of information about the image
    /// </summary>
    public class Image
    {
        /// <summary>
        ///   The path to the Dockerfile, within the git repo, which built the image
        /// </summary>
        public string Dockerfile { get; set; }

        /// <summary>
        ///   The git repository from which this image is built
        /// </summary>
        public Git Git { get; set; }

        /// <summary>
        ///   The Dockerhub repo which contains this image
        /// </summary>
        public Repository Repository { get; set; }

        /// <summary>
        ///   The platform which this image is built for
        /// </summary>
        public Platform Platform { get; set; }
        
        /// <summary>
        ///   All tags which refer to this image in it's reposiroty
        /// </summary>
        public ICollection<string> Tags { get; } = new HashSet<string>();

        /// <summary>
        ///   The uniqe identifier for the image this image is built from
        /// </summary>
        public Identifier Parent { get; set; }
    }

    [Serializable()]
    internal class DockerException : CommandException
    {
        public DockerException() { }
        public DockerException(string msg) : base(msg) { }
        public DockerException(string msg, Exception inner) : base(msg, inner) { }
        protected DockerException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    /// <summary>
    ///   DockerHarness allows analysis of docker repositories, images, and containers
    /// </summary>
    public class DockerHarness : IDisposable
    {
        /// <summary>
        ///   Collection of Dockerhub repositories loaded into this harness
        /// </summary>
        public ICollection<Repository> Repositories = new List<Repository>();

        /// <summary>
        ///   Dictionary of all Images loaded into this harness
        /// </summary>
        public IDictionary<Identifier, Image> Images = new Dictionary<Identifier, Image>();

        /// <summary>
        ///   The platform this Docker host currently supports
        ///   On Windows this may be linux/amd64 or windows/amd64
        /// </summary>
        public Platform Platform
        {
            get
            {
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

        /// <summary>
        ///   Yields identifiers for all images currently pulled on this host
        /// </summary>
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

        /// <summary>
        ///   Yields all images which are compatible with this host
        ///   An image is compatible if it is built for the same OS / Architecture
        /// </summary>
        public IEnumerable<Image> SupportedImages()
        {
            var seen = new HashSet<Image>(new IdentityEqualityComparer<Image>());

            foreach (var image in Images.Values)
            {
                if (seen.Add(image) && image.Platform.Equals(Platform))
                {
                    yield return image;
                }
            }
        }

        /// <summary>
        ///   Walks the image tree to identify the root image (e.g. debian, alpine)
        ///   Returns the first identifier this harness does not recognize
        /// </summary>
        public Identifier Base(Image image)
        {
            var baseId = image.Parent;
            while (baseId != null && Images.TryGetValue(baseId, out var img)) {
                baseId = img.Parent;
            }
            return baseId;
        }

        /// <summary>
        ///   Given the url of a git repository and the path to the manifest file
        ///   (either in official-library format or JSON format) this will parse
        ///   and load the manifest into this harness
        /// </summary>
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
                                else
                                {
                                    baseTag = "latest";
                                }
                            }
                        }

                        Debug.Assert(baseName != null && baseTag != null, $"Failed to find the base image from {filename}");

                        image.Parent = new Identifier(baseName, baseTag, image.Platform);
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

        /// <summary>
        ///   Returns to output of `docker image inspect` as a parsed JSON array
        /// </summary>
        public JArray Inspect(Identifier identifier)
        {
            PullImage(identifier);
            return JArray.Parse(Util.Command("docker", $"image inspect {identifier.Name}:{identifier.Tag}", block: false).ReadToEnd());
        }

        /// <summary>
        ///   Uses the appropriate package manager to get all packages installed in a given image
        ///   Works for images based on debian, alpine, and centos/rhell
        ///   The basename parameter provides a hint to which distrobution the image is
        ///   (Only works for Linux containers)
        /// </summary>
        public ICollection<string> InstalledPackages(Identifier identifier, string baseName=null)
        {
            ICollection<string> result;
            if (packageCache.TryGetValue(identifier, out result))
            {
                return result;
            }

            // Ensure the image is on disk
            // Not strictly needed because `run` will do this automatically,
            // but we do this explicily it will be tracked in our cache
            PullImage(identifier);

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
                    stdout = Util.Command("docker", $"run --rm {identifier.Name}:{identifier.Tag} sh -c \"which apk dpkg 2> /dev/null || command -v yum 2> /dev/null\"", block: false, handler: (p) => p.ExitCode == 127);
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

        /// <summary>
        ///   Removes some of portion of pulled image tags from disk (cache)
        ///   The images are removed in least-recently-used (LRU) order and
        ///   1/5 of pulled images are deleted each time
        /// </summary>
        private void EvictImages()
        {
            int count = pulledImages.Count / 5; // This ratio is more or less arbitrary
            for (int i=0; i < count; i++)
            {
                var evicted = pulledImages.Evict();
                // TODO: This may fail under certain conditions. Consider how to handle it
                // NOTE: `image rm` removes the tag. The image will only be delted when all of it's tags are deleted
                Util.Command("docker", $"image rm {evicted.Name}:{evicted.Tag}");
            }
        }

        /// <summary>
        ///   Ensures that the identified image resides on this server
        ///   The image will be pulled if the tag is not in a set of known pulled images
        ///   Images may be removed to make space on the disk by calling EvictImages
        /// </summary>
        private void PullImage(Identifier identifier)
        {
            // Pull the docker image. If there is not enough space on disk, evict some images and try again
            while (!pulledImages.Refresh(identifier))
            {
                using (var process = Util.Run("docker", $"pull {identifier.Name}:{identifier.Tag}"))
                {
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        if (process.StandardError.ReadToEnd().Contains("no space left on device"))
                        {
                            EvictImages();
                            continue;
                        }
                        throw new DockerException($"Docker image pull exited with code {process.ExitCode}. Error output follows:\n{process.StandardError.ReadToEnd()}");
                    }
                    else
                    {
                        pulledImages.Add(identifier);
                    }
                }
            }
        }

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

        private IEnumerable<Repository> ParseManifest(string manifest, Git git)
        {
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
