from dataclasses import dataclass, field
import dataclasses
from datetime import timedelta
import sys
import os
from typing import Any
import ci_setup
from performance.common import RunCommand, iswin
import performance_setup

@dataclass
class PerfSendToHelixArgs:
    project_file: str
    helix_build: str
    helix_source: str
    helix_type: str
    helix_target_queues: list[str]
    helix_access_token: str
    helix_pre_commands: list[str]
    helix_post_commands: list[str]
    work_item_dir: str
    work_item_timeout: timedelta
    correlation_payload_dir: str
    include_dotnet_cli: bool
    dotnet_cli_package_type: str
    dotnet_cli_version: str
    enable_xunit_reporter: bool
    wait_for_work_item_completion: bool
    creator: str
    helix_prereq_commands: list[str]
    partition_count: int
    cli_arguments: list[str]

    performance_repo_dir: str
    build_config: str
    system_access_token: str

def run_shell(script: str, args: list[str]):
    RunCommand([script, *args], verbose=True).run()

def run_powershell(script: str, args: list[str]):
    RunCommand(["powershell.exe", script, *args], verbose=True).run()

def perf_send_to_helix(args: PerfSendToHelixArgs):
    os.environ["BuildConfig"] = args.build_config
    os.environ["HelixSource"] = args.helix_source
    os.environ["HelixType"] = args.helix_type
    os.environ["HelixBuild"] = args.helix_build
    os.environ["HelixTargetQueues"] = ";".join(args.helix_target_queues)
    os.environ["HelixAccessToken"] = args.helix_access_token
    os.environ["HelixPreCommands"] = ";".join(args.helix_pre_commands)
    os.environ["HelixPostCommands"] = ";".join(args.helix_post_commands)
    os.environ["HelixPrereqCommands"] = ";".join(args.helix_prereq_commands)
    os.environ["WorkItemDirectory"] = args.work_item_dir
    os.environ["WorkItemTimeout"] = str(args.work_item_timeout)
    os.environ["CorrelationPayloadDirectory"] = args.correlation_payload_dir
    os.environ["IncludeDotNetCli"] = str(args.include_dotnet_cli)
    os.environ["DotNetCliPackageType"] = args.dotnet_cli_package_type
    os.environ["DotNetCliVersion"] = args.dotnet_cli_version
    os.environ["EnableXUnitReporter"] = str(args.enable_xunit_reporter)
    os.environ["WaitForWorkItemCompletion"] = str(args.wait_for_work_item_completion)
    os.environ["Creator"] = str(args.creator)
    os.environ["SYSTEM_ACCESSTOKEN"] = args.system_access_token
    os.environ["PartitionCount"] = str(args.partition_count)
    os.environ["CliArguments"] = " ".join(args.cli_arguments)

    binlog_dest = os.path.join(args.performance_repo_dir, "artifacts", "log", args.build_config, "SendToHelix.binlog")
    send_params = [args.project_file, "/restore", "/t:Test", f"/bl:{binlog_dest}"]

    common_dir = os.path.join(args.performance_repo_dir, "eng", "common")
    if iswin():
        run_powershell(os.path.join(common_dir, "msbuild.ps1"), ["-warnaserror", "0", *send_params])
    else:
        run_shell(os.path.join(common_dir, "msbuild.sh"), send_params)

@dataclass
class RunPerformanceJobArgs:
    queue: str
    framework: str
    run_kind: str
    core_root_dir: str
    performance_repo_dir: str
    architecture: str
    
    extra_bdn_args: str | None = None
    run_categories: str = 'runtime libraries'
    perflab_upload_token: str | None = None
    helix_access_token: str | None = os.environ.get("HelixAccessToken")
    system_access_token: str = os.environ.get("SYSTEM_ACCESSTOKEN", "")
    targets_windows: bool = False
    targets_musl: bool = False
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
    codegen_type: str = "JIT"
    runtime_type: str = "coreclr"

def run_performance_job(args: RunPerformanceJobArgs):
    if args.project_file is None:
        args.project_file = os.path.join(args.performance_repo_dir, "eng", "performance", "helix.proj")

    if args.helix_access_token is None:
        raise Exception("HelixAccessToken environment variable is not configured")
    
    if args.perflab_upload_token is None:
        if args.targets_windows:
            args.perflab_upload_token = os.environ.get("PerfCommandUploadToken")
        else:
            args.perflab_upload_token = os.environ.get("PerfCommandUploadTokenLinux")
    
    helix_post_commands: list[str] = []

    if args.targets_windows:
        helix_pre_commands = [
            "set ORIGPYPATH=%PYTHONPATH%",
            "py -m pip install -U pip",
            "py -3 -m venv %HELIX_WORKITEM_PAYLOAD%\\.venv",
            "call %HELIX_WORKITEM_PAYLOAD%\\.venv\\Scripts\\activate.bat",
            "set PYTHONPATH=;py -3 -m pip install -U pip",
            "py -3 -m pip install urllib3==1.26.15",
            "py -3 -m pip install azure.storage.blob==12.0.0",
            "py -3 -m pip install azure.storage.queue==12.0.0",
            f'set "PERFLAB_UPLOAD_TOKEN={args.perflab_upload_token}"'
        ]
    elif args.targets_musl:
        helix_pre_commands = [
            "export ORIGPYPATH=$PYTHONPATH",
            "sudo apk add icu-libs krb5-libs libgcc libintl libssl1.1 libstdc++ zlib cargo",
            "sudo apk add libgdiplus --repository http://dl-cdn.alpinelinux.org/alpine/edge/testing",
            "python3 -m venv $HELIX_WORKITEM_PAYLOAD/.venv",
            "source $HELIX_WORKITEM_PAYLOAD/.venv/bin/activate",
            "export PYTHONPATH=",
            "python3 -m pip install -U pip",
            "pip3 install urllib3==1.26.15",
            "pip3 install azure.storage.blob==12.7.1",
            "pip3 install azure.storage.queue==12.1.5",
            f'export PERFLAB_UPLOAD_TOKEN="{args.perflab_upload_token}"'
        ]
    else:
        if args.runtime_type == "wasm":
            wasm_precommand = (
                "sudo apt-get -y remove nodejs && "
                "curl -fsSL https://deb.nodesource.com/setup_14.x | sudo -E bash - && "
                "sudo apt-get -y install nodejs && "
                "npm install --prefix $HELIX_WORKITEM_PAYLOAD jsvu -g && "
                "$HELIX_WORKITEM_PAYLOAD/bin/jsvu --os=linux64 --engines=v8 && "
                "find ~/.jsvu -ls && "
                "~/.jsvu/v8 -e \"console.log(`V8 version: ${this.version()}`)\"")
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
             "pip3 install urllib3==1.26.15 && "
             "pip3 install --user azure.storage.blob==12.0.0 && "
             "pip3 install --user azure.storage.queue==12.0.0 && "
             "sudo apt-get update && "
             "sudo apt -y install curl dirmngr apt-transport-https lsb-release ca-certificates && "
             f"{wasm_precommand} && "
             f"export PERFLAB_UPLOAD_TOKEN=\"{args.perflab_upload_token}\" "
             "|| export PERF_PREREQS_INSTALL_FAILED=1"),
           'test "x$PERF_PREREQS_INSTALL_FAILED" = "x1" && echo "** Error: Failed to install prerequites **'
        ]

    mono_interpreter = False
    if args.codegen_type == "Interpreter" and args.runtime_type == "mono":
        mono_interpreter = True
        if args.targets_windows:
            helix_pre_commands += ['set MONO_ENV_OPTIONS="--interpreter"']
        else:
            helix_pre_commands += ['export MONO_ENV_OPTIONS="--interpreter"']

    if args.targets_windows:
        helix_pre_commands += ["set MSBUILDDEBUGCOMM=1", 'set "MSBUILDDEBUGPATH=%HELIX_WORKITEM_UPLOAD_ROOT%"']
        helix_post_commands = ["set PYTHONPATH=%ORIGPYPATH%"]
    else:
        helix_pre_commands += ["export MSBUILDDEBUGCOMM=1", 'export "MSBUILDDEBUGPATH=$HELIX_WORKITEM_UPLOAD_ROOT"']
        helix_post_commands = ["export PYTHONPATH=$ORIGPYPATH"]

    # TODO: Support custom helix log collection in post command

    working_directory = args.performance_repo_dir
    performance_setup_args = performance_setup.PerformanceSetupArgs(
        performance_directory=args.performance_repo_dir,
        core_root_directory=args.core_root_dir,
        working_directory=working_directory,
        queue=args.queue,
        kind=args.run_kind,
        no_pgo=args.pgo_run_type == "nopgo",
        dynamic_pgo=args.pgo_run_type == "dynamicpgo",
        full_pgo=args.pgo_run_type == "fullpgo",
        internal=args.internal,
        mono_interpreter=mono_interpreter,
        framework=args.framework,
        use_local_commit_time=False,
        run_categories=args.run_categories,
        extra_bdn_args=[] if args.extra_bdn_args is None else args.extra_bdn_args.split(" "),
        **args.additional_performance_setup_parameters
    )

    performance_setup_data = performance_setup.run(performance_setup_args)
    performance_setup_data.set_environment_variables(save_to_pipeline=False)

    setup_arguments = dataclasses.replace(performance_setup_data.setup_arguments, **args.additional_ci_setup_parameters)

    setup_arguments.target_windows = args.targets_windows
    if args.targets_windows:
        os.environ["TargetsWindows"] = "true"
    
    tools_dir = os.path.join(performance_setup_data.performance_directory, "tools")
    setup_arguments.output_file = os.path.join(tools_dir, "machine-setup")
    # setup_arguments.install_dir = os.path.join(tools_dir, "dotnet", performance_setup_data.architecture)
    ci_setup.main(setup_arguments)

    if args.targets_windows:
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

    perf_send_to_helix_args = PerfSendToHelixArgs(
        helix_source=f"{performance_setup_data.helix_source_prefix}/{args.build_repository_name}/{args.build_source_branch}",
        helix_type=f"test/performance/{performance_setup_data.kind}/{args.framework}/{performance_setup_data.architecture}/{args.helix_type_suffix}",
        helix_access_token=args.helix_access_token,
        helix_target_queues=[performance_setup_data.queue],
        helix_pre_commands=helix_pre_commands,
        helix_post_commands=helix_post_commands,
        creator=performance_setup_data.creator,
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
        cli_arguments=cli_arguments
    )

    perf_send_to_helix(perf_send_to_helix_args)

def main(argv: list[str]):
    args: dict[str, Any] = {}

    i = 1
    while i < len(argv):
        key = argv[i]
        bool_args = {
            "--targets-windows": "targets_windows",
            "--targets-musl": "targets_musl",
            "--internal": "internal",
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