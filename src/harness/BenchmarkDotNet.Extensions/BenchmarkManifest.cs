using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.Roslyn;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BenchmarkDotNet.Extensions
{
    public class BenchmarkManifest
    {
        public List<string>? BenchmarkCases { get; set; }
        public JobSettings? BaseJob { get; set; }
        public Dictionary<string, JobSettings>? Jobs { get; set; }
        public Dictionary<string, RunSettings>? BenchmarkCaseRunOverrides { get; set; }

        public class JobSettings
        {
            public EnvironmentSettings? Environment { get; set; }
            public RunSettings? Run { get; set; }
            public InfrastructureSettings? Infrastructure { get; set; }
            public AccuracySettings? Accuracy { get; set; }

            public Job ModifyJob(Job job)
            {
                if (Environment is not null)
                    job = Environment.ModifyJob(job);
                if (Run is not null)
                    job = Run.ModifyJob(job);
                if (Infrastructure is not null)
                    job = Infrastructure.ModifyJob(job);
                if (Accuracy is not null)
                    job = Accuracy.ModifyJob(job);
                return job;
            }
        }

        public class EnvironmentSettings
        {
            public Platform? Platform { get; set; }
            public Jit? Jit { get; set; }
            public RuntimeSettings? Runtime { get; set; }
            public int? Affinity { get; set; }
            public GcSettings? Gc { get; set; }
            public Dictionary<string, string>? EnvironmentVariables { get; set; }
            public PowerPlan? PowerPlan { get; set; }
            public Guid? PowerPlanGuid { get; set; }
            public bool? LargeAddressAware { get; set; }

            public Job ModifyJob(Job job)
            {
                if (Platform is Platform platform)
                    job = job.WithPlatform(platform);
                if (Jit is Jit jit)
                    job = job.WithJit(jit);
                if (Runtime is not null)
                    job = Runtime.ModifyJob(job);
                if (Affinity is int affinity)
                    job = job.WithAffinity((IntPtr)affinity);
                if (Gc is not null)
                    job = Gc.ModifyJob(job);
                if (EnvironmentVariables is not null)
                    job = job.WithEnvironmentVariables(EnvironmentVariables.Select(e => new EnvironmentVariable(e.Key, e.Value)).ToArray());
                if (PowerPlan is PowerPlan powerPlan)
                    job = job.WithPowerPlan(powerPlan);
                if (PowerPlanGuid is Guid guid)
                    job = job.WithPowerPlan(guid);
                if (LargeAddressAware is bool)
                    job = job.WithLargeAddressAware(LargeAddressAware.Value);
                return job;
            }
        }

        public class RuntimeSettings
        {
            public RuntimeType Type { get; set; }
            public string? Tfm { get; set; }
            public string? DisplayName { get; set; } // Only settable for some custom runtimes

            // Clr
            public string? ClrVersion { get; set; } // Only for a custom .NET Framework version

            // Mono
            public string? MonoPath { get; set; }
            public string? AotArgs { get; set; }
            public string? MonoBclPath { get; set; }

            // MonoAotLLVM
            public string? AOTCompilerPath { get; set; }
            public MonoAotCompilerMode? AOTCompilerMode { get; set; }

            // Wasm and WasmAot
            public string? WasmJavascriptEnginePath { get; set; }
            public string? WasmJavascriptEngineArguments { get; set; }
            public string? WasmDataDirectory { get; set; }

            public Job ModifyJob(Job job)
            {
                Runtime runtime = Type switch
                {
                    RuntimeType.Clr => GetClrRuntime(),
                    RuntimeType.Core => GetCoreRuntime(),
                    RuntimeType.Mono => new MonoRuntime(DisplayName ?? "Mono", MonoPath!, AotArgs!, MonoBclPath!),
                    RuntimeType.MonoAotLLVM => GetMonoAotLLVMRuntime(),
                    RuntimeType.NativeAot => GetNativeAotRuntime(),
                    RuntimeType.Wasm => GetWasmRuntime(isAot: false),
                    RuntimeType.WasmAot => GetWasmRuntime(isAot: true),
                    _ => throw new Exception("Runtime type must be specified")
                };

                return job.WithRuntime(runtime);
            }

            private ClrRuntime GetClrRuntime()
            {
                if (ClrVersion is not null)
                    return ClrRuntime.CreateForLocalFullNetFrameworkBuild(ClrVersion);

                return Tfm switch
                {
                    "4.6.1" => ClrRuntime.Net461,
                    "4.6.2" => ClrRuntime.Net462,
                    "4.7" => ClrRuntime.Net47,
                    "4.7.1" => ClrRuntime.Net471,
                    "4.7.2" => ClrRuntime.Net472,
                    "4.8" => ClrRuntime.Net48,
                    "4.8.1" => ClrRuntime.Net481,
                    null => throw new Exception("TFM cannot be null for CLR runtime"),
                    _ => throw new Exception($"Unknown TFM '{Tfm}' for CLR runtime"),
                };
            }

            private CoreRuntime GetCoreRuntime()
            {
                return Tfm switch
                {
                    // Commented out as they are not supported by BenchmarkDotNet
                    //"netcoreapp2.0" => CoreRuntime.Core20,
                    //"netcoreapp2.1" => CoreRuntime.Core21,
                    //"netcoreapp2.2" => CoreRuntime.Core22,
                    //"netcoreapp3.0" => CoreRuntime.Core30,
                    "netcoreapp3.1" => CoreRuntime.Core31,
                    "net5.0" => CoreRuntime.Core50,
                    "net6.0" => CoreRuntime.Core60,
                    "net7.0" => CoreRuntime.Core70,
                    "net8.0" => CoreRuntime.Core80,
                    "net9.0" => CoreRuntime.Core90,
                    "net10.0" => CoreRuntime.Core10_0,
                    null => throw new Exception("TFM cannot be null for Core runtime"),
                    _ => CoreRuntime.CreateForNewVersion(Tfm, DisplayName ?? Tfm),
                };
            }

            private MonoAotLLVMRuntime GetMonoAotLLVMRuntime()
            {
                if (AOTCompilerPath is null)
                    throw new Exception("AOTCompilerPath must be set for MonoAotLLVM runtime");
                FileInfo aotCompilerPath = new FileInfo(AOTCompilerPath);
                if (AOTCompilerMode is null)
                    throw new Exception("AOTCompilerMode must be set for MonoAotLLVM runtime");
                if (Tfm is null)
                    throw new Exception("TFM must be set for MonoAotLLVM runtime");

                var moniker = Tfm switch
                {
                    "net6.0" => RuntimeMoniker.MonoAOTLLVMNet60,
                    "net7.0" => RuntimeMoniker.MonoAOTLLVMNet70,
                    "net8.0" => RuntimeMoniker.MonoAOTLLVMNet80,
                    "net9.0" => RuntimeMoniker.MonoAOTLLVMNet90,
                    "net10.0" => RuntimeMoniker.MonoAOTLLVMNet10_0,
                    _ => RuntimeMoniker.MonoAOTLLVM,
                };

                return new MonoAotLLVMRuntime(aotCompilerPath, AOTCompilerMode.Value, Tfm!, DisplayName ?? "MonoAotLLVM", moniker);
            }

            private NativeAotRuntime GetNativeAotRuntime()
            {
                return Tfm switch
                {
                    "net6.0" => NativeAotRuntime.Net60,
                    "net7.0" => NativeAotRuntime.Net70,
                    "net8.0" => NativeAotRuntime.Net80,
                    "net9.0" => NativeAotRuntime.Net90,
                    "net10.0" => NativeAotRuntime.Net10_0,
                    _ => throw new Exception($"Unsupported TFM '{Tfm}' for NativeAot runtime"),
                };
            }

            private WasmRuntime GetWasmRuntime(bool isAot)
            {
                if (Tfm is null)
                    throw new Exception("TFM must be set for Wasm runtime");

                var moniker = Tfm switch
                {
                    "net5.0" => RuntimeMoniker.WasmNet50,
                    "net6.0" => RuntimeMoniker.WasmNet60,
                    "net7.0" => RuntimeMoniker.WasmNet70,
                    "net8.0" => RuntimeMoniker.WasmNet80,
                    "net9.0" => RuntimeMoniker.WasmNet90,
                    "net10.0" => RuntimeMoniker.WasmNet10_0,
                    _ => RuntimeMoniker.Wasm,
                };

                return new WasmRuntime(
                    Tfm, 
                    DisplayName ?? "Wasm", 
                    WasmJavascriptEnginePath ?? "v8", 
                    WasmJavascriptEngineArguments ?? "--expose_wasm",
                    isAot,
                    WasmDataDirectory,
                    moniker
                );
            }
        }

        public enum RuntimeType
        {
            Clr,
            Core,
            Mono, // For MonoVM (e.g. built from .NET runtime, use Core)
            MonoAotLLVM,
            NativeAot,
            Wasm,
            WasmAot
        }

        public class GcSettings
        {
            public bool? Server { get; set; }
            public bool? Concurrent { get; set; }
            public bool? CpuGroups { get; set; }
            public bool? Force { get; set; }
            public bool? AllowVeryLargeObjects { get; set; }
            public bool? RetainVm { get; set; }
            public bool? NoAffinitize { get; set; }
            public int? HeapAffinitizeMask { get; set; }
            public int? HeapCount { get; set; }

            public Job ModifyJob(Job job)
            {
                if (Server is bool server)
                    job = job.WithGcServer(server);
                if (Concurrent is bool concurrent)
                    job = job.WithGcConcurrent(concurrent);
                if (CpuGroups is bool cpuGroups)
                    job = job.WithGcCpuGroups(cpuGroups);
                if (Force is bool force)
                    job = job.WithGcForce(force);
                if (AllowVeryLargeObjects is bool allowVeryLargeObjects)
                    job = job.WithGcAllowVeryLargeObjects(allowVeryLargeObjects);
                if (RetainVm is bool retainVm)
                    job = job.WithGcRetainVm(retainVm);
                if (NoAffinitize is bool noAffinitize)
                    job = job.WithNoAffinitize(noAffinitize);
                if (HeapAffinitizeMask is int heapAffinitizeMask)
                    job = job.WithHeapAffinitizeMask(heapAffinitizeMask);
                if (HeapCount is int heapCount)
                    job = job.WithHeapCount(heapCount);
                return job;
            }
        }

        public class RunSettings
        {
            public RunStrategy? RunStrategy { get; set; }
            public int? LaunchCount { get; set; }
            public int? WarmupCount { get; set; }
            public int? IterationCount { get; set; }
            public double? IterationTimeMilliseconds { get; set; }
            public long? InvocationCount { get; set; }
            public long? OperationCount { get; set; } // InvocationCount == OperationCount / OperationsPerInvoke
            public int? UnrollFactor { get; set; }
            public int? MinIterationCount { get; set; }
            public int? MaxIterationCount { get; set; }
            public int? MinWarmupIterationCount { get; set; }
            public int? MaxWarmupIterationCount { get; set; }
            public bool? MemoryRandomization { get; set; }

            public Job ModifyJob(Job job, BenchmarkCase? benchmark = null)
            {
                if (RunStrategy is RunStrategy runStrategy)
                    job = job.WithStrategy(runStrategy);
                if (LaunchCount is int launchCount)
                    job = job.WithLaunchCount(launchCount);
                if (WarmupCount is int warmupCount)
                    job = job.WithWarmupCount(warmupCount);
                if (IterationCount is int iterationCount)
                    job = job.WithIterationCount(iterationCount);
                if (IterationTimeMilliseconds is double iterationTime)
                    job = job.WithIterationTime(TimeInterval.FromMilliseconds(iterationTime));
                if (InvocationCount is long && benchmark is null)
                    throw new Exception("InvocationCount can only be set per benchmark");
                if (OperationCount is long && benchmark is null)
                    throw new Exception("OperationCount can only be set per benchmark");
                if (UnrollFactor is int unrollFactor)
                    job = job.WithUnrollFactor(unrollFactor);
                if (MinIterationCount is int minIterationCount)
                    job = job.WithMinIterationCount(minIterationCount);
                if (MaxIterationCount is int maxIterationCount)
                    job = job.WithMaxIterationCount(maxIterationCount);
                if (MinWarmupIterationCount is int minWarmupIterationCount)
                    job = job.WithMinWarmupCount(minWarmupIterationCount);
                if (MaxWarmupIterationCount is int maxWarmupIterationCount)
                    job = job.WithMaxWarmupCount(maxWarmupIterationCount);
                if (MemoryRandomization is bool memoryRandomization)
                    job = job.WithMemoryRandomization(memoryRandomization);

                if (benchmark is not null)
                {
                    var benchmarkName = FullNameProvider.GetBenchmarkName(benchmark);
                    long? invocationCount = InvocationCount;
                    if (invocationCount is null && OperationCount is long operationCount)
                    {
                        var operationsPerInvoke = benchmark.Descriptor.OperationsPerInvoke;
                        if (operationsPerInvoke % operationsPerInvoke != 0)
                            throw new Exception($"OperationCount ({operationCount}) must be divisible by OperationsPerInvoke ({operationsPerInvoke}) for benchmark '{benchmarkName}'");

                        invocationCount = operationCount / operationsPerInvoke;
                    }

                    if (invocationCount is not null)
                    {
                        if (benchmark.Descriptor.IterationSetupMethod is not null || benchmark.Descriptor.IterationCleanupMethod is not null)
                            throw new Exception($"OperationCount or InvocationCount cannot be set for benchmark '{benchmarkName}' as it has iteration setup or cleanup methods.");

                        if (UnrollFactor is not null)
                            unrollFactor = UnrollFactor.Value;
                        else if (job.HasValue(RunMode.UnrollFactorCharacteristic))
                            unrollFactor = job.Run.UnrollFactor;
                        else if (invocationCount < 64) // This is a deviation from base BDN which uses unroll factor of 16 if invocation count > 16
                            unrollFactor = 1;
                        else
                            unrollFactor = 16;

                        if (invocationCount % unrollFactor != 0)
                            throw new Exception($"InvocationCount ({invocationCount}) must be divisible by UnrollFactor ({unrollFactor}) for benchmark '{benchmarkName}'");

                        job = job.WithInvocationCount(invocationCount.Value).WithUnrollFactor(unrollFactor);
                    }
                }

                return job;
            }
        }

        public class InfrastructureSettings
        {
            public ToolchainSettings? Toolchain { get; set; }
            public string? BuildConfiguration { get; set; }
            public List<string>? MonoArguments { get; set; }
            public List<string>? MsBuildArguments { get; set; }

            public Job ModifyJob(Job job)
            {
                if (Toolchain is not null)
                    job = Toolchain.ModifyJob(job);
                if (BuildConfiguration is string buildConfiguration)
                    job = job.WithCustomBuildConfiguration(buildConfiguration);
                var arguments = new List<Argument>();
                if (MonoArguments is not null && MonoArguments.Count > 0)
                    arguments.AddRange(MonoArguments.Select(arg => new MonoArgument(arg)));
                if (MsBuildArguments is not null && MsBuildArguments.Count > 0)
                    arguments.AddRange(MsBuildArguments.Select(arg => new MsBuildArgument(arg)));
                if (arguments.Count > 0)
                    job = job.WithArguments(arguments.ToArray());
                return job;
            }
        }

        public class ToolchainSettings
        {
            public ToolchainType? Type { get; set; }
            public string? DisplayName { get; set; }
            public string? Tfm { get; set; } // TODO: Can we reuse the Tfm from RuntimeSettings?
            public string? CliPath { get; set; }
            public string? RestorePath { get; set; }

            // CoreRun
            public string? CoreRunPath { get; set; }

            // CsProjCore, Mono, NativeAot
            public string? RuntimeFrameworkVersion { get; set; }

            // InProcessEmit and InProcessNoEmit
            public bool? InProcessLogOutput { get; set; }
            public double? InProcessTimeoutSeconds { get; set; }

            // MonoAotLLVM, Wasm
            public string? CustomRuntimePack { get; set; }
            public string? AOTCompilerPath { get; set; } // TODO: Can we reuse the AOTCompilerPath from RuntimeSettings?
            public MonoAotCompilerMode? AOTCompilerMode { get; set; } // TODO: Can we reuse the AOTCompilerMode from RuntimeSettings?

            // NativeAot
            public string? IlcPackagesDirectory { get; set; }
            public string? ILCompilerVersion { get; set; }
            public string? NativeAotNugetFeed { get; set; }
            public bool? RootAllApplicationAssemblies { get; set; }
            public bool? IlcGenerateCompleteTypeMetadata { get; set; }
            public bool? IlcGenerateStackTraceData { get; set; }
            public string? IlcOptimizationPreference { get; set; } // "Size" or "Speed"
            public string? IlcInstructionSet { get; set; }

            public Job ModifyJob(Job job)
            {
                var tfm = Tfm ?? job.Environment.Runtime?.MsBuildMoniker ?? CoreRuntime.Latest.MsBuildMoniker;

                var netCoreAppSettings = new NetCoreAppSettings(
                    targetFrameworkMoniker: tfm,
                    runtimeFrameworkVersion: RuntimeFrameworkVersion,
                    name: DisplayName ?? job.Environment.Runtime?.Name ?? tfm,
                    customDotNetCliPath: CliPath,
                    packagesPath: RestorePath,
                    customRuntimePack: CustomRuntimePack,
                    aotCompilerPath: AOTCompilerPath,
                    aotCompilerMode: AOTCompilerMode ?? MonoAotCompilerMode.mini);

                IToolchain toolchain = Type switch
                {
                    ToolchainType.CoreRun => new CoreRunToolchain(
                        new FileInfo(CoreRunPath ?? throw new Exception("CoreRunPath must be set for CoreRun toolchain")),
                        createCopy: true,
                        targetFrameworkMoniker: tfm,
                        customDotNetCliPath: CliPath is not null ? new FileInfo(CliPath) : null,
                        restorePath: RestorePath is not null ? new DirectoryInfo(RestorePath) : null,
                        displayName: DisplayName ?? "CoreRun"),
                    ToolchainType.CsProjClassicNet => CsProjClassicNetToolchain.From(tfm, RestorePath, CliPath),
                    ToolchainType.CsProjCore => CsProjCoreToolchain.From(netCoreAppSettings),
                    ToolchainType.InProcessEmit => new InProcessEmitToolchain(
                        InProcessTimeoutSeconds is null ? TimeSpan.Zero : TimeSpan.FromSeconds(InProcessTimeoutSeconds.Value), 
                        InProcessLogOutput ?? true),
                    ToolchainType.InProcessNoEmit => new InProcessNoEmitToolchain(
                        InProcessTimeoutSeconds is null ? TimeSpan.Zero : TimeSpan.FromSeconds(InProcessTimeoutSeconds.Value), 
                        InProcessLogOutput ?? true),
                    ToolchainType.Mono => MonoToolchain.From(netCoreAppSettings),
                    ToolchainType.MonoAot => MonoAotToolchain.Instance,
                    ToolchainType.MonoAotLLVM => MonoAotLLVMToolChain.From(netCoreAppSettings),
                    ToolchainType.NativeAot => GetNativeAotToolchain(tfm),
                    ToolchainType.Roslyn => RoslynToolchain.Instance,
                    ToolchainType.Wasm => WasmToolchain.From(netCoreAppSettings),
                    _ => throw new ArgumentException("Toolchain type must be specified")
                };

                return job.WithToolchain(toolchain);
            }

            private IToolchain GetNativeAotToolchain(string tfm)
            {
                var builder = NativeAotToolchain.CreateBuilder();

                builder.TargetFrameworkMoniker(tfm);

                if (CliPath is not null)
                    builder.DotNetCli(CliPath);
                
                if (RestorePath is not null)
                    builder.PackagesRestorePath(RestorePath);

                if (IlcPackagesDirectory is not null)
                    builder.UseLocalBuild(new DirectoryInfo(IlcPackagesDirectory));
                else if (ILCompilerVersion is not null)
                    builder.UseNuGet(ILCompilerVersion ?? "", NativeAotNugetFeed ?? "https://api.nuget.org/v3/index.json");

                if (RootAllApplicationAssemblies is bool rootAllApplicationAssemblies)
                    builder.RootAllApplicationAssemblies(rootAllApplicationAssemblies);

                if (IlcGenerateCompleteTypeMetadata is bool generateCompleteTypeMetadata)
                    builder.IlcGenerateCompleteTypeMetadata(generateCompleteTypeMetadata);

                if (IlcGenerateStackTraceData is bool generateStackTraceData)
                    builder.IlcGenerateStackTraceData(generateStackTraceData);

                if (IlcOptimizationPreference is string optimizationPreference)
                    builder.IlcOptimizationPreference(optimizationPreference);

                if (IlcInstructionSet is string instructionSet)
                    builder.IlcInstructionSet(instructionSet);

                return builder.ToToolchain();
            }
        }

        public enum ToolchainType
        {
            CoreRun,
            CsProjClassicNet,
            CsProjCore,
            InProcessEmit,
            InProcessNoEmit,
            Mono,
            MonoAot,
            MonoAotLLVM,
            NativeAot,
            Roslyn,
            Wasm,
        }

        public class AccuracySettings
        {
            public double? MaxRelativeError { get; set; }
            public double? MaxAbsoluteErrorNanoseconds { get; set; }
            public double? MinIterationTimeMilliseconds { get; set; }
            public int? MinInvokeCount { get; set; }
            public bool? EvaluateOverhead { get; set; }
            public OutlierMode? OutlierMode { get; set; }
            public bool? AnalyzeLaunchVariance { get; set; }

            public Job ModifyJob(Job job)
            {
                if (MaxRelativeError is double maxRelativeError)
                    job = job.WithMaxRelativeError(maxRelativeError);
                if (MaxAbsoluteErrorNanoseconds is double maxAbsoluteErrorNanoseconds)
                    job = job.WithMaxAbsoluteError(TimeInterval.FromNanoseconds(maxAbsoluteErrorNanoseconds));
                if (MinIterationTimeMilliseconds is double minIterationTime)
                    job = job.WithMinIterationTime(TimeInterval.FromMilliseconds(minIterationTime));
                if (MinInvokeCount is int minInvokeCount)
                    job = job.WithMinInvokeCount(minInvokeCount);
                if (EvaluateOverhead is bool evaluateOverhead)
                    job = job.WithEvaluateOverhead(evaluateOverhead);
                if (OutlierMode is OutlierMode outlierMode)
                    job = job.WithOutlierMode(outlierMode);
                if (AnalyzeLaunchVariance is bool analyzeLaunchVariance)
                    job = job.WithAnalyzeLaunchVariance(analyzeLaunchVariance);
                return job;
            }
        }
    }
}