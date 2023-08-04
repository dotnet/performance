from dataclasses import dataclass
from datetime import timedelta
import os
from performance.common import RunCommand, iswin

@dataclass
class PerfSendToHelixArgs:
    project_file: str
    architecture: str
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
    runtime_flavor: str

    performance_repo_dir: str
    build_config: str
    system_access_token: str

def run_shell(script: str, args: list[str]):
    RunCommand(["chmod", "+x", script]).run()
    RunCommand([script, *args], verbose=True).run()

def run_powershell(script: str, args: list[str]):
    RunCommand(["powershell.exe", script, *args], verbose=True).run()

def perf_send_to_helix(args: PerfSendToHelixArgs):
    os.environ["Architecture"] = args.architecture
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
    send_params = [args.project_file, "/restore", "/t:Test", f"/p:RuntimeFlavor={args.runtime_flavor}", f"/bl:{binlog_dest}"]

    common_dir = os.path.join(args.performance_repo_dir, "eng", "common")
    if iswin():
        run_powershell(os.path.join(common_dir, "msbuild.ps1"), ["-warnaserror", "0", *send_params])
    else:
        run_shell(os.path.join(common_dir, "msbuild.sh"), send_params)

