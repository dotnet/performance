// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Extensions;

public class MonoAotLLVMGenerator : CsProjGenerator
{
    private readonly MonoAotLLVMSettings _settings;

    public MonoAotLLVMGenerator(MonoAotLLVMSettings settings)
        : base(settings)
    {
        _settings = settings;
        BenchmarkRunCallType = CodeGenBenchmarkRunCallType.Direct;
    }

    protected override async ValueTask GenerateProjectAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger, CancellationToken cancellationToken)
    {
        BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
        var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

        var xmlDoc = new XmlDocument();
        xmlDoc.Load(projectFile.FullName);
        var (customProperties, _) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);

        string template = await LoadTemplateAsync(cancellationToken).ConfigureAwait(false);
        string content = new StringBuilder(template)
            .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
            .Replace("$CSPROJPATH$", projectFile.FullName)
            .Replace("$TFM$", Settings.TargetFrameworkMoniker)
            .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
            .Replace("$COPIEDSETTINGS$", customProperties)
            .Replace("$RUNTIMEPACK$", _settings.CustomRuntimePack)
            .Replace("$COMPILERBINARYPATH$", _settings.AotCompilerPath)
            .Replace("$RUNTIMEIDENTIFIER$", RuntimeInformation.RuntimeIdentifier)
            .Replace("$USELLVM$", _settings.AotCompilerMode == MonoAotCompilerMode.llvm ? "true" : "false")
            .ToString();

        await File.WriteAllTextAsync(artifactsPaths.ProjectFilePath, content, cancellationToken).ConfigureAwait(false);

        await GatherReferencesAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> LoadTemplateAsync(CancellationToken cancellationToken)
    {
        var assembly = typeof(MonoAotLLVMGenerator).Assembly;
        string resourceName = System.Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith("MonoAOTLLVMCsProj.txt"))
            ?? throw new FileNotFoundException("Embedded resource MonoAOTLLVMCsProj.txt not found");
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override string GetPublishDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
        => Path.Combine(GetBinariesDirectoryPath(buildArtifactsDirectoryPath, configuration), "publish");

    protected override string GetExecutablePath(string binariesDirectoryPath, string programName)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(binariesDirectoryPath, "publish", $"{programName}.exe")
            : Path.Combine(binariesDirectoryPath, "publish", programName);

    protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
        => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, Settings.TargetFrameworkMoniker, RuntimeInformation.RuntimeIdentifier);
}
