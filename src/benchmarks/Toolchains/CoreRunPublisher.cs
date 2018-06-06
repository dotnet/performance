using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;

namespace Benchmarks.Toolchains
{
    public class CoreRunPublisher : IBuilder
    {
        public CoreRunPublisher(string coreRunPath, string customDotNetCliPath = null)
        {
            CoreRunPath = coreRunPath;
            DotNetCliPublisher = new DotNetCliPublisher(customDotNetCliPath);
        }

        private string CoreRunPath { get; }

        private DotNetCliPublisher DotNetCliPublisher { get; }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var buildResult = DotNetCliPublisher.Build(generateResult, buildPartition, logger);

            if (buildResult.IsBuildSuccess)
                UpdateDuplicatedDependencies(buildResult.ArtifactsPaths, logger);

            return buildResult;
        }
        
        /// <summary>
        /// update CoreRun folder with newer versions of duplicated dependencies
        /// </summary>
        private void UpdateDuplicatedDependencies(ArtifactsPaths artifactsPaths, ILogger logger)
        {
            var publishedDirectory = new DirectoryInfo(artifactsPaths.BinariesDirectoryPath);
            var coreRunDirectory =  new FileInfo(CoreRunPath).Directory;

            foreach (var publishedDependency in publishedDirectory
                .EnumerateFileSystemInfos()
                .Where(file => file.Extension == ".dll" || file.Extension == ".exe" ))
            {
                var coreRunDependency = new FileInfo(Path.Combine(coreRunDirectory.FullName, publishedDependency.Name));
                
                if (!coreRunDependency.Exists)
                    continue; // the file does not exist in CoreRun directory, we don't need to worry, it will be just loaded from publish directory by CoreRun

                var publishedVersionInfo = FileVersionInfo.GetVersionInfo(publishedDependency.FullName);
                var coreRunVersionInfo = FileVersionInfo.GetVersionInfo(coreRunDependency.FullName);
                
                if(!Version.TryParse(publishedVersionInfo.FileVersion, out var publishedVersion) || !Version.TryParse(coreRunVersionInfo.FileVersion, out var coreRunVersion))
                    continue;
                
                if(publishedVersion > coreRunVersion) 
                {
                    File.Copy(publishedDependency.FullName, coreRunDependency.FullName, overwrite: true); // we need to ovwerite old things with their newer versions
                    
                    logger.WriteLineInfo($"Copying {publishedDependency.FullName} to {coreRunDependency.FullName}");
                }
            }
        }
    }
}