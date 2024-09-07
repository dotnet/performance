import re
from dataclasses import dataclass, field
from datetime import timedelta
from glob import glob
import json
import os
import shutil
import sys
import tempfile
import urllib.request
import xml.etree.ElementTree as ET
from typing import Any, Dict, List, Optional

import ci_setup
from performance.common import RunCommand, set_environment_variable
from send_to_helix import PerfSendToHelixArgs, perf_send_to_helix

def output_counters_for_crank(reports: List[Any]):
    print("#StartJobStatistics")

    statistics: dict[str, list[Any]] = {
        "metadata": [],
        "measurements": []
    }

    for report in reports:
        for test in report["tests"]:
            for counter in test["counters"]:
                measurement_name = f"benchmarkdotnet/{test['name']}/{counter['name']}"
                for result in counter["results"]:
                    statistics["measurements"].append({
                        "name": measurement_name,
                        "value": result
                    })

                if counter["topCounter"] == True:
                    statistics["metadata"].append({
                        "source": "BenchmarkDotNet",
                        "name": measurement_name,
                        "aggregate": "avg",
                        "reduce": "avg",
                        "format": "n0",
                        "shortDescription": f"{test['name']} ({counter['metricName']})"
                    })

    statistics["metadata"] = sorted(statistics["metadata"], key=lambda m: m["name"])

    print(json.dumps(statistics))

    print("#EndJobStatistics")

@dataclass
class RunPerformanceJobArgs:
    run_kind: str
    architecture: str
    os_group: str
    
    logical_machine: Optional[str] = None
    queue: Optional[str] = None
    framework: Optional[str] = None
    performance_repo_dir: str = "."
    runtime_repo_dir: Optional[str] = None
    core_root_dir: Optional[str] = None
    baseline_core_root_dir: Optional[str] = None
    mono_dotnet_dir: Optional[str] = None
    libraries_download_dir: Optional[str] = None
    versions_props_path: Optional[str] = None
    browser_versions_props_path: Optional[str] = None
    built_app_dir: Optional[str] = None
    extra_bdn_args: Optional[str] = None
    run_categories: str = 'Libraries Runtime'
    perflab_upload_token: Optional[str] = None
    helix_access_token: Optional[str] = os.environ.get("HelixAccessToken")
    os_sub_group: Optional[str] = None
    project_file: Optional[str] = None
    partition_count: Optional[int] = None
    build_repository_name: str = os.environ.get("BUILD_REPOSITORY_NAME", "dotnet/performance")
    build_source_branch: str = os.environ.get("BUILD_SOURCEBRANCH", "main")
    build_number: str = os.environ.get("BUILD_BUILDNUMBER", "local")
    build_definition_name: Optional[str] = os.environ.get("BUILD_DEFINITIONNAME")
    internal: bool = False
    pgo_run_type: Optional[str] = None
    physical_promotion_run_type: Optional[str] = None
    r2r_run_type: Optional[str] = None
    experiment_name: Optional[str] = None
    codegen_type: str = "JIT"
    runtime_type: str = "coreclr"
    affinity: Optional[str] = "0"
    run_env_vars: Dict[str, str] = field(default_factory=dict) # type: ignore
    is_scenario: bool = False
    runtime_flavor: Optional[str] = None
    local_build: bool = False
    compare: bool = False
    only_sanity_check: bool = False
    ios_llvm_build: bool = False
    ios_strip_symbols: bool = False
    hybrid_globalization: bool = False
    javascript_engine: str = "NoJS"
    send_to_helix: bool = False
    channel: Optional[str] = None
    perf_repo_hash: Optional[str] = os.environ.get("BUILD_SOURCEVERSION")
    performance_repo_ci: bool = False
    use_local_commit_time: bool = False
    javascript_engine_path: Optional[str] = None
    maui_version: Optional[str] = None
    pdn_path: Optional[str] = None
    os_version: Optional[str] = None
    dotnet_version_link: Optional[str] = None
    target_csproj: Optional[str] = None

def get_pre_commands(args: RunPerformanceJobArgs, v8_version: str):
    helix_pre_commands: list[str] = []

    # Increase file handle limit for Alpine: https://github.com/dotnet/runtime/pull/94439
    if args.os_sub_group == "_musl":
        helix_pre_commands += ["ulimit -n 4096"]

    # Remember the previous PYTHONPATH that was set so it can be restored in the post commands
    if args.os_group == "windows":
        helix_pre_commands += ["set ORIGPYPATH=%PYTHONPATH%"]
    else:
        helix_pre_commands += ["export ORIGPYPATH=$PYTHONPATH"]

    # Create separate list of commands to handle the next part. 
    # On non-Windows, these commands are chained together with && so they will stop if any fail
    install_prerequisites: list[str] = []

    # Install libgdiplus on Alpine
    if args.os_sub_group == "_musl":    
        install_prerequisites += [
            "sudo apk add icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib cargo",
            "sudo apk add libgdiplus --repository http://dl-cdn.alpinelinux.org/alpine/edge/testing"
        ]

    if args.internal:
        # Run inside a python venv
        if args.os_group == "windows":
            install_prerequisites += [
                "py -3 -m venv %HELIX_WORKITEM_ROOT%\\.venv",
                "call %HELIX_WORKITEM_ROOT%\\.venv\\Scripts\\activate.bat",
                "echo on" # venv activate script turns echo off, so turn it back on
            ]
        else:
            if args.os_group != "osx" and args.os_sub_group != "_musl":
                install_prerequisites += [
                    'echo "** Waiting for dpkg to unlock (up to 2 minutes) **"',
                    'timeout 2m bash -c \'while sudo fuser /var/lib/dpkg/lock-frontend >/dev/null 2>&1; do if [ -z "$printed" ]; then echo "Waiting for dpkg lock to be released... Lock is held by: $(ps -o cmd= -p $(sudo fuser /var/lib/dpkg/lock-frontend))"; printed=1; fi; echo "Waiting 5 seconds to check again"; sleep 5; done;\'',
                    "sudo apt-get remove -y lttng-modules-dkms", # https://github.com/dotnet/runtime/pull/101142
                    "sudo apt-get -y install python3-pip"
                ]

            install_prerequisites += [
                "python3 -m venv $HELIX_WORKITEM_ROOT/.venv",
                ". $HELIX_WORKITEM_ROOT/.venv/bin/activate"
            ]

        # Clear the PYTHONPATH first so that modules installed elsewhere are not used
        if args.os_group == "windows":
            install_prerequisites += ["set PYTHONPATH="]
        else:
            install_prerequisites += ["export PYTHONPATH="]

        # Install python pacakges needed to upload results to azure storage
        install_prerequisites += [
            f"python -m pip install -U pip",
            f"python -m pip install azure.storage.blob==12.13.0",
            f"python -m pip install azure.storage.queue==12.4.0",
            f"python -m pip install azure.identity==1.16.1",
            f"python -m pip install urllib3==1.26.19",
            f"python -m pip install opentelemetry-api==1.23.0",
            f"python -m pip install opentelemetry-sdk==1.23.0",
        ]

        # Install prereqs for NodeJS https://github.com/dotnet/runtime/pull/40667 
        # TODO: is this still needed? It seems like it was added to support wasm which is already setting up everything
        if args.os_group != "windows" and args.os_group != "osx" and args.os_sub_group != "_musl":
            install_prerequisites += [
                "sudo apt-get update",
                "sudo apt -y install curl dirmngr apt-transport-https lsb-release ca-certificates"
            ]

    # Set up everything needed for WASM runs
    if args.runtime_type == "wasm":
        # nodejs installation steps from https://github.com/nodesource/distributions
        install_prerequisites += [
            "export RestoreAdditionalProjectSources=$HELIX_CORRELATION_PAYLOAD/built-nugets",
            "sudo apt-get -y remove nodejs",
            "sudo apt-get update",
            "sudo apt-get install -y ca-certificates curl gnupg",
            "sudo mkdir -p /etc/apt/keyrings",
            "sudo rm -f /etc/apt/keyrings/nodesource.gpg",
            "curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | sudo gpg --dearmor --batch -o /etc/apt/keyrings/nodesource.gpg",
            "export NODE_MAJOR=18",
            "echo \"deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main\" | sudo tee /etc/apt/sources.list.d/nodesource.list",
            "sudo apt-get update",
            "sudo apt autoremove -y",
            "sudo apt-get install nodejs -y",
            f"test -n \"{v8_version}\"",
            "npm install --prefix $HELIX_WORKITEM_ROOT jsvu -g",
            f"$HELIX_WORKITEM_ROOT/bin/jsvu --os=linux64 v8@{v8_version}",
            f"export V8_ENGINE_PATH=~/.jsvu/bin/v8-{v8_version}",
            "${V8_ENGINE_PATH} -e 'console.log(`V8 version: ${this.version()}`)'"
        ]

    # Ensure that the upload token is set so that the results can be uploaded to the storage account
    if args.internal:
        if args.os_group == "windows":
            install_prerequisites += [f"set \"PERFLAB_UPLOAD_TOKEN={args.perflab_upload_token}\""]
        else:
            install_prerequisites += [f"export PERFLAB_UPLOAD_TOKEN=\"{args.perflab_upload_token}\""]

    # Add the install_prerequisites to the pre_commands
    if args.os_group == "windows":
        # TODO: Should we also give Windows the same treatment as linux and ensure that each command succeeds?
        helix_pre_commands += install_prerequisites
    else:
        if install_prerequisites:
            combined_prequisites = " && ".join(install_prerequisites)
            helix_pre_commands += [
                'echo "** Installing prerequistes **"',
                f"{combined_prequisites} || export PERF_PREREQS_INSTALL_FAILED=1",
                'test "x$PERF_PREREQS_INSTALL_FAILED" = "x1" && echo "** Error: Failed to install prerequites **"'
            ]

    # Set MONO_ENV_OPTIONS with for Mono Interpreter runs
    if args.codegen_type == "Interpreter" and args.runtime_type == "mono":
        if args.os_group == "windows":
            helix_pre_commands += ['set MONO_ENV_OPTIONS="--interpreter"']
        else:
            helix_pre_commands += ['export MONO_ENV_OPTIONS="--interpreter"']

    # Enable MSBuild node communication logs
    if args.os_group == "windows":
        helix_pre_commands += ["set MSBUILDDEBUGCOMM=1", 'set "MSBUILDDEBUGPATH=%HELIX_WORKITEM_UPLOAD_ROOT%"']
    else:
        helix_pre_commands += ["export MSBUILDDEBUGCOMM=1", 'export "MSBUILDDEBUGPATH=$HELIX_WORKITEM_UPLOAD_ROOT"']

    # Copy the performance repo and root directory to the work item directory
    if args.os_group == "windows":
        helix_pre_commands += [ 
            "robocopy /np /nfl /ndl /e %HELIX_CORRELATION_PAYLOAD%\\performance %HELIX_WORKITEM_ROOT%\\performance",
            "robocopy /np /nfl /ndl /e %HELIX_CORRELATION_PAYLOAD%\\root %HELIX_WORKITEM_ROOT%" ]
    else:
        helix_pre_commands += [ 
            "mkdir -p $HELIX_WORKITEM_ROOT/performance && cp -R $HELIX_CORRELATION_PAYLOAD/performance/* $HELIX_WORKITEM_ROOT/performance",
            "cp -R $HELIX_CORRELATION_PAYLOAD/root/* $HELIX_WORKITEM_ROOT" ]

    # invoke the machine-setup
    if args.os_group == "windows":
        helix_pre_commands += ["call %HELIX_WORKITEM_ROOT%\\machine-setup.cmd"]
    else:
        helix_pre_commands += [
            "chmod +x $HELIX_WORKITEM_ROOT/machine-setup.sh",
            ". $HELIX_WORKITEM_ROOT/machine-setup.sh",
        ]

    # ensure that the PYTHONPATH is set to the scripts directory
    # TODO: Run scripts out of work item directory instead of payload directory
    if args.os_group == "windows":
        helix_pre_commands += ["set PYTHONPATH=%HELIX_CORRELATION_PAYLOAD%\\scripts%3B%HELIX_CORRELATION_PAYLOAD%"]
    else:
        helix_pre_commands += ["export PYTHONPATH=$HELIX_CORRELATION_PAYLOAD/scripts:$HELIX_CORRELATION_PAYLOAD"]

    if args.runtime_type == "iOSMono":
        if args.os_group == "windows":
            helix_pre_commands += ["%HELIX_CORRELATION_PAYLOAD%\\monoaot\\mono-aot-cross --llvm --version"]
        else:
            helix_pre_commands += ["$HELIX_CORRELATION_PAYLOAD/monoaot/mono-aot-cross --llvm --version"]
        
    return helix_pre_commands

def get_post_commands(args: RunPerformanceJobArgs):
    if args.os_group == "windows":
        helix_post_commands = ["set PYTHONPATH=%ORIGPYPATH%"]
    else:
        helix_post_commands = ["export PYTHONPATH=$ORIGPYPATH"]

    if args.runtime_type == "wasm" and args.os_group != "windows":
        helix_post_commands += [
            """test -d "$HELIX_WORKITEM_UPLOAD_ROOT" && (
                export _PERF_DIR=$HELIX_WORKITEM_ROOT/performance;
                mkdir -p $HELIX_WORKITEM_UPLOAD_ROOT/log;
                find $_PERF_DIR -name '*.binlog' | xargs -I{} cp {} $HELIX_WORKITEM_UPLOAD_ROOT/log;
                test "$_commandExitCode" -eq 0 || (
                    mkdir -p $HELIX_WORKITEM_UPLOAD_ROOT/log/MicroBenchmarks/obj;
                    mkdir -p $HELIX_WORKITEM_UPLOAD_ROOT/log/MicroBenchmarks/bin;
                    mkdir -p $HELIX_WORKITEM_UPLOAD_ROOT/log/BenchmarkDotNet.Autogenerated/obj;
                    mkdir -p $HELIX_WORKITEM_UPLOAD_ROOT/log/for-running;
                    cp -R $_PERF_DIR/artifacts/obj/MicroBenchmarks $HELIX_WORKITEM_UPLOAD_ROOT/log/MicroBenchmarks/obj;
                    cp -R $_PERF_DIR/artifacts/bin/MicroBenchmarks $HELIX_WORKITEM_UPLOAD_ROOT/log/MicroBenchmarks/bin;
                    cp -R $_PERF_DIR/artifacts/obj/BenchmarkDotNet.Autogenerated $HELIX_WORKITEM_UPLOAD_ROOT/log/BenchmarkDotNet.Autogenerated/obj;
                    cp -R $_PERF_DIR/artifacts/bin/for-running $HELIX_WORKITEM_UPLOAD_ROOT/log/for-running))"""]

    return helix_post_commands

def logical_machine_to_queue(logical_machine: str, internal: bool, os_group: str, architecture: str, alpine: bool):
    if os_group == "windows":
        if not internal:
            return"Windows.10.Amd64.ClientRS4.DevEx.15.8.Open"
        else:
            queue_map = {
                "perftiger": "Windows.11.Amd64.Tiger.Perf",
                "perftiger_crossgen": "Windows.11.Amd64.Tiger.Perf",
                "perfowl": "Windows.11.Amd64.Owl.Perf",
                "perfsurf": "Windows.11.Arm64.Surf.Perf",
                "perfpixel4a": "Windows.11.Amd64.Pixel.Perf",
                "perfampere": "Windows.Server.Arm64.Perf",
                "perfviper": "Windows.11.Amd64.Viper.Perf",
                "cloudvm": "Windows.10.Amd64"
            }
            return queue_map.get(logical_machine, "Windows.11.Amd64.Tiger.Perf")
    else:
        if alpine:
            # this is the same for both public and internal
            return "alpine.amd64.tiger.perf"
        elif not internal:
            if architecture == "arm64":
                return "ubuntu.1804.armarch.open"
            else:
                return "Ubuntu.2204.Amd64.Open"
        else:
            if architecture == "arm64":
                if logical_machine == "perfampere":
                    return "Ubuntu.2204.Arm64.Perf"
                else:
                    return "Ubuntu.1804.Arm64.Perf"
            else:
                queue_map = {
                    "perfiphone12mini": "OSX.13.Amd64.Iphone.Perf",
                    "perfowl": "Ubuntu.2204.Amd64.Owl.Perf",
                    "perftiger_crossgen": "Ubuntu.1804.Amd64.Tiger.Perf",
                    "perfviper": "Ubuntu.2204.Amd64.Viper.Perf",
                    "cloudvm": "Ubuntu.2204.Amd64"
                }
                return queue_map.get(logical_machine, "Ubuntu.2204.Amd64.Tiger.Perf")

def run_performance_job(args: RunPerformanceJobArgs):
    helix_type_suffix = ""
    if args.runtime_type == "wasm":
        if args.codegen_type == "AOT":
            helix_type_suffix = "/wasm/aot"
        else:
            helix_type_suffix = "/wasm"

    alpine = args.runtime_type == "coreclr" and args.os_sub_group == "_musl"
    if args.queue is None:
        if args.logical_machine is None:
            raise Exception("Either queue or logical machine must be specifed")
        args.queue = logical_machine_to_queue(args.logical_machine, args.internal, args.os_group, args.architecture, alpine)

    if args.performance_repo_ci:
        # needs to be unique to avoid logs overwriting in mc.dot.net
        build_config = f"{args.architecture}_{args.channel}_{args.run_kind}"
        if args.dotnet_version_link is not None:
            build_config = f"{args.architecture}_{args.channel}_Linked_{args.run_kind}"
        helix_type = f"test/performance_{build_config}/"
    else:
        if args.framework is None:
            raise Exception("Framework not configured")
        
        build_config = f"{args.architecture}.{args.run_kind}.{args.framework}"
        helix_type = f"test/performance/{args.run_kind}/{args.framework}/{args.architecture}/{helix_type_suffix}"

    if not args.send_to_helix:
        # _BuildConfig is used by CI during log publishing
        set_environment_variable("_BuildConfig", build_config, save_to_pipeline=True) 
    
    if args.project_file is None:
        args.project_file = os.path.join(args.performance_repo_dir, "eng", "performance", "helix.proj")
    
    if args.perflab_upload_token is None:
        env_var_name = "PerfCommandUploadToken" if args.os_group == "windows" else "PerfCommandUploadTokenLinux"
        args.perflab_upload_token = os.environ.get(env_var_name)
        if args.perflab_upload_token is None:
            print(f"WARNING: {env_var_name} is not set. Results will not be uploaded.")
    
    args.performance_repo_dir = os.path.abspath(args.performance_repo_dir)

    mono_interpreter = args.codegen_type == "Interpreter" and args.runtime_type == "mono"

    if args.target_csproj is None:
        if args.os_group == "windows":
            args.target_csproj="src\\benchmarks\\micro\\MicroBenchmarks.csproj"
        else:
            args.target_csproj="src/benchmarks/micro/MicroBenchmarks.csproj"    
    elif args.os_group != "windows":
        args.target_csproj = args.target_csproj.replace("\\", "/")

    if args.libraries_download_dir is None and not args.performance_repo_ci and args.runtime_repo_dir is not None:
        args.libraries_download_dir = os.path.join(args.runtime_repo_dir, "artifacts")

    llvm = args.codegen_type == "AOT" and args.runtime_type != "wasm"
    android_mono = args.runtime_type == "AndroidMono"
    ios_mono = args.runtime_type == "iOSMono"
    ios_nativeaot = args.runtime_type == "iOSNativeAOT"
    mono_aot = False
    mono_aot_path = None
    mono_dotnet = None
    wasm_bundle_dir = None
    wasm_aot = False
    if args.runtime_type == "mono":
        if args.codegen_type == "AOT":
            if args.libraries_download_dir is None:
                raise Exception("Libraries not downloaded for MonoAOT")
            
            mono_aot = True
            mono_aot_path = os.path.join(args.libraries_download_dir, "bin", "aot")
        else:
            mono_dotnet = args.mono_dotnet_dir
            if mono_dotnet is None:
                if args.runtime_repo_dir is None:
                    raise Exception("Mono directory must be passed in for mono runs")
                mono_dotnet = os.path.join(args.runtime_repo_dir, ".dotnet-mono")
    elif args.runtime_type == "wasm":
        if args.libraries_download_dir is None:
                raise Exception("Libraries not downloaded for WASM")
        
        wasm_bundle_dir = os.path.join(args.libraries_download_dir, "bin", "wasm")
        if args.codegen_type == "AOT":
            wasm_aot = True

    working_dir = os.path.join(args.performance_repo_dir, "CorrelationStaging") # folder in which the payload and workitem directories will be made
    work_item_dir = os.path.join(working_dir, "workitem", "") # Folder in which the work item commands will be run in
    payload_dir = os.path.join(working_dir, "payload", "") # Uploaded folder containing everything needed to run the performance test
    root_payload_dir = os.path.join(payload_dir, "root") # folder that will get copied into the root of the payload directory

    # clear payload directory
    if os.path.exists(working_dir):
        print("Clearing existing payload directory")
        shutil.rmtree(working_dir)

    # ensure directories exist
    os.makedirs(work_item_dir, exist_ok=True)
    os.makedirs(root_payload_dir, exist_ok=True)

    # Include a copy of the whole performance in the payload directory
    performance_payload_dir = os.path.join(payload_dir, "performance")
    print("Copying performance repository to payload directory")
    shutil.copytree(args.performance_repo_dir, performance_payload_dir, ignore=shutil.ignore_patterns("CorrelationStaging", ".git", "artifacts", ".dotnet", ".venv", ".vs"))
    print("Finished copying performance repository to payload directory")

    bdn_arguments = ["--anyCategories", args.run_categories]

    if args.affinity is not None and not "0":
        bdn_arguments += ["--affinity", args.affinity]

    extra_bdn_arguments = [] if args.extra_bdn_args is None else args.extra_bdn_args.split(" ")
    if args.internal:
        creator = ""
        perf_lab_arguments = ["--upload-to-perflab-container"]
        helix_source_prefix = "official"
        if args.helix_access_token is None:
            raise Exception("HelixAccessToken environment variable is not configured")
    else:
        args.helix_access_token = None
        os.environ.pop("HelixAccessToken") # in case the environment variable is set on the system already
        args.perflab_upload_token = ""
        extra_bdn_arguments += [
            "--iterationCount", "1", 
            "--warmupCount", "0", 
            "--invocationCount", "1", 
            "--unrollFactor", "1", 
            "--strategy", "ColdStart", 
            "--stopOnFirstError", "true"
        ]
        creator = args.build_definition_name or ""
        if args.performance_repo_ci:
            creator = "dotnet-performance"
        perf_lab_arguments = []
        helix_source_prefix = "pr"

    category_exclusions: list[str] = []

    configurations = { "CompilationMode": "Tiered", "RunKind": args.run_kind }

    using_mono = False
    if mono_dotnet is not None:
        using_mono = True
        configurations["LLVM"] = str(llvm)
        configurations["MonoInterpreter"] = str(mono_interpreter)
        configurations["MonoAOT"] = str(mono_aot)

        # TODO: Validate if this exclusion filter is still needed
        extra_bdn_arguments += ["--exclusion-filter", "*Perf_Image*", "*Perf_NamedPipeStream*"]
        category_exclusions += ["NoMono"]

        if mono_interpreter:
            category_exclusions += ["NoInterpreter"]

        if mono_aot:
            category_exclusions += ["NoAOT"]

    using_wasm = False
    if wasm_bundle_dir is not None:
        using_wasm = True
        configurations["CompilationMode"] = "wasm"
        if wasm_aot:
            configurations["AOT"] = "true"
            build_config = f"wasmaot.{build_config}"
        else:
            build_config = f"wasm.{build_config}"

        if args.javascript_engine == "javascriptcore":
            configurations["JSEngine"] = "javascriptcore"

        category_exclusions += ["NoInterpreter", "NoWASM", "NoMono"]

    if args.pgo_run_type == "nodynamicpgo":
        configurations["PGOType"] = "nodynamicpgo"

    if args.physical_promotion_run_type == "physicalpromotion":
        configurations["PhysicalPromotionType"] = "physicalpromotion"

    if args.r2r_run_type == "nor2r":
        configurations["R2RType"] = "nor2r"

    if args.hybrid_globalization:
        configurations["HybridGlobalization"] = "True"

    if args.experiment_name is not None:
        configurations["ExperimentName"] = args.experiment_name
        if args.experiment_name == "memoryRandomization":
            extra_bdn_arguments += ["--memoryRandomization", "true"]

    runtime_type = ""

    if ios_mono:
        runtime_type = "Mono"
        configurations["iOSLlvmBuild"] = str(args.ios_llvm_build)
        configurations["iOSStripSymbols"] = str(args.ios_strip_symbols)
        configurations["RuntimeType"] = str(runtime_type)

    if ios_nativeaot:
        runtime_type = "NativeAOT"
        configurations["iOSStripSymbols"] = str(args.ios_strip_symbols)
        configurations["RuntimeType"] = str(runtime_type)

    if category_exclusions:
        extra_bdn_arguments += ["--category-exclusion-filter", *set(category_exclusions)]
    
    branch = os.environ.get("BUILD_SOURCEBRANCH")
    cleaned_branch_name = "main"
    if branch is not None and branch.startswith("refs/heads/release"):
        cleaned_branch_name = branch.replace("refs/heads/", "")

    ci_setup_arguments = ci_setup.CiSetupArgs(
        channel=cleaned_branch_name,
        queue=args.queue,
        build_configs=[f"{k}={v}" for k, v in configurations.items()],
        architecture=args.architecture,
        get_perf_hash=True)

    ci_setup_arguments.build_number = args.build_number

    if branch is not None and not (args.performance_repo_ci and branch == "refs/heads/main"):
        ci_setup_arguments.branch = branch

    if args.perf_repo_hash is not None and not args.performance_repo_ci:
        ci_setup_arguments.repository = f"https://github.com/{args.build_repository_name}"
        ci_setup_arguments.commit_sha = args.perf_repo_hash

        if args.use_local_commit_time:
            get_commit_time_command = RunCommand(["git", "show", "-s", "--format=%ci", args.perf_repo_hash], verbose=True)
            get_commit_time_command.run(args.runtime_repo_dir)
            ci_setup_arguments.commit_time = f"{get_commit_time_command.stdout.strip()}"

    # not_in_lab should stay False for internal dotnet performance CI runs
    if not args.internal and not args.performance_repo_ci:
        ci_setup_arguments.not_in_lab = True

    if mono_dotnet is not None:
        mono_dotnet_path = os.path.join(payload_dir, "dotnet-mono")
        shutil.copytree(mono_dotnet, mono_dotnet_path)

    v8_version = ""
    if wasm_bundle_dir is not None:
        wasm_bundle_dir_path = payload_dir
        shutil.copytree(wasm_bundle_dir, wasm_bundle_dir_path, dirs_exist_ok=True)

        wasm_args = "--expose_wasm"

        if args.javascript_engine == "v8":
            if args.browser_versions_props_path is None:
                if args.runtime_repo_dir is None:
                    raise Exception("BrowserVersions.props must be present for wasm runs")
                args.browser_versions_props_path = os.path.join(args.runtime_repo_dir, "eng", "testing", "BrowserVersions.props")
            
            wasm_args += " --module"

            with open(args.browser_versions_props_path) as f:
                for line in f:
                    match = re.search(r"linux_V8Version>([^<]*)<", line)
                    if match:
                        v8_version = match.group(1)
                        v8_version = ".".join(v8_version.split(".")[:3])
                        break
                else:
                    raise Exception("Unable to find v8 version in BrowserVersions.props")
            
            if args.javascript_engine_path is None:
                args.javascript_engine_path = f"/home/helixbot/.jsvu/bin/v8-{v8_version}"

        if args.javascript_engine_path is None:
            args.javascript_engine_path = f"/home/helixbot/.jsvu/bin/{args.javascript_engine}"    

        extra_bdn_arguments += [
            "--wasmEngine", args.javascript_engine_path,
            f"\\\"--wasmArgs={wasm_args}\\\"",
            "--cli", "$HELIX_CORRELATION_PAYLOAD/dotnet/dotnet",
            "--wasmDataDir", "$HELIX_CORRELATION_PAYLOAD/wasm-data"
        ]

        if wasm_aot:
            extra_bdn_arguments += [
                "--aotcompilermode", "wasm",
                "--buildTimeout", "3600"
            ]

        ci_setup_arguments.dotnet_path = f"{wasm_bundle_dir_path}/dotnet"

    if args.dotnet_version_link is not None:
        if args.dotnet_version_link.startswith("https"): # Version link is a proper url
            if args.dotnet_version_link.endswith(".json"):
                with urllib.request.urlopen(args.dotnet_version_link) as response:
                    values = json.loads(response.read().decode('utf-8'))
                    if "dotnet_version" in values:
                        ci_setup_arguments.dotnet_versions = [values["dotnet_version"]]
                    else:
                        ci_setup_arguments.dotnet_versions = [values["version"]]
            else:
                raise ValueError("Invalid dotnet_version_link provided. Must be a json file if a url.")
        elif os.path.exists(os.path.join(args.performance_repo_dir, args.dotnet_version_link)) and args.dotnet_version_link.endswith("Version.Details.xml"): # version_link is a file in the perf repo
            with open(os.path.join(args.performance_repo_dir, args.dotnet_version_link), encoding="utf-8") as f:
                root = ET.fromstring(f.read())
            dependency = root.find(".//Dependency[@Name='Microsoft.NET.Sdk']") # For net9.0
            if dependency is None: # For older than net9.0
                dependency = root.find(".//Dependency[@Name='Microsoft.Dotnet.Sdk.Internal']")
            if dependency is not None and "Version" in dependency.attrib: # Get the actual version
                ci_setup_arguments.dotnet_versions = [dependency.get("Version", "ERROR: Failed to get version")]
            else:
                raise ValueError("Unable to find dotnet version in the provided xml file")
        else:
            raise ValueError("Invalid dotnet_version_link provided")

    if args.pgo_run_type == "nodynamicpgo":
        ci_setup_arguments.pgo_status = "nodynamicpgo"

    if args.physical_promotion_run_type == "physicalpromotion":
        ci_setup_arguments.physical_promotion_status = "physicalpromotion"

    if args.r2r_run_type == "nor2r":
        ci_setup_arguments.r2r_status = "nor2r"

    ci_setup_arguments.experiment_name = args.experiment_name

    if mono_aot:
        if mono_aot_path is None:
            raise Exception("Mono AOT Path must be provided for MonoAOT runs")
        monoaot_dotnet_path = os.path.join(payload_dir, "monoaot")
        shutil.copytree(mono_aot_path, monoaot_dotnet_path)
        extra_bdn_arguments += [
            "--runtimes", "monoaotllvm",
            "--aotcompilerpath", "$HELIX_CORRELATION_PAYLOAD/monoaot/mono-aot-cross",
            "--customruntimepack", "$HELIX_CORRELATION_PAYLOAD/monoaot/pack", 
            "--aotcompilermode", "llvm",
        ]

    extra_bdn_arguments += ["--logBuildOutput", "--generateBinLog"]

    if args.only_sanity_check:
        extra_bdn_arguments += ["--filter", "System.Tests.Perf_*"]

    bdn_arguments += extra_bdn_arguments

    baseline_bdn_arguments = bdn_arguments[:]
    
    use_core_run = False
    use_baseline_core_run = False
    if not args.performance_repo_ci and args.runtime_type == "coreclr":
        use_core_run = True
        if args.core_root_dir is None:
            if args.runtime_repo_dir is None:
                raise Exception("Core_Root directory must be specified for non-performance CI runs")
            args.core_root_dir = os.path.join(args.runtime_repo_dir, "artifacts", "tests", "coreclr", f"{args.os_group}.{args.architecture}.Release", "Tests", "Core_Root")
        coreroot_payload_dir = os.path.join(payload_dir, "Core_Root")
        shutil.copytree(args.core_root_dir, coreroot_payload_dir, ignore=shutil.ignore_patterns("*.pdb"))

        if args.baseline_core_root_dir is not None:
            use_baseline_core_run = True
            baseline_coreroot_payload_dir = os.path.join(payload_dir, "Baseline_Core_Root")
            shutil.copytree(args.baseline_core_root_dir, baseline_coreroot_payload_dir)
    
    if args.maui_version is not None:
        ci_setup_arguments.maui_version = args.maui_version

    if args.built_app_dir is None:
        if args.runtime_repo_dir is not None:
            args.built_app_dir = args.runtime_repo_dir

    
    if android_mono:
        if args.built_app_dir is None:
            raise Exception("Built apps directory must be present for Android Mono benchmarks")
        shutil.copy(os.path.join(args.built_app_dir, "MonoBenchmarksDroid.apk"), os.path.join(root_payload_dir, "MonoBenchmarksDroid.apk"))
        shutil.copy(os.path.join(args.built_app_dir, "androidHelloWorld", "HelloAndroid.apk"), os.path.join(root_payload_dir, "HelloAndroid.apk"))
        ci_setup_arguments.architecture = "arm64"

    if ios_mono or ios_nativeaot:
        if args.built_app_dir is None:
            raise Exception("Built apps directory must be present for IOS Mono or IOS Native AOT benchmarks")
        
        shutil.copytree(os.path.join(args.built_app_dir, "iosHelloWorld"), os.path.join(payload_dir, "iosHelloWorld"))
        dest_zip_folder = shutil.copytree(os.path.join(args.built_app_dir, "iosHelloWorldZip"), os.path.join(payload_dir, "iosHelloWorldZip"))

        # rename all zips in the 2nd folder to iOSSampleApp.zip
        for file in glob(os.path.join(dest_zip_folder, "*.zip")):
            os.rename(file, os.path.join(dest_zip_folder, "iOSSampleApp.zip"))

    # ensure work item directory is not empty
    shutil.copytree(os.path.join(args.performance_repo_dir, "docs"), work_item_dir, dirs_exist_ok=True)

    if args.os_group == "windows":
        agent_python = "py -3"
    else:
        agent_python = "python3"

    helix_pre_commands = get_pre_commands(args, v8_version)
    helix_post_commands = get_post_commands(args)

    ci_setup_arguments.local_build = args.local_build

    if args.affinity != "0":
        ci_setup_arguments.affinity = args.affinity

    if args.run_env_vars:
        ci_setup_arguments.run_env_vars = [f"{k}={v}" for k, v in args.run_env_vars.items()]

    ci_setup_arguments.target_windows = args.os_group == "windows"

    ci_setup_arguments.output_file = os.path.join(root_payload_dir, "machine-setup")
    if args.is_scenario:
        ci_setup_arguments.install_dir = os.path.join(payload_dir, "dotnet")
    else:
        tools_dir = os.path.join(performance_payload_dir, "tools")
        ci_setup_arguments.install_dir = os.path.join(tools_dir, "dotnet", args.architecture)

    if args.channel is not None:
        ci_setup_arguments.channel = args.channel

    if args.perf_repo_hash is not None and args.performance_repo_ci:
        ci_setup_arguments.perf_hash = args.perf_repo_hash

    ci_setup.main(ci_setup_arguments)

    # ci_setup may modify global.json, so we should copy it across to the payload directory if that happens
    # TODO: Refactor this when we eventually remove the dependency on ci_setup.py directly from the runtime repository.
    shutil.copy(os.path.join(args.performance_repo_dir, 'global.json'), os.path.join(performance_payload_dir, 'global.json'))

    if args.is_scenario:
        set_environment_variable("DOTNET_ROOT", ci_setup_arguments.install_dir, save_to_pipeline=True)
        print(f"Set DOTNET_ROOT to {ci_setup_arguments.install_dir}")

        new_path = f"{ci_setup_arguments.install_dir}{os.pathsep}{os.environ['PATH']}"
        set_environment_variable("PATH", new_path, save_to_pipeline=True)
        print(f"Set PATH to {new_path}")

        framework = os.environ["PERFLAB_Framework"]
        os.environ["PERFLAB_TARGET_FRAMEWORKS"] = framework
        if args.os_group == "windows":
            runtime_id = f"win-{args.architecture}"
        elif args.os_group == "osx":
            runtime_id = f"osx-{args.architecture}"
        else:
            runtime_id = f"linux-{args.architecture}"

        dotnet_executable_path = os.path.join(ci_setup_arguments.install_dir, "dotnet")

        os.environ["MSBUILDDISABLENODEREUSE"] = "1" # without this, MSbuild will be kept alive

        # build Startup
        RunCommand([
            dotnet_executable_path, "publish", 
            "-c", "Release", 
            "-o", os.path.join(payload_dir, "startup"),
            "-f", framework,
            "-r", runtime_id,
            "--self-contained",
            os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "Startup", "Startup.csproj"),
            f"/bl:{os.path.join(args.performance_repo_dir, 'artifacts', 'log', build_config, 'Startup.binlog')}",
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"],
            verbose=True).run()

        # build SizeOnDisk
        RunCommand([
            dotnet_executable_path, "publish", 
            "-c", "Release", 
            "-o", os.path.join(payload_dir, "SOD"),
            "-f", framework,
            "-r", runtime_id,
            "--self-contained",
            os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "SizeOnDisk", "SizeOnDisk.csproj"),
            f"/bl:{os.path.join(args.performance_repo_dir, 'artifacts', 'log', build_config, 'SizeOnDisk.binlog')}",
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"],
            verbose=True).run()
        
        if args.performance_repo_ci:
            # build MemoryConsumption
            RunCommand([
                dotnet_executable_path, "publish", 
                "-c", "Release", 
                "-o", os.path.join(payload_dir, "MemoryConsumption"),
                "-f", framework,
                "-r", runtime_id,
                "--self-contained",
                os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "MemoryConsumption", "MemoryConsumption.csproj"),
                f"/bl:{os.path.join(args.performance_repo_dir, 'artifacts', 'log', build_config, 'MemoryConsumption.binlog')}",
                "-p:DisableTransitiveFrameworkReferenceDownloads=true"],
                verbose=True).run()
            
            # build PerfLabGenericEventSourceForwarder
            RunCommand([
                dotnet_executable_path, "publish", 
                "-c", "Release", 
                "-o", os.path.join(payload_dir, "PerfLabGenericEventSourceForwarder"),
                "-f", framework,
                "-r", runtime_id,
                os.path.join(args.performance_repo_dir, "src", "tools", "PerfLabGenericEventSourceForwarder", "PerfLabGenericEventSourceForwarder", "PerfLabGenericEventSourceForwarder.csproj"),
                f"/bl:{os.path.join(args.performance_repo_dir, 'artifacts', 'log', build_config, 'PerfLabGenericEventSourceForwarder.binlog')}",
                "-p:DisableTransitiveFrameworkReferenceDownloads=true"],
                verbose=True).run()
            
            # build PerfLabGenericEventSourceLTTngProvider
            if args.os_group != "windows" and args.os_group != "osx" and args.os_version == "2204":
                RunCommand([
                    os.path.join(args.performance_repo_dir, "src", "tools", "PerfLabGenericEventSourceLTTngProvider", "build.sh"),
                    "-o", os.path.join(payload_dir, "PerfLabGenericEventSourceForwarder")],
                    verbose=True).run()
            
            # copy PDN
            if args.os_group == "windows" and args.architecture != "x86" and args.pdn_path is not None:
                print("Copying PDN")
                pdn_dest = os.path.join(payload_dir, "PDN")
                pdn_file_path = os.path.join(pdn_dest, "PDN.zip")
                shutil.copyfile(args.pdn_path, pdn_file_path)
                print(f"PDN copied to {pdn_file_path}")

            # create a copy of the environment since we want these to only be set during the following invocation
            environ_copy = os.environ.copy()

            os.environ["CorrelationPayloadDirectory"] = payload_dir
            os.environ["Architecture"] = args.architecture
            os.environ["TargetsWindows"] = "true" if args.os_group == "windows" else "false"
            os.environ["HelixTargetQueues"] = args.queue
            os.environ["Python"] = agent_python
            os.environ["RuntimeFlavor"] = args.runtime_flavor or ''
            os.environ["HybridGlobalization"] = str(args.hybrid_globalization)

            # TODO: See if these commands are needed for linux as they were being called before but were failing.
            if args.os_group == "windows" or args.os_group == "osx":
                RunCommand([*(agent_python.split(" ")), "-m", "pip", "install", "--user", "--upgrade", "pip"]).run()
                RunCommand([*(agent_python.split(" ")), "-m", "pip", "install", "--user", "urllib3==1.26.19"]).run()
                RunCommand([*(agent_python.split(" ")), "-m", "pip", "install", "--user", "requests"]).run()

            scenarios_path = os.path.join(args.performance_repo_dir, "src", "scenarios")
            script_path = os.path.join(args.performance_repo_dir, "scripts")
            os.environ["PYTHONPATH"] = f"{os.environ.get('PYTHONPATH', '')}{os.pathsep}{script_path}{os.pathsep}{scenarios_path}"
            print(f"PYTHONPATH={os.environ['PYTHONPATH']}")

            os.environ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
            os.environ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
            os.environ["UseSharedCompilation"] = "false"

            print("Current dotnet directory:", ci_setup_arguments.install_dir)
            print("If more than one version exist in this directory, usually the latest runtime and sdk will be used.")

            # PreparePayloadWorkItems is only available for scenarios runs defined inside the performance repo
            if args.performance_repo_ci:
                RunCommand([
                    "dotnet", "msbuild", args.project_file, 
                    "/restore", 
                    "/t:PreparePayloadWorkItems",
                    f"/bl:{os.path.join(args.performance_repo_dir, 'artifacts', 'log', build_config, 'PrepareWorkItemPayloads.binlog')}"],
                    verbose=True).run()

            # restore env vars
            os.environ.update(environ_copy)

        shutil.copy(os.path.join(performance_payload_dir, "NuGet.config"), os.path.join(root_payload_dir, "NuGet.config"))
        shutil.copytree(os.path.join(performance_payload_dir, "scripts"), os.path.join(payload_dir, "scripts"))
        shutil.copytree(os.path.join(performance_payload_dir, "src", "scenarios", "shared"), os.path.join(payload_dir, "shared"))
        shutil.copytree(os.path.join(performance_payload_dir, "src", "scenarios", "staticdeps"), os.path.join(payload_dir, "staticdeps"))
        
        if args.architecture == "arm64":
            dotnet_dir = os.path.join(ci_setup_arguments.install_dir, "")
            arm64_dotnet_dir = os.path.join(args.performance_repo_dir, "tools", "dotnet", "arm64")
            shutil.rmtree(dotnet_dir)
            shutil.copytree(arm64_dotnet_dir, dotnet_dir)

        # Zip the workitem directory (for xharness (mobile) based workitems)
        if args.run_kind == "ios_scenarios" or args.run_kind == "android_scenarios":
            with tempfile.TemporaryDirectory() as temp_dir:
                archive_path = shutil.make_archive(os.path.join(temp_dir, 'workitem'), 'zip', work_item_dir)
                shutil.move(archive_path, f"{work_item_dir}.zip")

    if args.os_group == "windows":
        cli_arguments = [
            "--dotnet-versions", "%DOTNET_VERSION%", 
            "--cli-source-info", "args", 
            "--cli-branch", "%PERFLAB_BRANCH%", 
            "--cli-commit-sha", "%PERFLAB_HASH%",
            "--cli-repository", "https://github.com/%PERFLAB_REPO%",
            "--cli-source-timestamp", "%PERFLAB_BUILDTIMESTAMP%"
        ]
    else:
        cli_arguments = [
            "--dotnet-versions", "$DOTNET_VERSION", 
            "--cli-source-info", "args", 
            "--cli-branch", "$PERFLAB_BRANCH", 
            "--cli-commit-sha", "$PERFLAB_HASH",
            "--cli-repository", "https://github.com/$PERFLAB_REPO",
            "--cli-source-timestamp", "$PERFLAB_BUILDTIMESTAMP"
        ]

    if using_wasm:
        cli_arguments += ["--run-isolated", "--wasm", "--dotnet-path", "$HELIX_CORRELATION_PAYLOAD/dotnet/"]

    if using_mono:
        if args.versions_props_path is None:
            if args.runtime_repo_dir is None:
                raise Exception("Version.props must be present for mono runs")
            args.versions_props_path = os.path.join(args.runtime_repo_dir, "eng", "Versions.props")
            
        with open(args.versions_props_path) as f:
            for line in f:
                match = re.search(r"ProductVersion>([^<]*)<", line)
                if match:
                    product_version = match.group(1)
                    break
            else:
                raise Exception("Unable to find ProductVersion in Versions.props")

        if args.os_group == "windows":
            bdn_arguments += ["--corerun", f"%HELIX_CORRELATION_PAYLOAD%\\dotnet-mono\\shared\\Microsoft.NETCore.App\\{product_version}\\corerun.exe"]
        else:
            bdn_arguments += ["--corerun", f"$HELIX_CORRELATION_PAYLOAD/dotnet-mono/shared/Microsoft.NETCore.App/{product_version}/corerun"]
    
    if use_core_run:
        if args.os_group == "windows":
            bdn_arguments += ["--corerun", "%HELIX_CORRELATION_PAYLOAD%\\Core_Root\\CoreRun.exe"]
        else:
            bdn_arguments += ["--corerun", "$HELIX_CORRELATION_PAYLOAD/Core_Root/corerun"]

    if use_baseline_core_run:
        if args.os_group == "windows":
            baseline_bdn_arguments += ["--corerun", "%HELIX_CORRELATION_PAYLOAD%\\Baseline_Core_Root\\CoreRun.exe"]
        else:
            baseline_bdn_arguments += ["--corerun", "$HELIX_CORRELATION_PAYLOAD/Baseline_Core_Root/corerun"]

    if args.os_group == "windows":
        bdn_artifacts_directory = "%HELIX_WORKITEM_UPLOAD_ROOT%\\BenchmarkDotNet.Artifacts"
        bdn_baseline_artifacts_dir = "%HELIX_WORKITEM_UPLOAD_ROOT%\\BenchmarkDotNet.Artifacts_Baseline"
    else:
        bdn_artifacts_directory = "$HELIX_WORKITEM_UPLOAD_ROOT/BenchmarkDotNet.Artifacts"
        bdn_baseline_artifacts_dir = "$HELIX_WORKITEM_UPLOAD_ROOT/BenchmarkDotNet.Artifacts_Baseline"
    
    if args.os_group == "windows":
        work_item_command = [
            "python",
            "%HELIX_WORKITEM_ROOT%\\performance\\scripts\\benchmarks_ci.py", 
            "--csproj", f"%HELIX_WORKITEM_ROOT%\\performance\\{args.target_csproj}"]
    else:
        work_item_command = [
            "python",
            "$HELIX_WORKITEM_ROOT/performance/scripts/benchmarks_ci.py", 
            "--csproj", f"$HELIX_WORKITEM_ROOT/performance/{args.target_csproj}"]
        
    perf_lab_framework = os.environ['PERFLAB_Framework']
    work_item_command: List[str] = [
        *work_item_command, 
        "--incremental", "no",
        "--architecture", args.architecture,
        "-f", perf_lab_framework,
        *perf_lab_arguments]

    if perf_lab_framework != "net461":
        work_item_command = work_item_command + cli_arguments
    
    baseline_work_item_command = work_item_command[:]

    work_item_command += ["--bdn-artifacts", bdn_artifacts_directory]
    baseline_work_item_command += ["--bdn-artifacts", bdn_baseline_artifacts_dir]

    work_item_timeout = timedelta(hours=6)
    if args.only_sanity_check:
        work_item_timeout = timedelta(hours=1.5)

    helix_results_destination_dir=os.path.join(args.performance_repo_dir, "artifacts", "helix-results")

    compare_command = None
    fail_on_test_failure = True
    if args.compare:
        fail_on_test_failure = False        
        if args.os_group == "windows":
            dotnet_exe = f"%HELIX_WORKITEM_ROOT%\\performance\\tools\\dotnet\\{args.architecture}\\dotnet.exe"
            results_comparer = "%HELIX_WORKITEM_ROOT%\\performance\\src\\tools\\ResultsComparer\\ResultsComparer.csproj"
            threshold = "2%%"
            xml_results = "%HELIX_WORKITEM_ROOT%\\testResults.xml"
        else:
            dotnet_exe = f"$HELIX_WORKITEM_ROOT/performance/tools/dotnet/{args.architecture}/dotnet"
            results_comparer = "$HELIX_WORKITEM_ROOT/performance/src/tools/ResultsComparer/ResultsComparer.csproj"
            threshold = "2%"
            xml_results = "$HELIX_WORKITEM_ROOT/testResults.xml"

        compare_command = [
            dotnet_exe, "run",
            "-f", perf_lab_framework,
            "-p", results_comparer,
            "--base", bdn_baseline_artifacts_dir,
            "--diff", bdn_artifacts_directory,
            "--threshold", threshold,
            "--xml", xml_results]

    perf_send_to_helix_args = PerfSendToHelixArgs(
        helix_source=f"{helix_source_prefix}/{args.build_repository_name}/{args.build_source_branch}",
        helix_type=helix_type,
        helix_access_token=args.helix_access_token,
        helix_target_queues=[args.queue],
        helix_pre_commands=helix_pre_commands,
        helix_post_commands=helix_post_commands,
        creator=creator,
        architecture=args.architecture,
        work_item_timeout=work_item_timeout,
        work_item_dir=work_item_dir,
        correlation_payload_dir=payload_dir,
        project_file=args.project_file,
        build_config=build_config,
        performance_repo_dir=args.performance_repo_dir,
        helix_build=args.build_number,
        partition_count=args.partition_count,
        runtime_flavor=args.runtime_flavor or "",
        hybrid_globalization=args.hybrid_globalization,
        target_csproj=args.target_csproj,
        work_item_command=work_item_command,
        baseline_work_item_command=baseline_work_item_command,
        bdn_arguments=bdn_arguments,
        baseline_bdn_arguments=baseline_bdn_arguments,
        download_files_from_helix=True,
        targets_windows=args.os_group == "windows",
        helix_results_destination_dir=helix_results_destination_dir,
        python="python",
        affinity=args.affinity,
        compare=args.compare,
        compare_command=compare_command,
        only_sanity_check=args.only_sanity_check,
        ios_strip_symbols=args.ios_strip_symbols,
        ios_llvm_build=args.ios_llvm_build,
        fail_on_test_failure=fail_on_test_failure)
    
    if args.send_to_helix:
        perf_send_to_helix(perf_send_to_helix_args)

        results_glob = os.path.join(helix_results_destination_dir, '**', '*perf-lab-report.json')
        all_results: list[Any] = []
        for result_file in glob(results_glob, recursive=True):
            with open(result_file, 'r', encoding="utf8") as report_file:
                all_results.extend(json.load(report_file))

        output_counters_for_crank(all_results)
    else:
        # expose environment variables to CI for sending to helix
        perf_send_to_helix_args.set_environment_variables(save_to_pipeline=True)

        

def main(argv: List[str]):
    args: dict[str, Any] = {}

    i = 1
    while i < len(argv):
        key = argv[i]
        bool_args = {
            "--internal": "internal",
            "--physical-promotion": "physical_promotion_run_type",
            "--is-scenario": "is_scenario",
            "--local-build": "local_build",
            "--compare": "compare",
            "--ios-llvm-build": "ios_llvm_build",
            "--ios-strip-symbols": "ios_strip_symbols",
            "--hybrid-globalization": "hybrid_globalization",
            "--send-to-helix": "send_to_helix",
            "--performance-repo-ci": "performance_repo_ci",
            "--only-sanity-check": "only_sanity_check",
            "--use-local-commit-time": "use_local_commit_time",
        }

        if key in bool_args:
            args[bool_args[key]] = True
            i += 1
            continue

        simple_arg_map = {
            "--queue": "queue",
            "--framework": "framework",
            "--run-kind": "run_kind",
            "--architecture": "architecture",
            "--core-root-dir": "core_root_dir",
            "--baseline-core-root-dir": "baseline_core_root_dir",
            "--performance-repo-dir": "performance_repo_dir",
            "--mono-dotnet-dir": "mono_dotnet_dir",
            "--libraries-download-dir": "libraries_download_dir",
            "--versions-props-path": "versions_props_path",
            "--browser-versions-props-path": "browser_versions_props_path",
            "--built-app-dir": "built_app_dir",
            "--perflab-upload-token": "perflab_upload_token",
            "--helix-access-token": "helix_access_token",
            "--project-file": "project_file",
            "--build-repository-name": "build_repository_name",
            "--build-source-branch": "build_source_branch",
            "--build-number": "build_number",
            "--pgo-run-type": "pgo_run_type",
            "--r2r-run-type": "r2r_run_type",
            "--codegen-type": "codegen_type",
            "--runtime-type": "runtime_type",
            "--run-categories": "run_categories",
            "--extra-bdn-args": "extra_bdn_args",
            "--affinity": "affinity",
            "--os-group": "os_group",
            "--os-sub-group": "os_sub_group",
            "--runtime-flavor": "runtime_flavor",
            "--javascript-engine": "javascript_engine",
            "--experiment-name": "experiment_name",
            "--channel": "channel",
            "--perf-hash": "perf_hash",
            "--os-version": "os_version",
            "--dotnet-version-link": "dotnet_version_link",
            "--target-csproj": "target_csproj",
            "--pdn-path": "pdn_path",
            "--runtime-repo-dir": "runtime_repo_dir",
            "--logical-machine": "logical_machine"
        }

        if key in simple_arg_map:
            arg_name = simple_arg_map[key]
            val = argv[i + 1]
        elif key == "--partition-count":
            arg_name = "partition_count"
            val = int(argv[i + 1])
        elif key == "--run-env-vars":
            val = {}
            while i < len(argv):
                i += 1
                arg = argv[i]
                if arg.startswith("--"):
                    break
                k, v = arg.split("=")
                val[k] = v
            args["run_env_vars"] = val
            continue
        else:
            raise Exception(f"Invalid argument: {key}")

        args[arg_name] = val
        i += 2

    run_performance_job(RunPerformanceJobArgs(**args))

if __name__ == "__main__":
    main(sys.argv)