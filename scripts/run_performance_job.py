from logging import getLogger
import re
from dataclasses import dataclass, field
from datetime import timedelta
from glob import glob
import json
import os
import shutil
from subprocess import CalledProcessError
import sys
import tempfile
from traceback import format_exc
import urllib.request
import xml.etree.ElementTree as ET
from typing import Any, Dict, List, Optional

from build_runtime_payload import *
import ci_setup
from performance.common import RunCommand, set_environment_variable
from performance.logger import setup_loggers
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
    helix_access_token: Optional[str] = os.environ.get("HelixAccessToken")
    os_sub_group: Optional[str] = None
    project_file: Optional[str] = None
    partition_count: Optional[int] = None
    build_repository_name: str = os.environ.get("BUILD_REPOSITORY_NAME", "dotnet/performance")
    build_source_branch: str = os.environ.get("BUILD_SOURCEBRANCH", "main")
    build_number: str = os.environ.get("BUILD_BUILDNUMBER", "local")
    build_definition_name: Optional[str] = os.environ.get("BUILD_DEFINITIONNAME")
    build_reason: Optional[str] = os.environ.get("BUILD_REASON")
    internal: bool = False
    pgo_run_type: Optional[str] = None
    physical_promotion_run_type: Optional[str] = None
    r2r_run_type: Optional[str] = None
    experiment_name: Optional[str] = None
    codegen_type: str = "JIT"
    linking_type: str = "dynamic"
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
    build_config: str = "Release"
    live_libraries_build_config: Optional[str] = None

def get_pre_commands(
        os_group: str,
        internal: bool,
        runtime_type: str,
        codegen_type: str,
        v8_version: str):
    helix_pre_commands: list[str] = []

    # Remember the previous PYTHONPATH that was set so it can be restored in the post commands
    if os_group == "windows":
        helix_pre_commands += ["set ORIGPYPATH=%PYTHONPATH%"]
    else:
        helix_pre_commands += ["export ORIGPYPATH=$PYTHONPATH"]

    # Create separate list of commands to handle the next part. 
    # On non-Windows, these commands are chained together with && so they will stop if any fail
    install_prerequisites: list[str] = []

    if internal:
        # Run inside a python venv
        if os_group == "windows":
            install_prerequisites += [
                "py -3 -m venv %HELIX_WORKITEM_ROOT%\\.venv",
                "call %HELIX_WORKITEM_ROOT%\\.venv\\Scripts\\activate.bat",
                "echo on" # venv activate script turns echo off, so turn it back on
            ]
        else:
            if os_group != "osx":
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
        if os_group == "windows":
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
        if os_group != "windows" and os_group != "osx":
            install_prerequisites += [
                "sudo apt-get update",
                "sudo apt -y install curl dirmngr apt-transport-https lsb-release ca-certificates"
            ]

    # Set up everything needed for WASM runs
    if runtime_type == "wasm":
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

    # Add the install_prerequisites to the pre_commands
    if os_group == "windows":
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
    if codegen_type.lower() == "interpreter" and runtime_type == "mono":
        if os_group == "windows":
            helix_pre_commands += ['set MONO_ENV_OPTIONS="--interpreter"']
        else:
            helix_pre_commands += ['export MONO_ENV_OPTIONS="--interpreter"']

    # Enable MSBuild node communication logs
    if os_group == "windows":
        helix_pre_commands += ["set MSBUILDDEBUGCOMM=1", 'set "MSBUILDDEBUGPATH=%HELIX_WORKITEM_UPLOAD_ROOT%"']
    else:
        helix_pre_commands += ["export MSBUILDDEBUGCOMM=1", 'export "MSBUILDDEBUGPATH=$HELIX_WORKITEM_UPLOAD_ROOT"']

    # Copy the performance repo and root directory to the work item directory
    if os_group == "windows":
        helix_pre_commands += [ 
            "robocopy /np /nfl /ndl /e %HELIX_CORRELATION_PAYLOAD%\\performance %HELIX_WORKITEM_ROOT%\\performance",
            "robocopy /np /nfl /ndl /e %HELIX_CORRELATION_PAYLOAD%\\root %HELIX_WORKITEM_ROOT%" ]
    else:
        helix_pre_commands += [ 
            "mkdir -p $HELIX_WORKITEM_ROOT/performance && cp -R $HELIX_CORRELATION_PAYLOAD/performance/* $HELIX_WORKITEM_ROOT/performance",
            "cp -R $HELIX_CORRELATION_PAYLOAD/root/* $HELIX_WORKITEM_ROOT" ]

    # invoke the machine-setup
    if os_group == "windows":
        helix_pre_commands += ["call %HELIX_WORKITEM_ROOT%\\machine-setup.cmd"]
    else:
        helix_pre_commands += [
            "chmod +x $HELIX_WORKITEM_ROOT/machine-setup.sh",
            ". $HELIX_WORKITEM_ROOT/machine-setup.sh",
        ]

    # ensure that the PYTHONPATH is set to the scripts directory
    # TODO: Run scripts out of work item directory instead of payload directory
    if os_group == "windows":
        helix_pre_commands += ["set PYTHONPATH=%HELIX_CORRELATION_PAYLOAD%\\scripts%3B%HELIX_CORRELATION_PAYLOAD%"]
    else:
        helix_pre_commands += ["export PYTHONPATH=$HELIX_CORRELATION_PAYLOAD/scripts:$HELIX_CORRELATION_PAYLOAD"]

    if runtime_type == "iOSMono":
        if os_group == "windows":
            helix_pre_commands += ["%HELIX_CORRELATION_PAYLOAD%\\monoaot\\mono-aot-cross --llvm --version"]
        else:
            helix_pre_commands += ["$HELIX_CORRELATION_PAYLOAD/monoaot/mono-aot-cross --llvm --version"]
        
    return helix_pre_commands

def get_post_commands(os_group: str, runtime_type: str):
    if os_group == "windows":
        helix_post_commands = ["set PYTHONPATH=%ORIGPYPATH%"]
    else:
        helix_post_commands = ["export PYTHONPATH=$ORIGPYPATH"]

    if runtime_type == "wasm" and os_group != "windows":
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

def logical_machine_to_queue(logical_machine: str, internal: bool, os_group: str, architecture: str):
    if os_group == "windows":
        if not internal:
            return "Windows.10.Amd64.ClientRS4.DevEx.15.8.Open"
        else:
            queue_map = {
                "perftiger": "Windows.11.Amd64.Tiger.Perf",
                "perftiger_crossgen": "Windows.11.Amd64.Tiger.Perf",
                "perfpixel4a": "Windows.11.Amd64.Pixel.Perf",
                "perfampere": "Windows.Server.Arm64.Perf",
                "perfviper": "Windows.11.Amd64.Viper.Perf",
                "cloudvm": "Windows.10.Amd64"
            }
            return queue_map.get(logical_machine, "Windows.11.Amd64.Tiger.Perf")
    else:
        if not internal:
            if architecture == "arm64":
                return "ubuntu.1804.armarch.open"
            else:
                return "Ubuntu.2204.Amd64.Open"
        else:
            queue_map = {
                "perfampere": "Ubuntu.2204.Arm64.Perf",
                "perfiphone12mini": "OSX.13.Amd64.Iphone.Perf",
                "perftiger_crossgen": "Ubuntu.1804.Amd64.Tiger.Perf",
                "perfviper": "Ubuntu.2204.Amd64.Viper.Perf",
                "cloudvm": "Ubuntu.2204.Amd64"
            }
            return queue_map.get(logical_machine, "Ubuntu.2204.Amd64.Tiger.Perf")

def get_bdn_arguments(
        run_categories: str,
        internal: bool,
        os_group: str,
        runtime_type: str,
        codegen_type: str,
        only_sanity_check: bool = False,
        affinity: Optional[str] = None,
        experiment_name: Optional[str] = None,
        javascript_engine: Optional[str] = None,
        javascript_engine_path: Optional[str] = None,
        product_version: Optional[str] = None,
        corerun_payload_dir: Optional[str] = None,
        extra_bdn_args: Optional[str] = None):
    
    bdn_arguments = ["--anyCategories", run_categories]

    if affinity is not None and not "0":
        bdn_arguments += ["--affinity", affinity]

    if not internal:
        bdn_arguments += [
            "--iterationCount", "1", 
            "--warmupCount", "0", 
            "--invocationCount", "1", 
            "--unrollFactor", "1", 
            "--strategy", "ColdStart", 
            "--stopOnFirstError", "true"
        ]

    category_exclusions: list[str] = []

    is_aot = codegen_type.lower() == "aot"
    if runtime_type == "mono":
        # TODO: Validate if this exclusion filter is still needed
        bdn_arguments += ["--exclusion-filter", "*Perf_Image*", "*Perf_NamedPipeStream*"]

        if is_aot:
            category_exclusions += ["NoAOT", "NoWASM"]
            bdn_arguments += [
                "--runtimes", "monoaotllvm",
                "--aotcompilerpath", "$HELIX_CORRELATION_PAYLOAD/monoaot/mono-aot-cross",
                "--customruntimepack", "$HELIX_CORRELATION_PAYLOAD/monoaot/pack", 
                "--aotcompilermode", "llvm",
            ]
        else:
            category_exclusions += ["NoMono"]

        if codegen_type.lower() == "interpreter":
            category_exclusions += ["NoInterpreter"]

    if experiment_name == "memoryRandomization":
        bdn_arguments += ["--memoryRandomization", "true"]

    if runtime_type == "wasm":
        category_exclusions += ["NoInterpreter", "NoWASM", "NoMono"]

        wasm_args = ["--expose_wasm"]
        if javascript_engine == "v8":
            wasm_args += ["--module"]

        bdn_arguments += [
            "--wasmEngine", javascript_engine_path,
            f"\\\"--wasmArgs={' '.join(wasm_args)}\\\"",
            "--cli", "$HELIX_CORRELATION_PAYLOAD/dotnet/dotnet",
            "--wasmDataDir", "$HELIX_CORRELATION_PAYLOAD/wasm-data"
        ]

        if is_aot:
            bdn_arguments += [
                "--aotcompilermode", "wasm",
                "--buildTimeout", "3600"
            ]

    if category_exclusions:
        bdn_arguments += ["--category-exclusion-filter", *set(category_exclusions)]

    bdn_arguments += ["--logBuildOutput", "--generateBinLog"]

    if only_sanity_check:
        bdn_arguments += ["--filter", "System.Tests.Perf_*"]

    if runtime_type == "mono" and not is_aot:
        assert product_version is not None
        if os_group == "windows":
            bdn_arguments += ["--corerun", f"%HELIX_CORRELATION_PAYLOAD%\\dotnet-mono\\shared\\Microsoft.NETCore.App\\{product_version}\\corerun.exe"]
        else:
            bdn_arguments += ["--corerun", f"$HELIX_CORRELATION_PAYLOAD/dotnet-mono/shared/Microsoft.NETCore.App/{product_version}/corerun"]

    if corerun_payload_dir is not None:
        if os_group == "windows":
            bdn_arguments += ["--corerun", f"%HELIX_CORRELATION_PAYLOAD%\\{corerun_payload_dir}\\CoreRun.exe"]
        else:
            bdn_arguments += ["--corerun", f"$HELIX_CORRELATION_PAYLOAD/{corerun_payload_dir}/corerun"]

    if extra_bdn_args:
        bdn_arguments += extra_bdn_args.split(" ")

    return bdn_arguments

def get_run_configurations(
        run_kind: str,
        runtime_type: str,
        codegen_type: str,
        pgo_run_type: Optional[str] = None,
        physical_promotion_run_type: Optional[str] = None,
        r2r_run_type: Optional[str] = None,
        hybrid_globalization: bool = False,
        experiment_name: Optional[str] = None,
        linking_type: Optional[str] = None,
        runtime_flavor: Optional[str] = None,
        ios_llvm_build: bool = False,
        ios_strip_symbols: bool = False,
        javascript_engine: Optional[str] = None):
    
    configurations = { "CompilationMode": "Tiered", "RunKind": run_kind }

    is_aot = codegen_type.lower() == "aot"
    if runtime_type == "mono":
        llvm = is_aot and not run_kind == "android_scenarios"
        configurations["LLVM"] = str(llvm)
        configurations["MonoInterpreter"] = str(codegen_type.lower() == "interpreter")
        configurations["MonoAOT"] = str(is_aot)

    if runtime_type == "wasm":
        configurations["CompilationMode"] = "wasm"
        if is_aot:
            configurations["AOT"] = "true"

        if javascript_engine == "javascriptcore":
            configurations["JSEngine"] = "javascriptcore"

    if pgo_run_type == "nodynamicpgo":
        configurations["PGOType"] = "nodynamicpgo"

    if physical_promotion_run_type == "physicalpromotion":
        configurations["PhysicalPromotionType"] = "physicalpromotion"

    if r2r_run_type == "nor2r":
        configurations["R2RType"] = "nor2r"

    if hybrid_globalization:
        configurations["HybridGlobalization"] = "True"

    if experiment_name is not None:
        configurations["ExperimentName"] = experiment_name

    # dotnet/runtime Android sample app scenarios
    if run_kind == "android_scenarios":
        configurations["CodegenType"] = str(codegen_type)
        configurations["LinkingType"] = str(linking_type)
        configurations["RuntimeType"] = str(runtime_flavor)

    # .NET Android and .NET MAUI Android sample app scenarios
    if run_kind == "maui_scenarios_android":
        if not runtime_flavor in ("mono", "coreclr"):
            raise Exception("Runtime flavor must be specified for maui_scenarios_android")
        configurations["CodegenType"] = str(codegen_type)
        configurations["RuntimeType"] = str(runtime_flavor)

    # .NET iOS and .NET MAUI iOS sample app scenarios
    if run_kind == "maui_scenarios_ios":
        if not runtime_flavor in ("mono", "coreclr"):
            raise Exception("Runtime flavor must be specified for maui_scenarios_ios")
        configurations["CodegenType"] = str(codegen_type)
        configurations["RuntimeType"] = str(runtime_flavor)

    if runtime_type == "iOSMono":
        configurations["iOSLlvmBuild"] = str(ios_llvm_build)
        configurations["iOSStripSymbols"] = str(ios_strip_symbols)
        configurations["RuntimeType"] = "Mono"

    if runtime_type == "iOSNativeAOT":
        configurations["iOSStripSymbols"] = str(ios_strip_symbols)
        configurations["RuntimeType"] = "NativeAOT"

    return configurations

def get_work_item_command(os_group: str, target_csproj: str, architecture: str, perf_lab_framework: str, internal: bool, wasm: bool, bdn_artifacts_dir: str):
    if os_group == "windows":
        work_item_command = [
            "python",
            "%HELIX_WORKITEM_ROOT%\\performance\\scripts\\benchmarks_ci.py", 
            "--csproj", f"%HELIX_WORKITEM_ROOT%\\performance\\{target_csproj}"]
    else:
        work_item_command = [
            "python",
            "$HELIX_WORKITEM_ROOT/performance/scripts/benchmarks_ci.py", 
            "--csproj", f"$HELIX_WORKITEM_ROOT/performance/{target_csproj}"]
        
    work_item_command += [ 
        "--incremental", "no",
        "--architecture", architecture,
        "-f", perf_lab_framework]
    
    if internal:
        work_item_command += ["--upload-to-perflab-container"]

    if perf_lab_framework != "net462":
        if os_group == "windows":
            work_item_command += ["--dotnet-versions", "%DOTNET_VERSION%"]
        else:
            work_item_command += ["--dotnet-versions", "$DOTNET_VERSION"]

    if wasm:
        work_item_command += ["--run-isolated", "--wasm", "--dotnet-path", "$HELIX_CORRELATION_PAYLOAD/dotnet/"]

    work_item_command += ["--bdn-artifacts", bdn_artifacts_dir]

    return work_item_command

def run_performance_job(args: RunPerformanceJobArgs):
    setup_loggers(verbose=True)

    if args.queue is None:
        if args.logical_machine is None:
            raise Exception("Either queue or logical machine must be specifed")
        args.queue = logical_machine_to_queue(args.logical_machine, args.internal, args.os_group, args.architecture)

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
        helix_type = f"test/performance/{args.run_kind}/{args.framework}/{args.architecture}/"
        if args.runtime_type == "wasm":
            if args.codegen_type.lower() == "aot":
                helix_type += "/wasm/aot"
            else:
                helix_type += "/wasm"

    if not args.send_to_helix:
        # _BuildConfig is used by CI during log publishing
        set_environment_variable("_BuildConfig", build_config, save_to_pipeline=True) 
    
    if args.project_file is None:
        args.project_file = os.path.join(args.performance_repo_dir, "eng", "performance", "helix.proj")
    
    args.performance_repo_dir = os.path.abspath(args.performance_repo_dir)

    if args.target_csproj is None:
        if args.os_group == "windows":
            args.target_csproj="src\\benchmarks\\micro\\MicroBenchmarks.csproj"
        else:
            args.target_csproj="src/benchmarks/micro/MicroBenchmarks.csproj"
    elif args.os_group != "windows":
        args.target_csproj = args.target_csproj.replace("\\", "/")

    if args.libraries_download_dir is None and not args.performance_repo_ci and args.runtime_repo_dir is not None:
        args.libraries_download_dir = os.path.join(args.runtime_repo_dir, "artifacts")
    
    ios_mono = args.runtime_type == "iOSMono"
    ios_nativeaot = args.runtime_type == "iOSNativeAOT"
    is_aot = args.codegen_type.lower() == "aot"
    is_mono = args.runtime_type == "mono"
    mono_aot = is_mono and is_aot
    mono_dotnet = is_mono and not is_aot
    wasm = args.runtime_type == "wasm"
    wasm_aot = wasm and is_aot

    working_dir = os.path.join(args.performance_repo_dir, "CorrelationStaging") # folder in which the payload and workitem directories will be made
    work_item_dir = os.path.join(working_dir, "workitem", "") # Folder in which the work item commands will be run in
    payload_dir = os.path.join(working_dir, "payload", "") # Uploaded folder containing everything needed to run the performance test
    root_payload_dir = os.path.join(payload_dir, "root") # folder that will get copied into the root of the payload directory

    # clear payload directory
    if os.path.exists(working_dir):
        getLogger().info("Clearing existing payload directory")
        shutil.rmtree(working_dir)

    # ensure directories exist
    os.makedirs(work_item_dir, exist_ok=True)
    os.makedirs(root_payload_dir, exist_ok=True)

    # Include a copy of the whole performance in the payload directory
    performance_payload_dir = os.path.join(payload_dir, "performance")
    getLogger().info("Copying performance repository to payload directory")
    shutil.copytree(args.performance_repo_dir, performance_payload_dir, ignore=shutil.ignore_patterns("CorrelationStaging", ".git", "artifacts", ".dotnet", ".venv", ".vs"))

    if args.internal:
        creator = ""
        scenario_arguments = ["--upload-to-perflab-container"]
        helix_source_prefix = "official"
        if args.helix_access_token is None:
            raise Exception("HelixAccessToken environment variable is not configured")
    else:
        args.helix_access_token = None
        os.environ.pop("HelixAccessToken", None) # in case the environment variable is set on the system already
        creator = args.build_definition_name or ""
        if args.performance_repo_ci:
            creator = "dotnet-performance"
        scenario_arguments = []
        if args.build_reason == "PullRequest":
            helix_source_prefix = "pr"
        else:
            helix_source_prefix = "ci"

    if wasm_aot:
        build_config = f"wasmaot.{build_config}"
    elif wasm:
        build_config = f"wasm.{build_config}"

    if args.run_kind == "android_scenarios":
        if args.runtime_type == "AndroidMono":
            args.runtime_flavor = "mono"
        elif args.runtime_type == "AndroidCoreCLR":
            args.runtime_flavor = "coreclr"
        else:
            raise Exception("Android scenarios only support Mono and CoreCLR runtimes")
    
    branch = os.environ.get("BUILD_SOURCEBRANCH")
    cleaned_branch_name = "main"
    if branch is not None and branch.startswith("refs/heads/release"):
        cleaned_branch_name = branch.replace("refs/heads/", "")

    configurations = get_run_configurations(
        args.run_kind, args.runtime_type, args.codegen_type, args.pgo_run_type, args.physical_promotion_run_type,
        args.r2r_run_type, args.hybrid_globalization, args.experiment_name, args.linking_type,
        args.runtime_flavor, args.ios_llvm_build, args.ios_strip_symbols, args.javascript_engine
    )

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

    product_version = None
    if mono_dotnet and not mono_aot:
        if args.framework is None:
            raise Exception("Framework must be specified for Mono dotnet runs")
        
        if args.versions_props_path is None:
            if args.runtime_repo_dir is None:
                raise Exception("Please provide either the product version, a path to Versions.props, or a runtime repo directory")
            args.versions_props_path = os.path.join(args.runtime_repo_dir, "eng", "Versions.props")

        with open(args.versions_props_path) as f:
            for line in f:
                match = re.search(r"ProductVersion>([^<]*)<", line)
                if match:
                    product_version = match.group(1)
                    break
        if product_version is None:
            raise Exception("Unable to find ProductVersion in Versions.props")
        
        mono_dotnet_path = os.path.join(payload_dir, "dotnet-mono")
        getLogger().info("Copying mono dotnet directory to payload directory")
        if args.mono_dotnet_dir is None:
            build_mono_payload(
                mono_dotnet_path, 
                args.os_group, 
                args.framework, 
                args.build_config, 
                args.architecture,
                product_version,
                runtime_repo_dir=args.runtime_repo_dir,
                mono_archive_or_dir=os.path.join(args.libraries_download_dir, "bin") if args.libraries_download_dir else None)
        else:
            shutil.copytree(args.mono_dotnet_dir, mono_dotnet_path, dirs_exist_ok=True)

    v8_version = ""
    if wasm:
        if args.libraries_download_dir is None:
            raise Exception("Libraries not downloaded for wasm runs")
        
        getLogger().info("Copying wasm bundle directory to payload directory")
        browser_wasm_dir = os.path.join(args.libraries_download_dir, "BrowserWasm")
        build_wasm_payload(browser_wasm_dir, payload_dir, runtime_repo_dir=args.runtime_repo_dir)

        if args.javascript_engine == "v8":
            if args.browser_versions_props_path is None:
                if args.runtime_repo_dir is None:
                    raise Exception("BrowserVersions.props must be present for wasm runs")
                args.browser_versions_props_path = os.path.join(args.runtime_repo_dir, "eng", "testing", "BrowserVersions.props")

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

        ci_setup_arguments.dotnet_path = f"{payload_dir}/dotnet"

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
        if not args.libraries_download_dir:
            raise Exception("Libraries not downloaded for MonoAOT")

        linux_mono_aot_dir = os.path.join(args.libraries_download_dir, "LinuxMonoAOT")
        monoaot_dotnet_path = os.path.join(payload_dir, "monoaot")

        getLogger().info("Copying MonoAOT build to payload directory")
        build_monoaot_payload(linux_mono_aot_dir, monoaot_dotnet_path, args.architecture)

    use_core_run = False
    use_baseline_core_run = False
    if not args.performance_repo_ci and args.runtime_type == "coreclr":
        use_core_run = True
        coreroot_payload_dir = os.path.join(payload_dir, "Core_Root")
        if args.core_root_dir is None:
            if args.runtime_repo_dir is None:
                raise Exception("Core_Root directory must be specified for non-performance CI runs")
            
            build_coreroot_payload(
                args.runtime_repo_dir, 
                core_root_dest=coreroot_payload_dir, 
                os_group=args.os_group, 
                architecture=args.architecture,
                libraries_config=args.live_libraries_build_config)
        else:
            getLogger().info("Copying Core_Root directory to payload directory")
            shutil.copytree(args.core_root_dir, coreroot_payload_dir, ignore=shutil.ignore_patterns("*.pdb"))

        if args.baseline_core_root_dir is not None:
            use_baseline_core_run = True
            baseline_coreroot_payload_dir = os.path.join(payload_dir, "Baseline_Core_Root")
            getLogger().info("Copying Baseline Core_Root directory to payload directory")
            shutil.copytree(args.baseline_core_root_dir, baseline_coreroot_payload_dir)
    
    if args.maui_version is not None:
        ci_setup_arguments.maui_version = args.maui_version

    if args.built_app_dir is None:
        if args.runtime_repo_dir is not None:
            args.built_app_dir = args.runtime_repo_dir
    
    if args.run_kind == "android_scenarios":
        if args.built_app_dir is None:
            raise Exception("Built apps directory must be present for Android benchmarks")
        getLogger().info("Copying Android apps to payload directory")
        shutil.copy(os.path.join(args.built_app_dir, "androidHelloWorld", "HelloAndroid.apk"), os.path.join(root_payload_dir, "HelloAndroid.apk"))

        android_binlog_dir = os.path.join(root_payload_dir, "androidHelloWorldBinlog")
        shutil.copytree(os.path.join(args.built_app_dir, "androidHelloWorldBinlog"), android_binlog_dir)

        binlog_files = glob(os.path.join(android_binlog_dir, "**", "*.binlog"))
        if binlog_files:
            dest = os.path.join(android_binlog_dir, "msbuild.binlog")
            getLogger().info(f"Moving {binlog_files[0]} to {dest}")
            shutil.move(binlog_files[0], dest)
        # Disabled due to not successfully building at the moment. https://github.com/dotnet/performance/issues/4729
        # if android_mono:
            # shutil.copy(os.path.join(args.built_app_dir, "MonoBenchmarksDroid.apk"), os.path.join(root_payload_dir, "MonoBenchmarksDroid.apk"))
        ci_setup_arguments.architecture = "arm64"

    if ios_mono or ios_nativeaot:
        if args.built_app_dir is None:
            raise Exception("Built apps directory must be present for IOS Mono or IOS Native AOT benchmarks")
        
        getLogger().info("Copying IOS apps to payload directory")
        ios_hello_world_dir = os.path.join(payload_dir, "iosHelloWorld")
        os.makedirs(ios_hello_world_dir, exist_ok=True) # need to create the dir since the files actually get copied into a child dir
        shutil.copytree(os.path.join(args.built_app_dir, "iosHelloWorld"), os.path.join(ios_hello_world_dir, "iosHelloWorld"))

        ios_hello_world_zip_dir = os.path.join(payload_dir, "iosHelloWorldZip")
        shutil.copytree(os.path.join(args.built_app_dir, "iosHelloWorldZip"), ios_hello_world_zip_dir)

        # Find the zip file in the directory and move it to iOSSampleApp.zip
        zip_files = glob(os.path.join(ios_hello_world_zip_dir, "**", "*.zip"))
        if zip_files:
            dest = os.path.join(ios_hello_world_zip_dir, "iOSSampleApp.zip")
            getLogger().info(f"Moving {zip_files[0]} to {dest}")
            shutil.move(zip_files[0], dest)

        ios_hello_world_binlog_dir = os.path.join(payload_dir, "iosHelloWorldBinlog")
        shutil.copytree(os.path.join(args.built_app_dir, "iosHelloWorldBinlog"), ios_hello_world_binlog_dir)

        binlog_files = glob(os.path.join(ios_hello_world_binlog_dir, "**", "*.binlog"))
        if binlog_files:
            dest = os.path.join(ios_hello_world_binlog_dir, "msbuild.binlog")
            getLogger().info(f"Moving {binlog_files[0]} to {dest}")
            shutil.move(binlog_files[0], dest)

    # ensure work item directory is not empty
    getLogger().info("Copying docs to work item directory so it isn't empty")
    shutil.copytree(os.path.join(args.performance_repo_dir, "docs"), work_item_dir, dirs_exist_ok=True)

    if args.os_group == "windows":
        agent_python = "py -3"
    else:
        agent_python = "python3"

    helix_pre_commands = get_pre_commands(args.os_group, args.internal, args.runtime_type, args.codegen_type, v8_version)
    helix_post_commands = get_post_commands(args.os_group, args.runtime_type)

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

    # Make a backup of the global.json file as we need to restore it before we send to helix
    global_json_path = os.path.join(args.performance_repo_dir, "global.json")
    global_json_backup_path = f"{global_json_path}.bak"
    shutil.copy(global_json_path, global_json_backup_path)
    
    ci_setup.main(ci_setup_arguments)

    # ci_setup may modify global.json, so we should copy it across to the payload directory if that happens
    # TODO: Refactor this when we eventually remove the dependency on ci_setup.py directly from the runtime repository.
    getLogger().info("Copying global.json to payload directory")
    shutil.copy(global_json_path, os.path.join(performance_payload_dir, 'global.json'))

    # Building CertHelper needs to happen here as we need it on every run. This also means that we will need to move the calculation
    # of the parameters needed outside of the if block

    framework = os.environ["PERFLAB_Framework"]
    os.environ["PERFLAB_TARGET_FRAMEWORKS"] = framework
    if args.os_group == "windows":
        runtime_id = f"win-{args.architecture}"
    elif args.os_group == "osx":
        runtime_id = f"osx-{args.architecture}"
    else:
        runtime_id = "linux" + (f"{args.os_sub_group.replace('_', '-')}" if args.os_sub_group else "") + f"-{args.architecture}"

    dotnet_executable_path = os.path.join(ci_setup_arguments.dotnet_path or ci_setup_arguments.install_dir, "dotnet")
    ci_artifacts_log_dir = os.path.join(args.performance_repo_dir, 'artifacts', 'log', build_config)

    def publish_dotnet_app_to_payload(payload_dir_name, csproj_path, self_contained=True):
        RunCommand([
            dotnet_executable_path, "publish", 
            "-c", "Release", 
            "-o", os.path.join(payload_dir, payload_dir_name),
            "-f", framework,
            "-r", runtime_id,
            "--self-contained" if self_contained else "",
            csproj_path,
            f"/bl:{os.path.join(ci_artifacts_log_dir, f'{payload_dir_name}.binlog')}",
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"],
            verbose=True).run()

    publish_dotnet_app_to_payload("certhelper", os.path.join(args.performance_repo_dir, "src", "tools", "CertHelper", "CertHelper.csproj"))

    if args.is_scenario:
        set_environment_variable("DOTNET_ROOT", ci_setup_arguments.install_dir, save_to_pipeline=True)
        getLogger().info(f"Set DOTNET_ROOT to {ci_setup_arguments.install_dir}")

        new_path = f"{ci_setup_arguments.install_dir}{os.pathsep}{os.environ['PATH']}"
        set_environment_variable("PATH", new_path, save_to_pipeline=True)
        getLogger().info(f"Set PATH to {new_path}")

        os.environ["MSBUILDDISABLENODEREUSE"] = "1" # without this, MSbuild will be kept alive

        # build Startup
        publish_dotnet_app_to_payload("startup", os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "Startup", "Startup.csproj"))

        # build SizeOnDisk
        publish_dotnet_app_to_payload("SOD", os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "SizeOnDisk", "SizeOnDisk.csproj"))
        
        if args.performance_repo_ci:
            # build MemoryConsumption
            publish_dotnet_app_to_payload(
                "MemoryConsumption", 
                os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "MemoryConsumption", "MemoryConsumption.csproj"))
            
            # build PerfLabGenericEventSourceForwarder
            publish_dotnet_app_to_payload(
                "PerfLabGenericEventSourceForwarder",
                os.path.join(args.performance_repo_dir, "src", "tools", "PerfLabGenericEventSourceForwarder", "PerfLabGenericEventSourceForwarder", "PerfLabGenericEventSourceForwarder.csproj"),
                self_contained=False)
            
            # build PerfLabGenericEventSourceLTTngProvider
            if args.os_group != "windows" and args.os_group != "osx" and args.os_version == "2204":
                RunCommand([
                    os.path.join(args.performance_repo_dir, "src", "tools", "PerfLabGenericEventSourceLTTngProvider", "build.sh"),
                    "-o", os.path.join(payload_dir, "PerfLabGenericEventSourceForwarder")],
                    verbose=True).run()
            
            # copy PDN
            if args.os_group == "windows" and args.architecture != "x86" and args.pdn_path is not None:
                pdn_dest = os.path.join(payload_dir, "PDN")
                pdn_file_path = os.path.join(pdn_dest, "PDN.zip")
                getLogger().info(f"Copying PDN from {args.pdn_path} to {pdn_file_path}")
                os.makedirs(pdn_dest, exist_ok=True)
                shutil.copyfile(args.pdn_path, pdn_file_path)

            # create a copy of the environment since we want these to only be set during the following invocation
            environ_copy = os.environ.copy()

            os.environ["CorrelationPayloadDirectory"] = payload_dir
            os.environ["Architecture"] = args.architecture
            os.environ["TargetsWindows"] = "true" if args.os_group == "windows" else "false"
            os.environ["HelixTargetQueues"] = args.queue
            os.environ["Python"] = agent_python
            os.environ["RuntimeFlavor"] = args.runtime_flavor or ''
            os.environ["CodegenType"] = args.codegen_type or ''
            os.environ["HybridGlobalization"] = str(args.hybrid_globalization)

            # TODO: See if these commands are needed for linux as they were being called before but were failing.
            if args.os_group == "windows" or args.os_group == "osx":
                RunCommand([*(agent_python.split(" ")), "-m", "pip", "install", "--user", "--upgrade", "pip"]).run()
                RunCommand([*(agent_python.split(" ")), "-m", "pip", "install", "--user", "urllib3==1.26.19"]).run()
                RunCommand([*(agent_python.split(" ")), "-m", "pip", "install", "--user", "requests"]).run()

            scenarios_path = os.path.join(args.performance_repo_dir, "src", "scenarios")
            script_path = os.path.join(args.performance_repo_dir, "scripts")
            os.environ["PYTHONPATH"] = f"{os.environ.get('PYTHONPATH', '')}{os.pathsep}{script_path}{os.pathsep}{scenarios_path}"
            getLogger().info(f"PYTHONPATH={os.environ['PYTHONPATH']}")

            os.environ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
            os.environ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
            os.environ["UseSharedCompilation"] = "false"

            getLogger().info("Current dotnet directory: %s", ci_setup_arguments.install_dir)
            getLogger().info("If more than one version exist in this directory, usually the latest runtime and sdk will be used.")

            # PreparePayloadWorkItems is only available for scenarios runs defined inside the performance repo
            RunCommand([
                "dotnet", "msbuild", args.project_file, 
                "/restore", 
                "/t:PreparePayloadWorkItems",
                f"/bl:{os.path.join(ci_artifacts_log_dir, 'PrepareWorkItemPayloads.binlog')}",
                f"/p:ArtifactsLogDir={ci_artifacts_log_dir}"],
                verbose=True).run()
            
            # Search for additional binlogs generated by the maui scenarios prepare payload work items to copy to the artifacts log dir
            if args.run_kind in ["maui_scenarios_android", "maui_scenarios_ios"]:
                for binlog_path in glob(os.path.join(payload_dir, "scenarios_out", "**", "*.binlog"), recursive=True):
                    shutil.copy(binlog_path, ci_artifacts_log_dir)

            # restore env vars
            os.environ.update(environ_copy)

        getLogger().info("Copying NuGet.config, shared, and staticdeps to payload directory")
        shutil.copy(os.path.join(performance_payload_dir, "NuGet.config"), os.path.join(root_payload_dir, "NuGet.config"))
        shutil.copytree(os.path.join(performance_payload_dir, "scripts"), os.path.join(payload_dir, "scripts"))
        shutil.copytree(os.path.join(performance_payload_dir, "src", "scenarios", "shared"), os.path.join(payload_dir, "shared"))
        shutil.copytree(os.path.join(performance_payload_dir, "src", "scenarios", "staticdeps"), os.path.join(payload_dir, "staticdeps"))
        
        if args.architecture == "arm64":
            dotnet_dir = os.path.join(ci_setup_arguments.install_dir, "")
            arm64_dotnet_dir = os.path.join(args.performance_repo_dir, "tools", "dotnet", "arm64")
            getLogger().info(f"Copying arm64 dotnet directory to payload dotnet directory")
            shutil.rmtree(dotnet_dir)
            shutil.copytree(arm64_dotnet_dir, dotnet_dir)

        # Zip the workitem directory (for xharness (mobile) based workitems)
        if args.run_kind == "ios_scenarios" or args.run_kind == "android_scenarios":
            getLogger().info("Zipping workitem directory for app bundle")
            with tempfile.TemporaryDirectory() as temp_dir:
                archive_path = shutil.make_archive(os.path.join(temp_dir, 'workitem'), 'zip', work_item_dir)
                shutil.move(archive_path, f"{work_item_dir}.zip")

    def get_bdn_args_for_coreroot_dir(coreroot_dir: Optional[str]):
        return get_bdn_arguments(
            args.run_categories,
            args.internal,
            args.os_group,
            args.runtime_type,
            args.codegen_type,
            args.only_sanity_check,
            args.affinity,
            args.experiment_name,
            args.javascript_engine,
            args.javascript_engine_path,
            product_version,
            coreroot_dir,
            args.extra_bdn_args
        )

    bdn_arguments = get_bdn_args_for_coreroot_dir(coreroot_dir="Core_Root" if use_core_run else None)
    baseline_bdn_arguments = [] if use_baseline_core_run else get_bdn_args_for_coreroot_dir(coreroot_dir="Baseline_Core_Root")

    if args.os_group == "windows":
        bdn_artifacts_directory = "%HELIX_WORKITEM_UPLOAD_ROOT%\\BenchmarkDotNet.Artifacts"
        bdn_baseline_artifacts_dir = "%HELIX_WORKITEM_UPLOAD_ROOT%\\BenchmarkDotNet.Artifacts_Baseline"
    else:
        bdn_artifacts_directory = "$HELIX_WORKITEM_UPLOAD_ROOT/BenchmarkDotNet.Artifacts"
        bdn_baseline_artifacts_dir = "$HELIX_WORKITEM_UPLOAD_ROOT/BenchmarkDotNet.Artifacts_Baseline"
    
    perf_lab_framework = os.environ['PERFLAB_Framework']

    def get_work_item_command_for_artifact_dir(artifact_dir: str):
        assert args.target_csproj is not None
        return get_work_item_command(args.os_group, args.target_csproj, args.architecture, perf_lab_framework, args.internal, wasm, artifact_dir)
    
    work_item_command = get_work_item_command_for_artifact_dir(bdn_artifacts_directory)
    baseline_work_item_command = get_work_item_command_for_artifact_dir(bdn_baseline_artifacts_dir)

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
        
    # Restore original global.json from backup before sending to Helix
    shutil.copy(global_json_backup_path, global_json_path)

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
        codegen_type=args.codegen_type or "",
        linking_type=args.linking_type or "",
        hybrid_globalization=args.hybrid_globalization,
        target_csproj=args.target_csproj,
        work_item_command=work_item_command or None,
        baseline_work_item_command=baseline_work_item_command or None,
        bdn_arguments=bdn_arguments or None,
        baseline_bdn_arguments=baseline_bdn_arguments or None,
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
        fail_on_test_failure=fail_on_test_failure,
        scenario_arguments=scenario_arguments or None)
    
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
    setup_loggers(verbose=True)

    try:
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
                "--only-sanity": "only_sanity_check",
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
                "--helix-access-token": "helix_access_token",
                "--project-file": "project_file",
                "--build-repository-name": "build_repository_name",
                "--build-source-branch": "build_source_branch",
                "--build-number": "build_number",
                "--pgo-run-type": "pgo_run_type",
                "--r2r-run-type": "r2r_run_type",
                "--codegen-type": "codegen_type",
                "--linking-type": "linking_type",
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
                "--logical-machine": "logical_machine",
                "--build-config": "build_config",
                "--live-libraries-build-config": "live_libraries_build_config"
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
        return 0
    except CalledProcessError as ex:
        getLogger().error('Command: "%s", exited with status: %s', ex.cmd, ex.returncode)
    except IOError as ex:
        getLogger().error("I/O error (%s): %s: %s", ex.errno, ex.strerror, ex.filename)
    except Exception:
        getLogger().error('Unexpected error: %s', sys.exc_info()[0])
        getLogger().error(format_exc())
    return 1

if __name__ == "__main__":
    return_code = main(sys.argv)
    sys.exit(return_code)
