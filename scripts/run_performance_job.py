from dataclasses import dataclass, field
import dataclasses
from datetime import timedelta
import os
import shutil
import sys
from typing import Any
import ci_setup
from performance.common import RunCommand
import performance_setup
from send_to_helix import PerfSendToHelixArgs, perf_send_to_helix

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
    physical_promotion_run_type: bool = False
    codegen_type: str = "JIT"
    runtime_type: str = "coreclr"
    affinity: str | None = "0"
    run_env_vars: dict[str, str] = field(default_factory=dict[str, str])
    is_scenario: bool = False

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

    artifacts_directory = os.path.join(args.performance_repo_dir, "artifacts")

    if args.is_scenario:
        if args.os_group == "windows":
            helix_pre_commands = [
                "set ORIGPYPATH=%PYTHONPATH%",
                "py -3 -m venv %HELIX_WORKITEM_PAYLOAD%\\.venv",
                "call %HELIX_WORKITEM_PAYLOAD%\\.venv\\Scripts\\activate.bat",
                "set PYTHONPATH=",
                "py -3 -m pip install -U pip",
                "py -3 -m pip install --user urllib3==1.26.15 --force-reinstall",
                "py -3 -m pip install --user azure.storage.blob==12.0.0 --force-reinstall",
                "py -3 -m pip install --user azure.storage.queue==12.0.0 --force-reinstall",
                f'set "PERFLAB_UPLOAD_TOKEN={args.perflab_upload_token}"'
            ]
        elif args.os_group == "osx":
            helix_pre_commands = [
                "export ORIGPYPATH=$PYTHONPATH",
                "export CRYPTOGRAPHY_ALLOW_OPENSSL_102=true",
                "python3 -m venv $HELIX_WORKITEM_PAYLOAD/.venv",
                "source $HELIX_WORKITEM_PAYLOAD/.venv/bin/activate",
                "export PYTHONPATH=",
                "python3 -m pip install -U pip",
                "pip3 install --user urllib3==1.26.15 --force-reinstall",
                "pip3 install --user azure.storage.blob==12.7.1 --force-reinstall",
                "pip3 install --user azure.storage.queue==12.1.5 --force-reinstall",
                f'export PERFLAB_UPLOAD_TOKEN="{args.perflab_upload_token}"'
            ]
        elif args.os_sub_group == "_musl":
            helix_pre_commands = [
                "export ORIGPYPATH=$PYTHONPATH",
                "sudo apk add py3-virtualenv",
                "python3 -m venv $HELIX_WORKITEM_PAYLOAD/.venv",
                "source $HELIX_WORKITEM_PAYLOAD/.venv/bin/activate",
                "export PYTHONPATH=",
                "python3 -m pip install -U pip",
                "pip3 install --user urllib3==1.26.15 --force-reinstall",
                "pip3 install --user azure.storage.blob==12.7.1 --force-reinstall",
                "pip3 install --user azure.storage.queue==12.1.5 --force-reinstall",
                f'export PERFLAB_UPLOAD_TOKEN="{args.perflab_upload_token}"'
            ]
        else:
            helix_pre_commands = [
                "export ORIGPYPATH=$PYTHONPATH",
                "export CRYPTOGRAPHY_ALLOW_OPENSSL_102=true",
                "sudo apt-get -y install python3-venv",
                "python3 -m venv $HELIX_WORKITEM_PAYLOAD/.venv",
                "source $HELIX_WORKITEM_PAYLOAD/.venv/bin/activate",
                "export PYTHONPATH=",
                "python3 -m pip install -U pip",
                "pip3 install --user urllib3==1.26.15 --force-reinstall",
                "pip3 install --user azure.storage.blob==12.7.1 --force-reinstall",
                "pip3 install --user azure.storage.queue==12.1.5 --force-reinstall",
                f'export PERFLAB_UPLOAD_TOKEN="{args.perflab_upload_token}"'
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
                "py -3 -m pip install --user urllib3==1.26.15 --force-reinstall",
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
                "pip3 install --user urllib3==1.26.15 --force-reinstall",
                "pip3 install --user azure.storage.blob==12.7.1 --force-reinstall",
                "pip3 install --user azure.storage.queue==12.1.5 --force-reinstall",
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
                "pip3 install --user urllib3==1.26.15 && "
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
        physical_promotion=args.physical_promotion_run_type,
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

    if args.affinity != "0":
        setup_arguments.affinity = args.affinity

    if args.run_env_vars:
        setup_arguments.run_env_vars = [f"{k}={v}" for k, v in args.run_env_vars.items()]

    setup_arguments.target_windows = args.os_group == "windows"
    if args.os_group == "windows":
        os.environ["TargetsWindows"] = "true"

    if args.is_scenario:
        if args.runtime_type != "wasm":
            setup_arguments.install_dir = os.path.join(performance_setup_data.payload_directory, "dotnet")

        setup_arguments.output_file = os.path.join(performance_setup_data.work_item_directory, "machine-setup")
    else:
        tools_dir = os.path.join(performance_setup_data.performance_directory, "tools")
        setup_arguments.output_file = os.path.join(tools_dir, "machine-setup")
        setup_arguments.install_dir = os.path.join(tools_dir, "dotnet", performance_setup_data.architecture)

    ci_setup.main(setup_arguments)

    if args.is_scenario:
        if args.runtime_type == "wasm":
            os.makedirs(os.path.join(artifacts_directory, "bin", "wasm", "data"))
            shutil.copytree(os.path.join(artifacts_directory, "BrowserWasm", "staging", "dotnet-latest"), os.path.join(artifacts_directory, "bin", "wasm"))
            shutil.copytree(os.path.join(artifacts_directory, "BrowserWasm", "staging", "built-nugets"), os.path.join(artifacts_directory, "bin", "wasm"))
            # TODO: Add test-main.js as a source for wasm runs so that it gets uploaded to the agent
            # cp src/mono/wasm/test-main.js $(librariesDownloadDir)/bin/wasm/data/test-main.js &&
            # find $(librariesDownloadDir)/bin/wasm -type f -exec chmod 664 {} \;

        # copy scenario support files
        shutil.copytree(os.path.join(args.performance_repo_dir, "scripts"), os.path.join(performance_setup_data.work_item_directory, "scripts"))
        shutil.copytree(os.path.join(args.performance_repo_dir, "src", "scenarios", "shared"), os.path.join(performance_setup_data.work_item_directory, "shared"))
        shutil.copytree(os.path.join(args.performance_repo_dir, "src", "scenarios", "staticdeps"), os.path.join(performance_setup_data.work_item_directory, "staticdeps"))

        os.environ["PERFLAB_TARGET_FRAMEWORKS"] = "net7.0"
        if args.os_group == "windows":
            runtime_id = f"win-{args.architecture}"
        elif args.os_group == "osx":
            runtime_id = f"osx-{args.architecture}"
        else:
            runtime_id = f"linux-{args.architecture}"

        # build Startup
        RunCommand([
            "dotnet", "publish", 
            "-c", "Release", 
            "-o", os.path.join(performance_setup_data.work_item_directory, "startup"),
            "-f", "net7.0",
            "-r", runtime_id,
            os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "Startup", "Startup.csproj"),
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"])

        # build SizeOnDisk
        RunCommand([
            "dotnet", "publish", 
            "-c", "Release", 
            "-o", os.path.join(performance_setup_data.work_item_directory, "SOD"),
            "-f", "net7.0",
            "-r", runtime_id,
            os.path.join(args.performance_repo_dir, "src", "tools", "ScenarioMeasurement", "SizeOnDisk", "SizeOnDisk.csproj"),
            "-p:DisableTransitiveFrameworkReferenceDownloads=true"])
        
        # zip work item directory
        if args.run_kind == "android_scenarios" or args.run_kind == "ios_scenarios":
            shutil.make_archive(performance_setup_data.work_item_directory, "zip", performance_setup_data.work_item_directory)

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
        runtime_flavor=''
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
            "--is-scenario": "is_scenario"
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
            "--affinity": "affinity"
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