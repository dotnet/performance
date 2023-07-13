# This is a script for testing the performance of the different dotnet/runtime build types locally
# Example usage from the performance/scripts folder: 
# python .\benchmarks_local.py --local-test-repo "<absolute path to runtime folder>/runtime" --run-type MonoJIT --filter *Span.IndexerBench.CoveredIndex2* --bdn-arguments='-i'
# or if you want remotes:
# python .\benchmarks_local.py --branches main --repo-storage-dir "<absolute path to where you want to store runtime clones>" --run-type MonoJIT --filter *Span.IndexerBench.CoveredIndex2* --bdn-arguments='-i'

# The general flow as it stands is:
# * For each commit single or pair value specified:
#   * Get the repo to the proper commit
#   * Build the dependencies for the run types specified and copy them to the artifact storage path
# * Run the benchmarks:
#   * For each run type specified with the artifacts all passed as coreruns to take advantage of BDN's comparisons
# * Adding a new run type:
#   * Add the run type to the RunType enum
#   * Add the build instructions to the generate_all_runtime_artifacts function
#   * Add the BDN run arguments to the generate_benchmark_ci_args function


from argparse import ArgumentParser, ArgumentTypeError, Namespace
from channel_map import ChannelMap
from enum import Enum
from logging import getLogger
from git.repo import Repo
from git import GitCommandError
from performance.common import get_machine_architecture, RunCommand
from performance.logger import setup_loggers
from subprocess import CalledProcessError

import benchmarks_ci
from datetime import datetime
import platform
import shutil
import sys
import os


# Assumptions: We are only testing this Performance repo, should allow single run or multiple runs
# For dotnet_version based runs, use the benchmarks_monthly .py script instead
# Verify the input commands
# What are supported default cases: MonoJIT, MonoAOTLLVM, MonoInter, Corerun, etc. (WASM)
class RunType(Enum):
    CoreRun = 1
    MonoAOTLLVM = 2
    MonoInterpreter = 3
    MonoJIT = 4

start_time = datetime.now()

def kill_dotnet_processes():
    if platform == 'win32':
        os.system('TASKKILL /F /T /IM dotnet.exe 2> nul || TASKKILL /F /T /IM VSTest.Console.exe 2> nul || TASKKILL /F /T /IM msbuild.exe 2> nul || TASKKILL /F /T /IM ".NET Host" 2> nul')
    else:
        os.system('killall -9 dotnet 2> /dev/null || killall -9 VSTest.Console 2> /dev/null || killall -9 msbuild 2> /dev/null || killall -9 ".NET Host" 2> /dev/null') # Always kill dotnet so it isn't left with handles on its files

def enum_name_to_enum(EnumType, enum_name: str):
    for enum in EnumType:
        if enum.name == enum_name:
            return enum
    raise ValueError(f"Enum name {enum_name} not found in {EnumType}.")

def enum_name_list_to_enum_list(EnumType, enum_name_list: list):
    return [enum_name_to_enum(EnumType, enum_name) for enum_name in enum_name_list]

def check_for_runtype_specified(parsed_args: Namespace, run_types_to_check: list) -> bool:
    for run_type in run_types_to_check:
        if run_type.name in parsed_args.run_type_names:
            return True
    return False

# Uses python copy to copy the contents of a directory to another directory while overwriting any existing files
def copy_directory_contents(src_dir: str, dest_dir: str):
    for src_dirpath, src_dirnames, src_filenames in os.walk(src_dir):
        dest_dirpath = os.path.join(dest_dir, os.path.relpath(src_dirpath, src_dir))
        if not os.path.exists(dest_dirpath):
            os.makedirs(dest_dirpath)
        for src_filename in src_filenames:
            if os.path.exists(os.path.join(dest_dirpath, src_filename)) and os.path.samefile(os.path.join(src_dirpath, src_filename), os.path.join(dest_dirpath, src_filename)):
                continue
            shutil.copy2(os.path.join(src_dirpath, src_filename), dest_dirpath)
        
# Builds libs and corerun by default
def build_runtime_dependency(parsed_args: Namespace, repo_path: str, subset: str = "clr+libs", configuration: str = "Release", additional_args: list = []):    
    # Run the command
    if parsed_args.os == "windows":
        build_libs_and_corerun_command = [
                "pwsh",
                "-File",
                f"build.ps1"
        ]
    else:
        build_libs_and_corerun_command = [
                "bash",
                "build.sh"
        ]
    build_libs_and_corerun_command += [
                "-subset", subset, 
                "-configuration", configuration, 
                "-os", f"{parsed_args.os}",
                "-arch", f"{parsed_args.architecture}", 
                "-framework", f"{parsed_args.framework}",
                "-bl"
            ] + additional_args
    RunCommand(build_libs_and_corerun_command, verbose=True).run(os.path.join(repo_path, "eng"))

def generate_layout(parsed_args: Namespace, repo_path: str, additional_args: list = []):
    # Run the command
    if parsed_args.os == "windows":
        build_script = "build.cmd"
    else:
        build_script = "./build.sh"
    generate_layout_command = [
                f"{build_script}",
                "release",
                parsed_args.architecture,
                "generatelayoutonly",
                "/p:LibrariesConfiguration=Release"
            ] + additional_args
    RunCommand(generate_layout_command, verbose=True).run(os.path.join(repo_path, "src/tests"))

def get_run_artifact_path(parsed_args: Namespace, run_type: RunType, commit: str) -> str:
    return os.path.join(parsed_args.artifact_storage_path, f"{run_type.name}-{commit}-{parsed_args.os}-{parsed_args.architecture}-{parsed_args.framework}")

# Try to generate all of a single runs dependencies at once to save time
def generate_all_runtype_dependencies(parsed_args: Namespace, repo_path: str, commit: str, force_regenerate: bool = False):
    getLogger().info(f"Generating dependencies for {' '.join(map(str, parsed_args.run_type_names))} run types in {repo_path} and storing in {parsed_args.artifact_storage_path}.")
    
    if check_for_runtype_specified(parsed_args, [RunType.CoreRun]):
        dest_dir = os.path.join(get_run_artifact_path(parsed_args, RunType.CoreRun, commit), "Core_Root")
        
        if force_regenerate or not os.path.exists(dest_dir):
            build_runtime_dependency(parsed_args, repo_path)
            generate_layout(parsed_args, repo_path)
            # Store the corerun in the artifact storage path
            core_root_path = os.path.join(repo_path, "artifacts", "tests", "coreclr", f"{parsed_args.os}.{parsed_args.architecture}.Release", "Tests", "Core_Root")
            shutil.rmtree(dest_dir, ignore_errors=True)
            copy_directory_contents(core_root_path, dest_dir)
            shutil.rmtree(os.path.join(repo_path, "artifacts"), ignore_errors=True)
        else:
            getLogger().info(f"CoreRun already exists in {dest_dir}. Skipping generation.")

    if check_for_runtype_specified(parsed_args, [RunType.MonoInterpreter, RunType.MonoJIT]):
        dest_dir_mono_interpreter = os.path.join(get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit), "dotnet_mono")
        dest_dir_mono_jit = os.path.join(get_run_artifact_path(parsed_args, RunType.MonoJIT, commit), "dotnet_mono")

        if force_regenerate or not os.path.exists(dest_dir_mono_interpreter) or not os.path.exists(dest_dir_mono_jit):
            build_runtime_dependency(parsed_args, repo_path, "clr+mono+libs")
            build_runtime_dependency(parsed_args, repo_path, "libs.pretest", additional_args=['-testscope', 'innerloop', '/p:RuntimeFlavor=mono', f"/p:RuntimeArtifactsPath={os.path.join(repo_path, 'artifacts', 'bin', 'mono', f'{parsed_args.os}.{parsed_args.architecture}.Release')}"])

            # Create the mono-dotnet
            src_dir = os.path.join(repo_path, "artifacts", "bin", "runtime", f"net8.0-{parsed_args.os}-Release-{parsed_args.architecture}")
            dest_dir = os.path.join(repo_path, "artifacts", "bin", "testhost", f"net8.0-{parsed_args.os}-Release-{parsed_args.architecture}", "shared", "Microsoft.NETCore.App", "8.0.0")
            copy_directory_contents(src_dir, dest_dir)
            src_dir = os.path.join(repo_path, "artifacts", "bin", "testhost", f"net8.0-{parsed_args.os}-Release-{parsed_args.architecture}")
            dest_dir = os.path.join(repo_path, "artifacts", "dotnet_mono")
            copy_directory_contents(src_dir, dest_dir)
            src_file = os.path.join(repo_path, "artifacts", "bin", "coreclr", f"{parsed_args.os}.{parsed_args.architecture}.Release", f"corerun{'.exe' if parsed_args.os == 'windows' else ''}")
            dest_dir = os.path.join(repo_path, "artifacts", "dotnet_mono", "shared", "Microsoft.NETCore.App", "8.0.0")
            dest_file = os.path.join(dest_dir, f"corerun{'.exe' if parsed_args.os == 'windows' else ''}")
            if os.path.exists(dest_dir):
                shutil.rmtree(dest_dir, ignore_errors=True)
            os.makedirs(dest_dir)
            shutil.copy2(src_file, dest_file)

            # Store the dotnet_mono in the artifact storage path
            dotnet_mono_path = os.path.join(repo_path, "artifacts", "dotnet_mono")
            shutil.rmtree(dest_dir_mono_interpreter, ignore_errors=True)
            copy_directory_contents(dotnet_mono_path, dest_dir_mono_interpreter)
            shutil.rmtree(dest_dir_mono_jit, ignore_errors=True)
            copy_directory_contents(dotnet_mono_path, dest_dir_mono_jit)
            shutil.rmtree(os.path.join(repo_path, "artifacts"), ignore_errors=True)
        else:
            getLogger().info(f"dotnet_mono already exists in {dest_dir_mono_interpreter} and {dest_dir_mono_jit}. Skipping generation.")

    if check_for_runtype_specified(parsed_args, [RunType.MonoAOTLLVM]):
        raise NotImplementedError("MonoAOTLLVM is not yet implemented.")
        build_runtime_dependency(parsed_args, repo_path, "mono+libs+host+packs", additional_args=['/p:CrossBuild=false' '/p:MonoLLVMUseCxx11Abi=false'])
        # TODO: Finish MonoAOTLLVM Build stuff
        # Clean up the build results
        shutil.rmtree(os.path.join(repo_path, "artifacts"), ignore_errors=True) # TODO: Can we trust the build system to update these when necessary or do we need to clean them up ourselves?
    
    getLogger().info(f"Finished generating dependencies for {' '.join(map(str, parsed_args.run_type_names))} run types in {repo_path} and stored in {parsed_args.artifact_storage_path}.")

def generate_benchmark_ci_args(parsed_args: Namespace, specific_run_type: RunType, all_commits: list) -> list:
    getLogger().info(f"Generating benchmark_ci.py arguments for {specific_run_type.name} run type using artifacts in {parsed_args.artifact_storage_path}.")
    benchmark_ci_args = []
    bdn_args_unescaped = []
    benchmark_ci_args += ['--architecture', parsed_args.architecture]
    benchmark_ci_args += ['--frameworks', parsed_args.framework]
    benchmark_ci_args += ['--filter', parsed_args.filter]
    benchmark_ci_args += ['--csproj', parsed_args.csproj]
    benchmark_ci_args += ['--incremental', "no"]
    benchmark_ci_args += ['--bdn-artifacts', os.path.join(parsed_args.artifact_storage_path, f"BenchmarkDotNet.Artifacts.{specific_run_type.name}.{start_time.strftime('%y%m%d_%H%M%S')}")]

    if specific_run_type == RunType.CoreRun:
        bdn_args_unescaped += [
                                '--anyCategories', 'Libraries', 'Runtime',
                                '--logBuildOutput',
                                '--generateBinLog'
                            ]
        
        bdn_args_unescaped += [ '--corerun' ]
        for commit in all_commits:
            bdn_args_unescaped += [ os.path.join(get_run_artifact_path(parsed_args, RunType.CoreRun, commit), "Core_Root", f'corerun{".exe" if parsed_args.os == "windows" else ""}') ]

    elif specific_run_type == RunType.MonoAOTLLVM:
        raise NotImplementedError("MonoAOTLLVM is not yet implemented.")
    
    elif specific_run_type == RunType.MonoInterpreter:
        bdn_args_unescaped += [
                                '--anyCategories', 'Libraries', 'Runtime', 
                                '--category-exclusion-filter', 'NoInterpreter', 'NoMono',
                                '--logBuildOutput',
                                '--generateBinLog'
                            ]
        bdn_args_unescaped += [ '--corerun' ]
        for commit in all_commits:
            bdn_args_unescaped += [ os.path.join(get_run_artifact_path(parsed_args, RunType.MonoInterpreter, commit), "dotnet_mono", "shared", "Microsoft.NETCore.App", "8.0.0", f'corerun{".exe" if parsed_args.os == "windows" else ""}') ]
        
        bdn_args_unescaped += ['--envVars', 'MONO_ENV_OPTIONS:--interpreter']

    elif specific_run_type == RunType.MonoJIT:
        bdn_args_unescaped += [ 
                                '--anyCategories', 'Libraries', 'Runtime', 
                                '--category-exclusion-filter', 'NoInterpreter', 'NoMono',
                                '--logBuildOutput',
                                '--generateBinLog'
                            ]
        bdn_args_unescaped += [ '--corerun' ]
        for commit in all_commits:
            bdn_args_unescaped += [ os.path.join(get_run_artifact_path(parsed_args, RunType.MonoJIT, commit), "dotnet_mono", "shared", "Microsoft.NETCore.App", "8.0.0", f'corerun{".exe" if parsed_args.os == "windows" else ""}') ]

    if parsed_args.bdn_arguments:
        bdn_args_unescaped += [parsed_args.bdn_arguments]
    benchmark_ci_args += [f'--bdn-arguments={" ".join(bdn_args_unescaped)}']
    getLogger().info(f"Finished generating benchmark_ci.py arguments for {specific_run_type.name} run type using artifacts in {parsed_args.artifact_storage_path}.")
    return benchmark_ci_args

def generate_artifacts_for_commit(parsed_args: Namespace, repo_url: str, repo_dir: str, commit: str, is_local: bool = False) -> None:
    kill_dotnet_processes()
    if is_local:
        repo_path = repo_dir
        if(not os.path.exists(repo_path)):
            raise RuntimeError(f"The specified local path {repo_path} does not exist.")
        getLogger().info(f"Running for {repo_path} at {commit}.")
    else:
        repo_path = os.path.join(parsed_args.repo_storage_path, repo_dir)
        getLogger().info(f"Running for {repo_path} at {commit}.")

        # TODO Check to see if we already have all necessary artifacts generated for the commit and run types (Move the check from generate_all_runtype_dependencies to here before checkout)
        if not os.path.exists(repo_path):
            Repo.clone_from(repo_url, repo_path)
            repo = Repo(repo_path)
            repo.git.checkout(commit)
            repo.git.show('HEAD')
        else:
            repo = Repo(repo_path)
            repo.remotes.origin.fetch()
            repo.git.checkout(commit)
            repo.git.show('HEAD')

    # Determine what we need to generate for the local benchmarks
    generate_all_runtype_dependencies(parsed_args, repo_path, commit, (is_local and not parsed_args.skip_local_rebuild) or parsed_args.rebuild_artifacts)

# Run tests on the local machine
def run_benchmarks(parsed_args: Namespace, commits: list) -> None:
    # Generate the correct benchmarks_ci.py arguments for the run type
    for run_type in enum_name_list_to_enum_list(RunType, parsed_args.run_type_names):
        # Run the benchmarks_ci.py test and save results
        try:
            benchmark_ci_args = generate_benchmark_ci_args(parsed_args, run_type, commits)
            getLogger().info(f"Running benchmarks_ci.py for {run_type} at {commits} with arguments \"{' '.join(benchmark_ci_args)}\".")
            benchmarks_ci.__main(benchmark_ci_args) # Build the runtime includes a download of dotnet at this location
        except CalledProcessError:
            getLogger().error('benchmarks_ci exited with non zero exit code, please check the log and report benchmark failure')
            raise

        getLogger().info(f"Finished running benchmark for {run_type} at {commits}.")

# Check if the specified references exist in the given repository URL.
# If a reference does not exist, raise an exception.
# 
# Arguments:
# - repo_url (str): The URL of the repository to check.
# - references (list): A list of references (branches or commit hashes) to check.
# - repo_storage_path (str): The directory where the cloned repository is stored.
# - repo_dir (str): The name of the directory where the cloned repository is stored.
#
# Returns: None
def check_references_exist_and_add_branch_commits(repo_url: str, references: list, repo_storage_path: str, repo_dir: str):
    getLogger().debug(f"Inside check_references_exist_and_add_branch_commits: Checking if references {references} exist in {repo_url}.")
    
    # Initialize a new Git repository in the specified directory
    repo_combined_path = os.path.join(repo_storage_path, repo_dir)
    if not os.path.exists(repo_combined_path):
        getLogger().debug(f"Cloning {repo_url} to {repo_combined_path}.")
        repo = Repo.clone_from(repo_url, repo_combined_path)
    else:
        repo = Repo(repo_combined_path)
        repo.remotes.origin.fetch()

    # Check if each reference exists in the repository
    for reference in references:
        try:
            result = repo.git.branch('-r', '--contains', reference) # Use git branch -r --contains <commit> to check if a commit is in a branch
            if "error: malformed object" in result:
                raise Exception(f"Reference {reference} does not exist in {repo_url}.")
        except GitCommandError:
            raise Exception(f"Reference {reference} does not exist in {repo_url}.")

def add_arguments(parser):
    # Arguments for the local runner script
    parser.add_argument('--list-cached-builds', action='store_true', help='Lists the cached builds located in the artifact-storage-path.')
    parser.add_argument('--commits', nargs='+', type=str, help='The commits to test.')
    parser.add_argument('--repo_url', type=str, default='https://github.com/dotnet/runtime.git', help='The runtime repo to test from, used to get data for a fork.')
    parser.add_argument('--local-test-repo', type=str, help='Path to a local repo with the runtime source code to test from.') 
    parser.add_argument('--separate-repos', action='store_true', help='Whether to test each runtime version from their own separate repo directory.')
    parser.add_argument('--repo-storage-path', type=str, default='.', help='The path to store the cloned repositories in.')
    parser.add_argument('--artifact-storage-path', type=str, default=f'{os.getcwd()}{os.path.sep}runtime-testing-artifacts', help='The path to store the artifacts in (builds, results, etc).')
    parser.add_argument('--rebuild-artifacts', action='store_true', help='Whether to rebuild the artifacts for the specified commits before benchmarking.')
    parser.add_argument('--build-only', action='store_true', help='Whether to only build the artifacts for the specified commits and not run the benchmarks.')
    parser.add_argument('--skip-local-rebuild', action='store_true', help='Whether to skip rebuilding the local repo and use the already built version (if already built). Useful if you need to run against local changes again.')
    def __is_valid_run_type(value):
        try:
            RunType[value]
        except KeyError:
            raise ArgumentTypeError(f"Invalid run type: {value}.")
        return value
    parser.add_argument('--run-types', dest='run_type_names', nargs='+', type=__is_valid_run_type, choices=[run_type.name for run_type in RunType], help='The types of runs to perform.')
    parser.add_argument('--quiet', dest='verbose', action='store_false', help='Whether to not print verbose output.')
    
    # Arguments specifically for dependency generation and BDN
    parser.add_argument('--bdn-arguments', type=str, default="", help='Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes')
    parser.add_argument('--architecture', choices=['x64', 'x86', 'arm64', 'arm'], default=get_machine_architecture(), help='Specifies the SDK processor architecture')
    parser.add_argument('--os', choices=['windows', 'linux'], default=platform.system().lower(), help='Specifies the operating system of the system')
    parser.add_argument('--filter', type=str, default='*', help='Specifies the benchmark filter to pass to BenchmarkDotNet')
    parser.add_argument('-f', '--framework', choices=ChannelMap.get_supported_frameworks(), default='net8.0', help='The target framework to run the benchmarks against.') # Can and should this accept multiple frameworks?
    parser.add_argument('--csproj', type=str, default='../src/benchmarks/micro/MicroBenchmarks.csproj', help='The path to the csproj file to run benchmarks against.')    

def __main(args: list):
    # Define the ArgumentParser
    parser = ArgumentParser(description='Run local benchmarks for the Performance repo.')
    add_arguments(parser)
    parsed_args = parser.parse_args(args)
    
    setup_loggers(verbose=parsed_args.verbose)

    # If list cached builds is specified, list the cached builds and exit
    if parsed_args.list_cached_builds:
        for folder in os.listdir(parsed_args.artifact_storage_path):
            if any([run_type.name in folder for run_type in RunType]):
                print(folder)
        return

    # Check to make sure we have something specified to test
    if parsed_args.commits or parsed_args.local_test_repo:
        if parsed_args.commits:
            getLogger().info(f"Commits to test are: {parsed_args.commits}")
        if parsed_args.local_test_repo:
            getLogger().info(f"Local repo to test is: {parsed_args.local_test_repo}")
    else:
        raise Exception("A commit id and/or local repo must be specified.")

    getLogger().debug(f"Input arguments: {parsed_args}")

    repo_dirs = []
    repo_url = parsed_args.repo_url
    if parsed_args.commits:
        check_references_exist_and_add_branch_commits(repo_url, parsed_args.commits, parsed_args.repo_storage_path, repo_dirs[0] if parsed_args.separate_repos else "runtime")
        for commit in parsed_args.commits:
            repo_dirs.append(f"runtime-{commit.replace('/', '-')}")

    try:
        getLogger().info("Killing any running dotnet, vstest, or msbuild processes... (ignore system cannot find path specified)")
        kill_dotnet_processes()
        getLogger().info("****** MAKE SURE TO RUN AS ADMINISTRATOR ******")

        # Generate the artifacts for each of the remote versions
        if parsed_args.commits:
            getLogger().info(f"Checking if references {parsed_args.commits} exist in {repo_url}.")
            getLogger().info(f"References {parsed_args.commits} exist in {repo_url}.")
            for repo_dir, commit in zip(repo_dirs, parsed_args.commits):
                if parsed_args.separate_repos:
                    generate_artifacts_for_commit(parsed_args, repo_url, repo_dir, commit)
                else:
                    generate_artifacts_for_commit(parsed_args, repo_url, "runtime", commit)

        # Generate the artifacts for the local version
        if parsed_args.local_test_repo:
            generate_artifacts_for_commit(parsed_args, "local", parsed_args.local_test_repo, "local", True)

        if(not parsed_args.build_only):
            # Run the benchmarks
            commitsToRun = []
            if parsed_args.commits:
                commitsToRun = parsed_args.commits
            if parsed_args.local_test_repo:
                commitsToRun.append("local")
            run_benchmarks(parsed_args, commitsToRun)
        else:
            getLogger().info("Skipping benchmark run because --build-only was specified.")
        
    finally:
        kill_dotnet_processes()
    # TODO: Compare the results of the benchmarks || This is doable with just BDN as a start for now

if __name__ == "__main__":
    __main(sys.argv[1:])