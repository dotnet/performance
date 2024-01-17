from dataclasses import dataclass, field
from datetime import timedelta
from glob import glob
import json
import os
import shutil
import sys
import urllib.request
from typing import Any

import ci_setup
from performance.common import RunCommand, iswin
import performance_setup
from send_to_helix import PerfSendToHelixArgs, perf_send_to_helix

def output_counters_for_crank(reports: list[Any]):
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
    queue: str
    framework: str
    run_kind: str
    core_root_dir: str
    performance_repo_dir: str
    architecture: str
    os_group: str
    
    extra_bdn_args: str | None = None
    run_categories: str = 'Libraries Runtime'
    perflab_upload_token: str | None = None
    helix_access_token: str | None = os.environ.get("HelixAccessToken")
    system_access_token: str = os.environ.get("SYSTEM_ACCESSTOKEN", "")
    os_sub_group: str | None = None
    project_file: str | None = None
    partition_count: int = 1
    additional_performance_setup_parameters: dict[str, Any] = field(default_factory=dict[str, Any])
    additional_ci_setup_parameters: dict[str, Any] = field(default_factory=dict[str, Any])
    helix_type_suffix: str = ""
    build_repository_name: str = os.environ.get("BUILD_REPOSITORY_NAME", "dotnet/performance")
    build_source_branch: str = os.environ.get("BUILD_SOURCEBRANCH", "main")
    build_number: str = os.environ.get("BUILD_BUILDNUMBER", "local")
    internal: bool = True
    pgo_run_type: str | None = None
    physical_promotion_run_type: str | None = None
    r2r_run_type: str | None = None
    experiment_name: str | None = None
    codegen_type: str = "JIT"
    runtime_type: str = "coreclr"
    affinity: str | None = "0"
    run_env_vars: dict[str, str] = field(default_factory=dict[str, str])
    is_scenario: bool = False
    runtime_flavor: str | None = None
    local_build: bool = False

def run_performance_job(args: RunPerformanceJobArgs):
    if args.project_file is None:
        args.project_file = os.path.join(args.performance_repo_dir, "eng", "performance", "helix.proj")

    if args.helix_access_token is None:
        raise Exception("HelixAccessToken environment variable is not configured")
    
    if args.perflab_upload_token is None:
        if args.os_group == "windows":
            args.perflab_upload_token = os.environ.get("PerfCommandUploadToken")
        else:
            args.perflab_upload_token = os.environ.get("PerfCommandUploadTokenLinux")
    
    helix_post_commands: list[str] = []

    if args.is_scenario:
        if args.os_group == "windows":
            script_extension = ".cmd"
            additional_helix_pre_commands = [
                f"call %HELIX_CORRELATION_PAYLOAD%\\machine-setup{script_extension}",
                "xcopy %HELIX_CORRELATION_PAYLOAD%\\NuGet.config %HELIX_WORKITEM_ROOT% /Y"
            ]
            preserve_python_path = "set ORIGPYPATH=%PYTHONPATH%"
            python = "py -3"
        else:
            script_extension = ".sh"
            additional_helix_pre_commands = [
                "chmod +x $HELIX_CORRELATION_PAYLOAD/machine-setup.sh",
                f". $HELIX_CORRELATION_PAYLOAD/machine-setup{script_extension}",
                "cp $HELIX_CORRELATION_PAYLOAD/NuGet.config $HELIX_WORKITEM_ROOT"
            ]
            preserve_python_path = "export ORIGPYPATH=$PYTHONPATH"
            python = "python3"
        
        if not args.internal:
            helix_pre_commands = [
                preserve_python_path,
                *additional_helix_pre_commands
            ]
        elif args.os_group == "windows":
            helix_pre_commands = [
                preserve_python_path,
                "py -3 -m venv %HELIX_WORKITEM_PAYLOAD%\\.venv",
                "call %HELIX_WORKITEM_PAYLOAD%\\.venv\\Scripts\\activate.bat",
                "set PYTHONPATH=",
                "py -3 -m pip install azure.storage.blob==12.0.0",
                "py -3 -m pip install azure.storage.queue==12.0.0",
                "py -3 -m pip install urllib3==1.26.18 --force-reinstall",
                f"set \"PERFLAB_UPLOAD_TOKEN={args.perflab_upload_token}\"",
                *additional_helix_pre_commands
            ]
        else:
            helix_pre_commands = [
                preserve_python_path,
                "export CRYPTOGRAPHY_ALLOW_OPENSSL_102=true"
                "sudo apt-get -y install python3-venv",
                "python3 -m venv $HELIX_WORKITEM_PAYLOAD/.venv",
                ". $HELIX_WORKITEM_PAYLOAD/.venv/bin/activate",
                "export PYTHONPATH=",
                "python3 -m pip install -U pip",
                "pip3 install azure.storage.blob==12.0.0",
                "pip3 install azure.storage.queue==12.0.0",
                "pip3 install urllib3==1.26.18 --force-reinstall",
                f"export PERFLAB_UPLOAD_TOKEN=\"{args.perflab_upload_token}\"",
                *additional_helix_pre_commands
            ]
    else:
        if args.os_group == "windows":
            helix_pre_commands = [
                "set ORIGPYPATH=%PYTHONPATH%",
                "py -m pip install -U pip",
                "py -3 -m venv %HELIX_WORKITEM_PAYLOAD%\\.venv",
                "call %HELIX_WORKITEM_PAYLOAD%\\.venv\\Scripts\\activate.bat",
                "set PYTHONPATH=",
                "py -3 -m pip install -U pip",
                "py -3 -m pip install --user urllib3==1.26.18 --force-reinstall",
                "py -3 -m pip install --user azure.storage.blob==12.0.0 --force-reinstall",
                "py -3 -m pip install --user azure.storage.queue==12.0.0 --force-reinstall",
                f'set "PERFLAB_UPLOAD_TOKEN={args.perflab_upload_token}"'
            ]
        elif args.os_sub_group == "_musl":
            helix_pre_commands = [
                "export ORIGPYPATH=$PYTHONPATH",
                "sudo apk add icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib cargo",
                "sudo apk add libgdiplus --repository http://dl-cdn.alpinelinux.org/alpine/edge/testing",
                "python3 -m venv $HELIX_WORKITEM_PAYLOAD/.venv",
                "source $HELIX_WORKITEM_PAYLOAD/.venv/bin/activate",
                "export PYTHONPATH=",
                "python3 -m pip install -U pip",
                "pip3 install --user urllib3==1.26.18 --force-reinstall",
                "pip3 install --user azure.storage.blob==12.7.1 --force-reinstall",
                "pip3 install --user azure.storage.queue==12.1.5 --force-reinstall",
                f'export PERFLAB_UPLOAD_TOKEN="{args.perflab_upload_token}"'
            ]
        else:
            if args.runtime_type == "wasm":
                wasm_precommand = (
                    "sudo apt-get -y remove nodejs && "
                    "curl -fsSL https://deb.nodesource.com/setup_16.x | sudo -E bash - && "
                    "sudo apt-get -y install nodejs && "
                    "npm install --prefix $HELIX_WORKITEM_PAYLOAD jsvu -g && "
                    "$HELIX_WORKITEM_PAYLOAD/bin/jsvu --os=linux64 --engines=v8 && "
                    "find ~/.jsvu -ls && "
                    "~/.jsvu/bin/v8 -e 'console.log(`V8 version: ${this.version()}`)'")
            else:
                wasm_precommand = "echo"

            helix_pre_commands = [
                "export ORIGPYPATH=$PYTHONPATH",
                "export CRYPTOGRAPHY_ALLOW_OPENSSL_102=true",
                'echo "** Installing prerequistes **"',
                ("python3 -m pip install --user -U pip && "
                "sudo apt-get -y install python3-venv && "
                "python3 -m venv $HELIX_WORKITEM_PAYLOAD/.venv && "
                "ls -l $HELIX_WORKITEM_PAYLOAD/.venv/bin/activate && "
                "export PYTHONPATH= && "
                "python3 -m pip install --user -U pip && "
                "pip3 install --user urllib3==1.26.18 && "
                "pip3 install --user azure.storage.blob==12.0.0 && "
                "pip3 install --user azure.storage.queue==12.0.0 && "
                "sudo apt-get update && "
                "sudo apt -y install curl dirmngr apt-transport-https lsb-release ca-certificates && "
                f"{wasm_precommand} && "
                f"export PERFLAB_UPLOAD_TOKEN=\"{args.perflab_upload_token}\" "
                "|| export PERF_PREREQS_INSTALL_FAILED=1"),
            'test "x$PERF_PREREQS_INSTALL_FAILED" = "x1" && echo "** Error: Failed to install prerequites **"'
            ]

    mono_interpreter = False
    if args.codegen_type == "Interpreter" and args.runtime_type == "mono":
        mono_interpreter = True
        if args.os_group == "windows":
            helix_pre_commands += ['set MONO_ENV_OPTIONS="--interpreter"']
        else:
            helix_pre_commands += ['export MONO_ENV_OPTIONS="--interpreter"']

    if args.os_group == "windows":
        helix_pre_commands += ["set MSBUILDDEBUGCOMM=1", 'set "MSBUILDDEBUGPATH=%HELIX_WORKITEM_UPLOAD_ROOT%"']
        helix_post_commands = ["set PYTHONPATH=%ORIGPYPATH%"]
    else:
        helix_pre_commands += ["export MSBUILDDEBUGCOMM=1", 'export "MSBUILDDEBUGPATH=$HELIX_WORKITEM_UPLOAD_ROOT"']
        helix_post_commands = ["export PYTHONPATH=$ORIGPYPATH"]

    if args.is_scenario:
        # these commands are added inside the proj file for non-scenario runs
        if args.os_group == "windows":
            helix_pre_commands += [
                "call %HELIX_WORKITEM_PAYLOAD%\\machine-setup.cmd",
                "set PYTHONPATH=%HELIX_WORKITEM_PAYLOAD%\\scripts%3B%HELIX_WORKITEM_PAYLOAD%"
            ]
        else:
            helix_pre_commands += [
                "chmod +x $HELIX_WORKITEM_PAYLOAD/machine-setup.sh",
                ". $HELIX_WORKITEM_PAYLOAD/machine-setup.sh",
                "export PYTHONPATH=$HELIX_WORKITEM_PAYLOAD/scripts:$HELIX_WORKITEM_PAYLOAD"
            ]

    # TODO: Support custom helix log collection in post command

    working_directory = args.performance_repo_dir
    performance_setup_args = performance_setup.PerformanceSetupArgs(
        performance_directory=args.performance_repo_dir,
        core_root_directory=args.core_root_dir,
        working_directory=working_directory,
        queue=args.queue,
        kind=args.run_kind,
        no_dynamic_pgo=args.pgo_run_type == "nodynamicpgo",
        physical_promotion=args.physical_promotion_run_type == "physicalpromotion",
        no_r2r=args.r2r_run_type == "nor2r",
        experiment_name=args.experiment_name,
        internal=args.internal,
        mono_interpreter=mono_interpreter,
        framework=args.framework,
        use_local_commit_time=False,
        run_categories=args.run_categories,
        extra_bdn_args=[] if args.extra_bdn_args is None else args.extra_bdn_args.split(" "),
        python="py -3" if args.os_group == "windows" else "python3",
        csproj="src\\benchmarks\\micro\\MicroBenchmarks.csproj" if args.os_group == "windows" else "src/benchmarks/micro/MicroBenchmarks.csproj",
        **args.additional_performance_setup_parameters
    )

    performance_setup_data = performance_setup.run(performance_setup_args)
    performance_setup_data.set_environment_variables(save_to_pipeline=False)

    setup_arguments = performance_setup_data.setup_arguments
    for k, v in args.additional_ci_setup_parameters.items():
        setattr(setup_arguments, k, v)
    setup_arguments.local_build = args.local_build

    if args.affinity != "0":
        setup_arguments.affinity = args.affinity

    if args.run_env_vars:
        setup_arguments.run_env_vars = [f"{k}={v}" for k, v in args.run_env_vars.items()]

    setup_arguments.target_windows = args.os_group == "windows"
    if args.os_group == "windows":
        os.environ["TargetsWindows"] = "true"

    if args.is_scenario:
        setup_arguments.output_file = os.path.join(performance_setup_data.payload_directory, "machine-setup")
        setup_arguments.install_dir = os.path.join(performance_setup_data.payload_directory, "dotnet")
    else:
        tools_dir = os.path.join(performance_setup_data.performance_directory, "tools")
        setup_arguments.output_file = os.path.join(tools_dir, "machine-setup")
        setup_arguments.install_dir = os.path.join(tools_dir, "dotnet", performance_setup_data.architecture)

    ci_setup.main(setup_arguments)

    if args.is_scenario:
        performance_setup_data.payload_directory += os.path.sep

        dotnet_path = os.path.join(setup_arguments.install_dir, "dotnet")

        shutil.copyfile(os.path.join(args.performance_repo_dir, "NuGet.config"), performance_setup_data.payload_directory)
        shutil.copytree(os.path.join(args.performance_repo_dir, "scripts"), os.path.join(performance_setup_data.payload_directory, "scripts"))
        shutil.copytree(os.path.join(args.performance_repo_dir, "src", "scenarios", "shared"), os.path.join(performance_setup_data.payload_directory, "shared"))
        shutil.copytree(os.path.join(args.performance_repo_dir, "src", "scenarios", "staticdeps"), os.path.join(performance_setup_data.payload_directory, "staticdeps"))

        framework = os.environ["PERFLAB_Framework"]
        os.environ["PERFLAB_TARGET_FRAMEWORKS"] = framework
        if args.os_group == "windows":
            runtime_id = f"win-{args.architecture}"
        elif args.os_group == "osx":
            runtime_id = f"osx-{args.architecture}"
        else:
            runtime_id = f"linux-{args.architecture}"

        # build Startup
        RunCommand([
            dotnet_path, "publish", 
            "-c", "Release", 
            "-o", os.path.join(performance_setup_data.payload_directory, "startup"),
            "-f", framework,
            "-r", runtime_id,
            "--self-contained",
            os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "Startup", "Startup.csproj"),
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"]).run()

        # build SizeOnDisk
        RunCommand([
            dotnet_path, "publish", 
            "-c", "Release", 
            "-o", os.path.join(performance_setup_data.payload_directory, "SOD"),
            "-f", framework,
            "-r", runtime_id,
            "--self-contained",
            os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "SizeOnDisk", "SizeOnDisk.csproj"),
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"]).run()
        
        # build MemoryConsumption
        RunCommand([
            dotnet_path, "publish", 
            "-c", "Release", 
            "-o", os.path.join(performance_setup_data.payload_directory, "MemoryConsumption"),
            "-f", framework,
            "-r", runtime_id,
            "--self-contained",
            os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "MemoryConsumption", "MemoryConsumption.csproj"),
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"]).run()
        
        # download PDN
        escaped_upload_token = str(os.environ.get("PerfCommandUploadTokenLinux")).replace("%25", "%")
        pdn_url = f"https://pvscmdupload.blob.core.windows.net/assets/paint.net.5.0.3.portable.{args.architecture}.zip{escaped_upload_token}"
        pdn_dest = os.path.join(performance_setup_data.payload_directory, "PDN")
        os.makedirs(pdn_dest)
        with urllib.request.urlopen(pdn_url) as response, open(os.path.join(pdn_dest, "PDN.zip"), "wb") as f:
            data = response.read()
            f.write(data)

        environ_copy = os.environ.copy()

        python = "py -3" if iswin() else "python3"
        os.environ["CorrelationPayloadDirectory"] = performance_setup_data.payload_directory
        os.environ["Architecture"] = args.architecture
        os.environ["TargetsWindows"] = "true" if args.os_group == "windows" else "false"
        os.environ["WorkItemDirectory"] = args.performance_repo_dir
        os.environ["HelixTargetQueues"] = args.queue
        os.environ["Python"] = python

        RunCommand([*(python.split(" ")), "-m", "pip", "install", "--upgrade", "pip"]).run()
        RunCommand([*(python.split(" ")), "-m", "pip", "install", "urllib3==1.26.18"]).run()
        RunCommand([*(python.split(" ")), "-m", "pip", "install", "requests"]).run()

        scenarios_path = os.path.join(args.performance_repo_dir, "src", "scenarios")
        script_path = os.path.join(args.performance_repo_dir, "scripts")
        os.environ["PYTHONPATH"] = f"{os.environ.get('PYTHONPATH', '')}{os.pathsep}{script_path}{os.pathsep}{scenarios_path}"
        print(f"PYTHONPATH={os.environ['PYTHONPATH']}")

        os.environ["DOTNET_ROOT"] = setup_arguments.install_dir
        os.environ["PATH"] = f"{setup_arguments.install_dir}{os.pathsep}{os.environ['PATH']}"
        os.environ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        os.environ["DOTNET_MULTILEVEL_LOOKUP"] = "0"
        os.environ["UseSharedCompilation"] = "false"

        print("Current dotnet directory:", setup_arguments.install_dir)
        print("If more than one version exist in this directory, usually the latest runtime and sdk will be used.")

        RunCommand([
            "dotnet", "msbuild", args.project_file, 
            "/restore", 
            "/t:PreparePayloadWorkItems",
            f"/p:RuntimeFlavor={args.runtime_flavor or ''}"
            f"/bl:{os.path.join(args.performance_repo_dir, 'artifacts', 'log', performance_setup_data.build_config, 'PrepareWorkItemPayloads.binlog')}"],
            verbose=True).run()
        
        if args.os_group == "windows" and args.architecture == "arm64":
            RunCommand(["taskkill", "/im", "dotnet.exe", "/f"]).run()
            RunCommand(["del", os.path.join(setup_arguments.install_dir, "*"), "/F", "/S", "/Q"]).run()
            RunCommand(["xcopy", os.path.join(args.performance_repo_dir, "tools", "dotnet", "arm64", "*"), "/E", "/I", "/Y"]).run()

        # restore env vars
        os.environ.update(environ_copy)

        performance_setup_data.work_item_directory = args.performance_repo_dir

        # TODO: Support WASM from runtime repository
        
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

    os.environ["DownloadFilesFromHelix"] = "true"
    perf_send_to_helix_args = PerfSendToHelixArgs(
        helix_source=f"{performance_setup_data.helix_source_prefix}/{args.build_repository_name}/{args.build_source_branch}",
        helix_type=f"test/performance/{performance_setup_data.kind}/{args.framework}/{performance_setup_data.architecture}/{args.helix_type_suffix}",
        helix_access_token=args.helix_access_token,
        helix_target_queues=[performance_setup_data.queue],
        helix_pre_commands=helix_pre_commands,
        helix_post_commands=helix_post_commands,
        creator=performance_setup_data.creator,
        architecture=args.architecture,
        work_item_timeout=timedelta(hours=4),
        work_item_dir=performance_setup_data.work_item_directory,
        correlation_payload_dir=performance_setup_data.payload_directory,
        project_file=args.project_file,
        build_config=performance_setup_data.build_config,
        performance_repo_dir=args.performance_repo_dir,
        system_access_token=args.system_access_token,
        helix_build=args.build_number,
        dotnet_cli_package_type="",
        dotnet_cli_version="",
        enable_xunit_reporter=False,
        helix_prereq_commands=[],
        include_dotnet_cli=False,
        wait_for_work_item_completion=True,
        partition_count=args.partition_count,
        cli_arguments=cli_arguments,
        runtime_flavor=args.runtime_flavor or ""
    )

    perf_send_to_helix(perf_send_to_helix_args)

    results_glob = os.path.join(performance_setup_data.payload_directory, "performance", "artifacts", "helix-results", '**', '*perf-lab-report.json')
    all_results: list[Any] = []
    for result_file in glob(results_glob, recursive=True):
        with open(result_file, 'r', encoding="utf8") as report_file:
            all_results.extend(json.load(report_file))

    output_counters_for_crank(all_results)


def main(argv: list[str]):
    args: dict[str, Any] = {}

    i = 1
    while i < len(argv):
        key = argv[i]
        bool_args = {
            "--internal": "internal",
            "--is-scenario": "is_scenario",
            "--local-build": "local_build",
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
            "--performance-repo-dir": "performance_repo_dir",
            "--perflab-upload-token": "perflab_upload_token",
            "--helix-access-token": "helix_access_token",
            "--system-access-token": "system_access_token",
            "--project-file": "project_file",
            "--helix-type-suffix": "helix_type_suffix",
            "--build-repository-name": "build_repository_name",
            "--build-source-branch": "build_source_branch",
            "--build-number": "build_number",
            "--pgo-run-type": "pgo_run_type",
            "--codegen-type": "codegen_type",
            "--runtime-type": "runtime_type",
            "--run-categories": "run_categories",
            "--extra-bdn-args": "extra_bdn_args",
            "--affinity": "affinity",
            "--os-group": "os_group",
            "--os-sub-group": "os_sub_group",
            "--runtime-flavor": "runtime_flavor"
        }

        if key in simple_arg_map:
            arg_name = simple_arg_map[key]
            val = argv[i + 1]
        elif key == "--partition-count":
            arg_name = "partition_count"
            val = int(argv[i + 1])
        else:
            raise Exception(f"Invalid argument: {key}")

        args[arg_name] = val
        i += 2

        # TODO: support additional_performance_setup_parameters and additional_ci_setup_parameters

    run_performance_job(RunPerformanceJobArgs(**args))

if __name__ == "__main__":
    main(sys.argv)