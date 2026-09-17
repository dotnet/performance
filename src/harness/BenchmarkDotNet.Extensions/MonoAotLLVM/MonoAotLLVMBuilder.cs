// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using BenchmarkDotNet.Code;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Extensions;

public class MonoAotLLVMBuilder : CsProjBuilder
{
    private readonly MonoAotLLVMSettings _settings;

    public MonoAotLLVMBuilder(MonoAotLLVMSettings settings) : base(settings)
    {
        _settings = settings;
        BenchmarkRunCallType = CodeGenBenchmarkRunCallType.Direct;
    }

    // The AOT compilation runs after Publish, so the app has to be published rather than built.
    protected override bool PublishesOutput => true;

    // The app is published against the custom runtime pack, which only provides Microsoft.NETCore.App, so the
    // frameworks another SDK would reference could not be resolved.
    protected override string GetSdkName(XElement benchmarkProject) => "Microsoft.NET.Sdk";

    protected override void AddEarlyProperties(XElement project, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, FileInfo projectFile)
    {
        project.Add(new XElement("PropertyGroup",
            new XElement("OriginalCSProjPath", projectFile.FullName),
            new XElement("MonoPropsPath", "$([System.IO.Path]::ChangeExtension('$(OriginalCSProjPath)', '.Mono.props'))"),
            new XElement("MonoTargetsPath", "$([System.IO.Path]::ChangeExtension('$(OriginalCSProjPath)', '.Mono.targets'))")));

        project.Add(new XElement("Import",
            new XAttribute("Project", "$(MonoPropsPath)"),
            new XAttribute("Condition", "Exists($(MonoPropsPath))")));

        base.AddEarlyProperties(project, buildPartition, artifactsPaths, projectFile);
    }

    protected override void AddProjectContent(XElement project, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, FileInfo projectFile)
    {
        base.AddProjectContent(project, buildPartition, artifactsPaths, projectFile);

        project.Add(new XElement("PropertyGroup",
            new XElement("MicrosoftNetCoreAppRuntimePackDir", _settings.CustomRuntimePack),
            new XElement("RuntimeIdentifier", RuntimeInformation.RuntimeIdentifier),
            new XElement("EnableTargetingPackDownload", "false"),
            new XElement("PublishTrimmed", "false"),
            new XElement("ErrorOnDuplicatePublishOutputFiles", "false")));

        // Self-contained is what makes the publish use the custom runtime pack. BenchmarkDotNet's reference-gathering
        // build disables the app host, which self-contained requires, so it is left out of that pass. It is set here
        // rather than passed as --self-contained, which would also restore the benchmark project for this RID.
        project.Add(new XElement("PropertyGroup",
            new XAttribute("Condition", "'$(BenchmarkDotNetGatherReferences)' != 'true'"),
            new XElement("SelfContained", "true")));

        project.Add(new XElement("ItemGroup",
            new XElement("PackageReference",
                new XAttribute("Include", "Microsoft.NET.Runtime.MonoAOTCompiler.Task"),
                new XAttribute("Version", "6.0.0-*"),
                new XAttribute("GeneratePathProperty", "true"))));

        project.Add(new XComment(" Redirect 'dotnet publish' to in-tree runtime pack "));
        project.Add(new XElement("Target",
            new XAttribute("Name", "TrickRuntimePackLocation"),
            new XAttribute("AfterTargets", "ProcessFrameworkReferences"),
            new XElement("ItemGroup",
                new XElement("RuntimePack",
                    new XElement("PackageDirectory", "$(MicrosoftNetCoreAppRuntimePackDir)"))),
            new XElement("Message",
                new XAttribute("Text", "Packaged ID: %(RuntimePack.PackageDirectory)"),
                new XAttribute("Importance", "high"))));

        project.Add(new XElement("UsingTask",
            new XAttribute("TaskName", "MonoAOTCompiler"),
            new XAttribute("AssemblyFile", "$(PkgMicrosoft_NET_Runtime_MonoAOTCompiler_Task)/MonoAOTCompiler.dll")));

        project.Add(new XElement("Target",
            new XAttribute("Name", "AotApp"),
            new XAttribute("AfterTargets", "Publish"),
            new XElement("PropertyGroup",
                new XElement("PublishDirFullPath", "$([System.IO.Path]::GetFullPath($(PublishDir)))"),
                new XElement("SharedLibraryType", new XAttribute("Condition", "$([MSBuild]::IsOSPlatform('OSX'))"), "Dylib"),
                new XElement("SharedLibraryType", new XAttribute("Condition", "$([MSBuild]::IsOSPlatform('Windows'))"), "Dll"),
                new XElement("SharedLibraryType", new XAttribute("Condition", "'$(SharedLibraryType)' == ''"), "So")),
            new XElement("ItemGroup",
                new XElement("AotInputAssemblies",
                    new XAttribute("Include", "$(PublishDirFullPath)\\*.dll"),
                    new XElement("AotArguments", "mcpu=native"))),
            new XElement("MonoAOTCompiler",
                new XAttribute("CompilerBinaryPath", _settings.AotCompilerPath),
                new XAttribute("Mode", "Normal"),
                new XAttribute("OutputType", "Library"),
                new XAttribute("LibraryFormat", "$(SharedLibraryType)"),
                new XAttribute("Assemblies", "@(AotInputAssemblies)"),
                new XAttribute("UseLLVM", _settings.AotCompilerMode == MonoAotCompilerMode.llvm ? "true" : "false"),
                new XAttribute("LLVMPath", "$(MicrosoftNetCoreAppRuntimePackDir)\\runtimes\\$(RuntimeIdentifier)\\native"),
                new XAttribute("OutputDir", "$(PublishDir)"),
                new XAttribute("UseAotDataFile", "false"),
                new XAttribute("IntermediateOutputPath", "$(IntermediateOutputPath)"),
                new XElement("Output",
                    new XAttribute("TaskParameter", "CompiledAssemblies"),
                    new XAttribute("ItemName", "BundleAssemblies"))),
            new XElement("Message",
                new XAttribute("Text", "CompiledAssemblies: $(BundleAssemblies)"),
                new XAttribute("Importance", "high"))));
    }

    protected override void AddLateProperties(XElement project, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, FileInfo projectFile)
    {
        base.AddLateProperties(project, buildPartition, artifactsPaths, projectFile);

        // After the late properties so the Mono targets can override anything the generated project defines.
        project.Add(new XElement("Import",
            new XAttribute("Project", "$(MonoTargetsPath)"),
            new XAttribute("Condition", "Exists($(MonoTargetsPath))")));
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
