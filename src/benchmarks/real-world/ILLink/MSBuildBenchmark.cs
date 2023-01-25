using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace ILLinkBenchmarks;

[BenchmarkCategory("ILLink")]
public class RunMSBuildPublish
{
    string _projectFilePath;
    string _projectOutputPath;
    string _iterationPublishPath;
    string _linkSemaphore;

    [GlobalSetup(Targets = new[] { nameof(LinkHelloWorld) })]
    public void LinkHelloWorldGlobalSetup()
    {
        _projectFilePath = Environment.GetEnvironmentVariable("ILLINK_SAMPLE_PROJECT");
        _projectOutputPath = Utilities.PublishSampleProject(_projectFilePath);
        _linkSemaphore = Path.Combine(_projectOutputPath, "link.semaphore");
        return;
    }

    [GlobalCleanup(Targets=new[] {nameof(LinkHelloWorld) })]
    public void LinkHelloWorldGlobalCleanup()
    {
        Directory.Delete(_projectOutputPath, recursive: true);
    }

    [Benchmark]
    [BenchmarkCategory("ILLink")]
    public string LinkHelloWorld()
    {
        _iterationPublishPath = Utilities.PublishSampleProject(_projectFilePath, "--no-build", "/p:PublishTrimmed=true", $"/p:_LinkSemaphore=\"{_linkSemaphore}\"");
        return _iterationPublishPath;
    }

    [IterationCleanup(Targets = new[] { nameof(LinkHelloWorld) })]
    public void LinkHelloWorldIterationCleanup ()
    {
        Directory.Delete(_iterationPublishPath, recursive: true);
        File.Delete(_linkSemaphore);
    }
}
