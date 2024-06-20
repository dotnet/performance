from typing import List, Optional, Union
from dataclasses import dataclass, field
from datetime import timedelta
import os
from performance.common import RunCommand, iswin, set_environment_variable

@dataclass
class PerfSendToHelixArgs:
    """
    The Helix SDK receives all its arguments via environment variables. This class makes it easy to see all the 
    possible arguments that can be set and set_environment_variables will ensure that they are set into the right 
    environment variables. For CI runs, it will also ensure that the environment variables are propagated to the agent 
    so that it can be used in conjunction with the send-to-helix.yml file instead of invoking the Helix SDK from 
    perf_send_to_helix defined inside this python file.

    Docs can be found here: https://github.com/dotnet/arcade/blob/main/Documentation/AzureDevOps/SendingJobsToHelix.md
    """
    # Used by this python script
    project_file: str
    performance_repo_dir: str

    # Required for Helix SDK
    helix_source: str = "pr/default"
    helix_type: str = "tests/default/"
    build_config: str = ""
    helix_build: str = os.environ.get("BUILD_BUILDNUMBER", "")
    helix_target_queues: List[str] = field(default_factory=list) # type: ignore

    # Environment variables that need to be set
    env_build_reason: str = os.environ.get("BUILD_REASON", "pr")
    env_build_repository: str = os.environ.get("BUILD_REPOSITORY_NAME", "dotnet/performance")
    env_build_source_branch: str = os.environ.get("BUILD_SOURCEBRANCH", "main")
    env_system_team_project: str = os.environ.get("SYSTEM_TEAMPROJECT", "internal")
    env_system_access_token: str = os.environ.get("SYSTEM_ACCESSTOKEN", "")

    # Optional for Helix SDK
    helix_access_token: Optional[str] = None
    helix_pre_commands: List[str] = field(default_factory=list) # type: ignore
    helix_post_commands: List[str] = field(default_factory=list) # type: ignore
    include_dotnet_cli: bool = False
    dotnet_cli_package_type: str = ""
    dotnet_cli_version: str = ""
    enable_xunit_reporter: bool = False
    wait_for_work_item_completion: bool = True
    creator: str = ""
    helix_results_destination_dir : Optional[str] = None
    fail_on_test_failure: bool = False

    # Used by our custom .proj files
    work_item_dir: str = ""
    architecture: str = "x64"
    work_item_timeout: timedelta = timedelta(hours=4)
    correlation_payload_dir: str = ""
    target_csproj: str = ""
    download_files_from_helix: bool = False
    targets_windows: bool = True

    # Used by BDN projects
    work_item_command: Optional[List[str]] = None
    baseline_work_item_command: Optional[List[str]] = None
    partition_count: Optional[int] = None
    bdn_arguments: Optional[List[str]] = None
    baseline_bdn_arguments: Optional[List[str]] = None
    compare: bool = False
    compare_command: Optional[List[str]] = None
    only_sanity_check: bool = False

    # Used by scenarios projects
    runtime_flavor: Optional[str] = None
    hybrid_globalization: Optional[bool] = None
    python: Optional[str] = None
    affinity: Optional[str] = None
    ios_strip_symbols: Optional[bool] = None
    ios_llvm_build: Optional[bool] = None

    def set_environment_variables(self, save_to_pipeline: bool = True):
        def set_env_var(name: str, value: Union[str, bool, List[str], timedelta, int, None], sep = " ", save_to_pipeline=save_to_pipeline):
            if value is None:
                # None means don't set it
                return
            elif isinstance(value, str):
                value_str = value
            elif isinstance(value, bool):
                value_str = "true" if value else "false"
            elif isinstance(value, timedelta) or isinstance(value, int):
                value_str = str(value)
            else:
                value_str = sep.join(value)
            set_environment_variable(name, value_str, save_to_pipeline=save_to_pipeline)
        set_env_var("Architecture", self.architecture)
        set_env_var("BuildConfig", self.build_config)
        set_env_var("HelixSource", self.helix_source)
        set_env_var("HelixType", self.helix_type)
        set_env_var("HelixBuild", self.helix_build)
        set_env_var("HelixTargetQueues", self.helix_target_queues, sep=";")
        set_env_var("HelixAccessToken", self.helix_access_token)
        set_env_var("HelixPreCommands", self.helix_pre_commands, sep=";")
        set_env_var("HelixPostCommands", self.helix_post_commands, sep=";")
        set_env_var("WorkItemDirectory", self.work_item_dir)
        set_env_var("WorkItemTimeout", self.work_item_timeout)
        set_env_var("CorrelationPayloadDirectory", self.correlation_payload_dir)
        set_env_var("IncludeDotNetCli", self.include_dotnet_cli)
        set_env_var("DotNetCliPackageType", self.dotnet_cli_package_type)
        set_env_var("DotNetCliVersion", self.dotnet_cli_version)
        set_env_var("EnableXUnitReporter", self.enable_xunit_reporter)
        set_env_var("WaitForWorkItemCompletion", self.wait_for_work_item_completion)
        set_env_var("Creator", self.creator)
        set_env_var("PartitionCount", self.partition_count)
        set_env_var("RuntimeFlavor", self.runtime_flavor)
        set_env_var("HybridGlobalization", self.hybrid_globalization)
        set_env_var("iOSStripSymbols", self.ios_strip_symbols)
        set_env_var("iOSLlvmBuild", self.ios_llvm_build)
        set_env_var("TargetCsproj", self.target_csproj)
        set_env_var("WorkItemCommand", self.work_item_command, sep=" ")
        set_env_var("BaselineWorkItemCommand", self.baseline_work_item_command, sep=" ")
        set_env_var("CompareCommand", self.compare_command, sep=" ")
        set_env_var("BenchmarkDotNetArguments", self.bdn_arguments, sep=" ")
        set_env_var("BaselineBenchmarkDotNetArguments", self.baseline_bdn_arguments, sep=" ")
        set_env_var("DownloadFilesFromHelix", self.download_files_from_helix)
        set_env_var("TargetsWindows", self.targets_windows)
        set_env_var("HelixResultsDestinationDir", self.helix_results_destination_dir)
        set_env_var("Python", self.python)
        set_env_var("AffinityValue", self.affinity)
        set_env_var("Compare", self.compare)
        set_env_var("FailOnTestFailure", self.fail_on_test_failure)
        set_env_var("OnlySanityCheck", self.only_sanity_check)

        # The following will already be set in the CI pipeline, but are required to run Helix locally
        set_env_var("BUILD_REASON", self.env_build_reason, save_to_pipeline=False)
        set_env_var("BUILD_REPOSITORY_NAME", self.env_build_repository, save_to_pipeline=False)
        set_env_var("BUILD_SOURCEBRANCH", self.env_build_source_branch, save_to_pipeline=False)
        set_env_var("SYSTEM_TEAMPROJECT", self.env_system_team_project, save_to_pipeline=False)
        set_env_var("SYSTEM_ACCESSTOKEN", self.env_system_access_token, save_to_pipeline=False)

def run_shell(script: str, args: List[str]):
    RunCommand(["chmod", "+x", script]).run()
    RunCommand([script, *args], verbose=True).run()

def run_powershell(script: str, args: List[str]):
    RunCommand(["powershell.exe", script, *args], verbose=True).run()

def perf_send_to_helix(args: PerfSendToHelixArgs):
    args.set_environment_variables(save_to_pipeline=False)

    binlog_dest = os.path.join(args.performance_repo_dir, "artifacts", "log", args.build_config, "SendToHelix.binlog")
    send_params = [args.project_file, "/restore", "/t:Test", f"/bl:{binlog_dest}"]

    common_dir = os.path.join(args.performance_repo_dir, "eng", "common")
    if iswin():
        run_powershell(os.path.join(common_dir, "msbuild.ps1"), ["-warnaserror", "0", *send_params])
    else:
        run_shell(os.path.join(common_dir, "msbuild.sh"), ["--warnaserror", "false", *send_params])

