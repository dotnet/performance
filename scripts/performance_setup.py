from dataclasses import dataclass
from glob import glob
import os
import shutil
from ci_setup import CiSetupArgs

from performance.common import RunCommand, set_environment_variable


@dataclass
class PerformanceSetupArgs:
    performance_directory: str # Path to local copy of performance repository
    working_directory: str # Path to directory where the payload and work item directories will be created
    queue: str # The helix queue to run on

    csproj: str # Path to benchmark project file
    runtime_directory: str | None = None # Path to local copy of runtime repository
    core_root_directory: str | None = None # Path to the core root directory so that pre-built versions of the runtime repo can be used
    baseline_core_root_directory: str | None = None
    architecture: str = "x64"
    framework: str | None = None
    compilation_mode: str = "Tiered"
    repository: str | None = os.environ.get("BUILD_REPOSITORY_NAME")
    branch: str | None = os.environ.get("BUILD_SOURCEBRANCH")
    commit_sha: str | None = os.environ.get("BUILD_SOURCEVERSION")
    build_number: str | None = os.environ.get("BUILD_BUILDNUMBER")
    build_definition_name: str | None = os.environ.get("BUILD_DEFINITIONNAME")
    run_categories: str = "Libraries Runtime"
    kind: str = "micro"
    alpine: bool = False
    llvm: bool = False
    mono_interpreter: bool = False
    mono_aot: bool = False
    mono_aot_path: str | None = None
    internal: bool = False
    compare: bool = False
    mono_dotnet: str | None = None
    wasm_bundle_directory: str | None = None
    wasm_aot: bool = False
    javascript_engine: str = "v8"
    configurations: dict[str, str] | None = None
    android_mono: bool = False
    ios_mono: bool = False
    ios_nativeaot: bool = False
    no_dynamic_pgo: bool = False
    physical_promotion: bool = False
    ios_llvm_build: bool = False
    ios_strip_symbols: bool = False
    maui_version: str | None = None
    use_local_commit_time: bool = False
    only_sanity_check: bool = False
    extra_bdn_args: list[str] | None = None
    affinity: str | None = None
    python: str = "python3"

@dataclass
class PerformanceSetupData:
    payload_directory: str
    performance_directory: str
    work_item_directory: str
    python: str
    bdn_arguments: list[str]
    extra_bdn_arguments: list[str]
    setup_arguments: CiSetupArgs
    perf_lab_arguments: list[str]
    bdn_categories: str
    target_csproj: str
    kind: str
    architecture: str
    use_core_run: bool
    use_baseline_core_run: bool
    run_from_perf_repo: bool
    compare: bool
    mono_dotnet: bool
    wasm_dotnet: bool
    ios_llvm_build: bool
    ios_strip_symbols: bool
    creator: str
    queue: str
    helix_source_prefix: str
    build_config: str
    runtime_type: str
    only_sanity_check: bool

    def set_environment_variables(self, save_to_pipeline: bool = True):
        def set_env_var(name: str, value: str | bool | list[str], sep = " "):
            if isinstance(value, str):
                value_str = value
            elif isinstance(value, bool):
                value_str = "true" if value else "false"
            else:
                value_str = sep.join(value)
            set_environment_variable(name, value_str, save_to_pipeline=save_to_pipeline)

        set_env_var("PayloadDirectory", self.payload_directory)
        set_env_var("PerformanceDirectory", self.performance_directory)
        set_env_var("WorkItemDirectory", self.work_item_directory)
        set_env_var("Python", self.python)
        set_env_var("BenchmarkDotNetArguments", self.bdn_arguments)
        set_env_var("ExtraBenchmarkDotNetArguments", self.extra_bdn_arguments)
        # set_env_var("SetupArguments", self.setup_arguments) # Skipping as this is not currently being used as an env var
        set_env_var("PerfLabArguments", self.perf_lab_arguments)
        set_env_var("BDNCategories", self.bdn_categories)
        set_env_var("TargetCsproj", self.target_csproj)
        set_env_var("Kind", self.kind)
        set_env_var("Architecture", self.architecture)
        set_env_var("UseCoreRun", self.use_core_run)
        set_env_var("UseBaselineCoreRun", self.use_baseline_core_run)
        set_env_var("RunFromPerfRepo", self.run_from_perf_repo)
        set_env_var("Compare", self.compare)
        set_env_var("MonoDotnet", self.mono_dotnet)
        set_env_var("WasmDotnet", self.wasm_dotnet)
        set_env_var("iOSLlvmBuild", self.ios_llvm_build)
        set_env_var("iOSStripSymbols", self.ios_strip_symbols)
        set_env_var("Creator", self.creator)
        set_env_var("Queue", self.queue)
        set_env_var("HelixSourcePrefix", self.helix_source_prefix)
        set_env_var("_BuildConfig", self.build_config)
        set_env_var("RuntimeType", self.runtime_type)
        set_env_var("OnlySanityCheck", self.only_sanity_check)


def run(args: PerformanceSetupArgs):    
    payload_directory = os.path.join(args.working_directory, "Payload")
    performance_directory = os.path.join(payload_directory, "performance")
    work_item_directory = os.path.join(args.working_directory, "workitem")

    bdn_arguments = ["--anyCategories", args.run_categories]

    if args.affinity is not None and not "0":
        bdn_arguments += ["--affinity", args.affinity]

    extra_bdn_arguments = [] if args.extra_bdn_args is None else args.extra_bdn_args[:]
    if args.internal:
        creator = ""
        perf_lab_arguments = ["--upload-to-perflab-container"]
        helix_source_prefix = "official"
    else:
        extra_bdn_arguments += [
            "--iterationCount", "1", 
            "--warmupCount", "0", 
            "--invocationCount", "1", 
            "--unrollFactor", "1", 
            "--strategy", "ColdStart", 
            "--stopOnFirstError", "true"
        ]
        creator = args.build_definition_name or ""
        perf_lab_arguments = []
        helix_source_prefix = "pr"

    build_config = f"{args.architecture}.{args.kind}.{args.framework}"

    category_exclusions: list[str] = []

    if args.configurations is None:
        args.configurations = { "CompilationMode": args.compilation_mode, "RunKind": args.kind }

    using_mono = False
    if args.mono_dotnet is not None:
        using_mono = True
        args.configurations["LLVM"] = str(args.llvm)
        args.configurations["MonoInterpreter"] = str(args.mono_interpreter)
        args.configurations["MonoAOT"] = str(args.mono_aot)

        # TODO: Validate if this exclusion filter is still needed
        extra_bdn_arguments += ["--exclusion-filter", "*Perf_Image*", "*Perf_NamedPipeStream*"]
        category_exclusions += ["NoMono"]

        if args.mono_interpreter:
            category_exclusions += ["NoInterpreter"]

        if args.mono_aot:
            category_exclusions += ["NoAOT"]

    using_wasm = False
    if args.wasm_bundle_directory is not None:
        using_wasm = True
        args.configurations["CompilationMode"] = "wasm"
        if args.wasm_aot:
            args.configurations["AOT"] = "true"
            build_config = f"wasmaot.{build_config}"
        else:
            build_config = f"wasm.{build_config}"

        if args.javascript_engine == "javascriptcore":
            args.configurations["JSEngine"] = "javascriptcore"

        category_exclusions += ["NoInterpreter", "NoWASM", "NoMono"]

    if args.no_dynamic_pgo:
        args.configurations["PGOType"] = "nodynamicpgo"

    if args.physical_promotion:
        args.configurations["PhysicalPromotionType"] = "physicalpromotion"

    runtime_type = ""

    if args.ios_mono:
        runtime_type = "Mono"
        args.configurations["iOSLlvmBuild"] = str(args.ios_llvm_build)
        args.configurations["iOSStripSymbols"] = str(args.ios_strip_symbols)
        args.configurations["RuntimeType"] = str(runtime_type)

    if args.ios_nativeaot:
        runtime_type = "NativeAOT"
        args.configurations["iOSStripSymbols"] = str(args.ios_strip_symbols)
        args.configurations["RuntimeType"] = str(runtime_type)

    if category_exclusions:
        extra_bdn_arguments += ["--category-exclusion-filter", *set(category_exclusions)]
    
    cleaned_branch_name = "main"
    if args.branch is not None and args.branch.startswith("refs/heads/release"):
        cleaned_branch_name = args.branch.replace("refs/heads/", "")

    setup_arguments = CiSetupArgs(
        channel=cleaned_branch_name,
        queue=args.queue,
        build_configs=[f"{k}={v}" for k, v in args.configurations.items()],
        architecture=args.architecture,
        get_perf_hash=True
    )

    if args.build_number is not None:
        setup_arguments.build_number = args.build_number

    if args.repository is not None:
        setup_arguments.repository = f"https://github.com/{args.repository}"

    if args.branch is not None:
        setup_arguments.branch = args.branch

    if args.commit_sha is not None:
        setup_arguments.commit_sha = args.commit_sha

    if not args.internal:
        setup_arguments.not_in_lab = True

    # TODO: Figure out if this should be the runtime or performance commit time, or if we need to capture both
    if args.use_local_commit_time and args.commit_sha is not None:
        get_commit_time_command = RunCommand(["git", "show", "-s", "--format=%ci", args.commit_sha])
        get_commit_time_command.run()
        setup_arguments.commit_time = f"\"{get_commit_time_command.stdout}\""

    ignored_paths = [
        payload_directory,
        ".git",
        "artifacts",
    ]
    shutil.copytree(args.performance_directory, performance_directory, ignore=shutil.ignore_patterns(*ignored_paths))
    
    if args.mono_dotnet is not None:
        mono_dotnet_path = os.path.join(payload_directory, "dotnet-mono")
        shutil.copytree(args.mono_dotnet, mono_dotnet_path)

    if args.wasm_bundle_directory is not None:
        wasm_bundle_directory_path = payload_directory
        shutil.copytree(args.wasm_bundle_directory, wasm_bundle_directory_path)

        # Ensure there is a space at the beginning, so BDN can correctly read them
        # as arguments to `--wasmArgs`
        wasm_args = " --experimental-wasm-eh --expose_wasm"

        if args.javascript_engine == "v8":
            wasm_args += " --module"

        extra_bdn_arguments += [
            "--wasmEngine", f"/home/helixbot/.jsvu/bin/{args.javascript_engine}",
            "--wasmArgs", f"\"{wasm_args}\""
            "--cli", "$HELIX_CORRELATION_PAYLOAD/dotnet/dotnet",
            "--wasmDataDir", "$HELIX_CORRELATION_PAYLOAD/wasm-data"
        ]

        if args.wasm_aot:
            extra_bdn_arguments += [
                "--aotcompilermode", "wasm",
                "--buildTimeout", "3600"
            ]

        setup_arguments.dotnet_path = f"{wasm_bundle_directory_path}/dotnet"

    if args.no_dynamic_pgo:
        setup_arguments.pgo_status = "nodynamicpgo"

    if args.physical_promotion:
        setup_arguments.physical_promotion = "physicalpromotion"

    if args.mono_aot:
        if args.mono_aot_path is None:
            raise Exception("Mono AOT Path must be provided for MonoAOT runs")
        monoaot_dotnet_path = os.path.join(payload_directory, "monoaot")
        shutil.copytree(args.mono_aot_path, monoaot_dotnet_path)
        extra_bdn_arguments += [
            "--runtimes", "monoaotllvm",
            "--aotcompilerpath", "$HELIX_CORRELATION_PAYLOAD/monoaot/sgen/mini/mono-sgen",
            "--customruntimepack", "$HELIX_CORRELATION_PAYLOAD/monoaot/pack --aotcompilermode llvm"
        ]

    extra_bdn_arguments += ["--logBuildOutput", "--generateBinLog"]

    use_core_run = False
    if args.core_root_directory is not None:
        use_core_run = True
        new_core_root = os.path.join(payload_directory, "Core_Root")
        shutil.copytree(args.core_root_directory, new_core_root, ignore=shutil.ignore_patterns("*.pdb"))

    use_baseline_core_run = False
    if args.baseline_core_root_directory is not None:
        use_baseline_core_run = True
        new_baseline_core_root = os.path.join(payload_directory, "Baseline_Core_Root")
        shutil.copytree(args.baseline_core_root_directory, new_baseline_core_root)
    
    if args.maui_version is not None:
        setup_arguments.maui_version = args.maui_version
    
    if args.android_mono:
        if args.runtime_directory is None:
            raise Exception("Runtime directory must be present for Android Mono benchmarks")
        os.makedirs(work_item_directory, exist_ok=True)
        shutil.copy(os.path.join(args.runtime_directory, "MonoBenchmarksDroid.apk"), payload_directory)
        shutil.copy(os.path.join(args.runtime_directory, "androidHelloWorld", "HelloAndroid.apk"), payload_directory)
        setup_arguments.architecture = "arm64"

    if args.ios_mono or args.ios_nativeaot:
        if args.runtime_directory is None:
            raise Exception("Runtime directory must be present for IOS Mono or IOS Native AOT benchmarks")
        
        dest_zip_folder = os.path.join(payload_directory, "iosHelloWorldZip")
        shutil.copy(os.path.join(args.runtime_directory, "iosHelloWorld"), os.path.join(payload_directory, "iosHelloWorld"))
        shutil.copy(os.path.join(args.runtime_directory, "iosHelloWorldZip"), dest_zip_folder)

        # rename all zips in the 2nd folder to iOSSampleApp.zip
        for file in glob(os.path.join(dest_zip_folder, "*.zip")):
            os.rename(file, os.path.join(dest_zip_folder, "iOSSampleApp.zip"))

    shutil.copytree(os.path.join(performance_directory, "docs"), work_item_directory)

    return PerformanceSetupData(
        payload_directory=payload_directory,
        performance_directory=performance_directory,
        work_item_directory=work_item_directory,
        python=args.python,
        bdn_arguments=bdn_arguments,
        extra_bdn_arguments=extra_bdn_arguments,
        setup_arguments=setup_arguments,
        perf_lab_arguments=perf_lab_arguments,
        bdn_categories=args.run_categories,
        target_csproj=args.csproj,
        kind=args.kind,
        architecture=args.architecture,
        use_core_run=use_core_run,
        use_baseline_core_run=use_baseline_core_run,
        run_from_perf_repo=False,
        compare=args.compare,
        mono_dotnet=using_mono,
        wasm_dotnet=using_wasm,
        ios_llvm_build=args.ios_llvm_build,
        ios_strip_symbols=args.ios_strip_symbols,
        creator=creator,
        queue=args.queue,
        helix_source_prefix=helix_source_prefix,
        build_config=build_config,
        runtime_type=runtime_type,
        only_sanity_check=args.only_sanity_check)
