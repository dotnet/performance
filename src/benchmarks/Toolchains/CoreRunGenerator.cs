using System;
using System.IO;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.CsProj;

namespace Benchmarks.Toolchains
{
    public class CoreRunGenerator : CsProjGenerator
    {
        public CoreRunGenerator(string coreRunPath, string targetFrameworkMoniker, Func<Platform, string> platformProvider, string runtimeFrameworkVersion = null)
            : base(targetFrameworkMoniker, platformProvider, runtimeFrameworkVersion)
        {
            CoreRunPath = coreRunPath;
        }
        
        private string CoreRunPath { get; }

        protected override string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath) 
            => null; // we don't want to restore to a dedicated folder

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, "publish");
    }
}