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
import platform
import subprocess
import sys
import os


# Assumptions: We are only testing this Performance repo, should allow single run or multiple runs
# For dotnet_version based runs, use the benchmarks_monthly .py script instead
# Verify the input commands
# What do we want to be able to test on: git commits, branches, etc?
# What are supported default cases: MonoJIT, MonoAOT, MonoInter, Corerun, etc. 

class RuntimeRefType(Enum):
    BRANCH = 1
    HASH = 2

class RunType(Enum):
    CoreRun = 1
    MonoAOT = 2
    MonoInterpreter = 3
    MonoJIT = 4

def check_for_runtype_specified(parsed_args: Namespace, run_types_to_check: list) -> bool:
    for run_type in run_types_to_check:
        if run_type in parsed_args.run_type_name:
            return True
    return False

# Builds libs and corerun by default
def build_runtime_dependency(parsed_args: Namespace, repo_path: str, subset: str = "clr+libs+libs.tests", configuration: str = "Release", additional_args: list = []):    
    # Run the command
    build_libs_and_corerun_command = [
                f"build.ps1", 
                "-subset", subset, 
                "-configuration", configuration, 
                "-os", f"{parsed_args.operating_system}",
                "-arch", f"{parsed_args.architecture}", 
                "-framework", f"{parsed_args.frameworks}"
            ] + additional_args
    RunCommand(build_libs_and_corerun_command, verbose=True).run(os.path.join(repo_path, "eng"))

def generate_layout(parsed_args: Namespace, repo_path: str, additional_args: list):
    # Run the command
    generate_layout_command = [
                f"build.ps1", 
                "release",
                parsed_args.architecture,
                "generatelayoutonly",
                "/p:LibrariesConfiguration=Release"
            ] + additional_args
    RunCommand(generate_layout_command, verbose=True).run(os.path.join(repo_path, "eng"))

# Try to generate all of a single runs dependencies at once to save time
def generate_all_runtype_dependencies(parsed_args: Namespace, repo_path: str):
    getLogger().info("Generating dependencies for " + parsed_args.run_type_name + " run types in " + repo_path + ".")
    
    if check_for_runtype_specified(parsed_args, [RunType.CoreRun, RunType.MonoInterpreter, RunType.MonoJIT]):
        build_runtime_dependency(parsed_args, repo_path) # Build libs and corerun by default
    if check_for_runtype_specified(parsed_args, [RunType.MonoInterpreter, RunType.MonoJIT]):
        build_runtime_dependency(parsed_args, repo_path, "mono+libs+host+packs") 
        build_runtime_dependency(parsed_args, repo_path, "libs.pretest", additional_args=['-testscope', 'innerloop', '/p:RuntimeFlavor=mono', f"/p:RuntimeArtifactsPath={repo_path}/artifacts/bin/mono/{parsed_args.os}.{parsed_args.architecture}.Release"]) 
    if check_for_runtype_specified(parsed_args, [RunType.MonoAOT]):
        build_runtime_dependency(parsed_args, repo_path, "mono+libs+host+packs", additional_args=['/p:CrossBuild=false' '/p:MonoLLVMUseCxx11Abi=false']) 
    
    getLogger().info("Finished generating dependencies for " + parsed_args.run_type_name + " run types in " + repo_path + ".")

def generate_benchmark_ci_args(parsed_args: Namespace, repo_path: str, specific_run_type: RunType) -> list:
    getLogger().info("Generating benchmark_ci.py arguments for " + specific_run_type.name + " run type in " + repo_path + ".")
    benchmark_ci_args = []
    benchmark_ci_args += ['--architecture', parsed_args.architecture]
    benchmark_ci_args += ['--frameworks', parsed_args.frameworks]
    benchmark_ci_args += ['--filter', parsed_args.filter]
    benchmark_ci_args += ['--csproj', parsed_args.csproj]
    benchmark_ci_args += ['--incremental', "no"]
    benchmark_ci_args += ['--bdn-artifacts-directory', os.path.join(repo_path, "artifacts", "BenchmarkDotNet.Artifacts", parsed_args.os + "." + parsed_args.architecture + ".Release")]

    if specific_run_type == RunType.CoreRun:
        benchmark_ci_args += ['--core-run']
        raise NotImplementedError("CoreRun is not yet implemented.")
    elif specific_run_type == RunType.MonoAOT:
        benchmark_ci_args += ['--mono-aot']
        raise NotImplementedError("MonoAOT is not yet implemented.")
    elif specific_run_type == RunType.MonoInterpreter:
        benchmark_ci_args += [
                                '--anyCategories', 'Libraries', 'Runtime', 
                                '--category-exclusion-filter', 'NoInterpreter', 'NoMono', 
                                '--logBuildOutput', 
                                '--generateBinLog', 
                                '--corerun', f'{repo_path}/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/8.0.0/corerun'
                              ]
        benchmark_ci_args += ['--envVars', 'MONO_ENV_OPTIONS=--interpreter']
        raise NotImplementedError("MonoInterpreter is not yet implemented.")
    elif specific_run_type == RunType.MonoJIT:
        benchmark_ci_args += ['--mono-jit']
        raise NotImplementedError("MonoJIT is not yet implemented.")
    
    if parsed_args.bdn_arguments:
        benchmark_ci_args += ['--bdn-arguments', parsed_args.bdn_arguments]
    getLogger().info("Finished generating benchmark_ci.py arguments for " + specific_run_type.name + " run type in " + repo_path + ".")
    return benchmark_ci_args

# Run tests on the local machine
def run_benchmark(parsed_args: Namespace, reference_type: RuntimeRefType, repo_url: str, repo_dir: str, branch_name_or_commit_hash: str):
    repo_path = os.path.join(parsed_args.repo_storage_dir, repo_dir)
    getLogger().info("Running for " + repo_path + " at " + branch_name_or_commit_hash + ".")
    # Clone runtime or checkout the correct commit or branch
    if not os.path.exists(repo_path):
        if reference_type == RuntimeRefType.BRANCH:
            Repo.clone_from(repo_url, repo_path, branch=branch_name_or_commit_hash)
        elif reference_type == RuntimeRefType.HASH:
            Repo.clone_from(repo_url, repo_path)
            repo = Repo(repo_path)
            repo.git.checkout(branch_name_or_commit_hash)
    else:
        repo = Repo(repo_path)
        repo.remotes.origin.fetch()
        repo.git.checkout(branch_name_or_commit_hash)

    # Determine what we need to generate for the local benchmarks
    generate_all_runtype_dependencies(parsed_args, repo_path)

    # Generate the correct benchmarks_ci.py arguments for the run type
    for run_type in parsed_args.run_type_name:
        # Run the benchmarks_ci.py test and save results
        try:
            benchmark_ci_args = generate_benchmark_ci_args(parsed_args, repo_path, run_type)
            getLogger().info("Running benchmarks_ci.py for " + repo_path + " at " + branch_name_or_commit_hash + " with arguments \"" + ' '.join(map(str, benchmark_ci_args)) + "\".")
            benchmarks_ci.__main(benchmark_ci_args)
        except CalledProcessError:
            getLogger().error('benchmarks_ci exited with non zero exit code, please check the log and report benchmark failure')
            raise

    getLogger().info("Finished running benchmark for " + repo_path + " at " + branch_name_or_commit_hash + ".")

# Check if the specified references exist in the given repository URL.
# If a reference does not exist, raise an exception.
# 
# Arguments:
# - repo_url (str): The URL of the repository to check.
# - references (list): A list of references (branches or commit hashes) to check.
# - repo_storage_dir (str): The directory where the cloned repository is stored.
# - repo_dir (str): The name of the directory where the cloned repository is stored.
#
# Returns: None
def check_references_exist(repo_url: str, references: list, repo_storage_dir: str, repo_dir: str):
    # Initialize a new Git repository in the specified directory
    repo = Repo.init(os.path.join(repo_storage_dir, repo_dir))
    
    # Check if each reference exists in the repository
    for reference in references:
        remotes = repo.git.ls_remote(repo_url, reference)
        if len(remotes) == 0:
            raise Exception(f"Reference {reference} does not exist in {repo_url}.")


def add_arguments(parser):
    
    # Arguments for the local runner script
    parser.add_argument('--branches', nargs='+', type=str, help='The branches to test.')
    parser.add_argument('--hashes', nargs='+', type=str, help='The hashes to test.')
    parser.add_argument('--repo', type=str, default='https://github.com/dotnet/runtime.git', help='The runtime repo to test from, used to get data for a fork.')
    parser.add_argument('--separate-repos', action='store_true', help='Whether to test each runtime version from their own separate repo directory.')
    parser.add_argument('--repo-storage-dir', type=str, default='.', help='The directory to store the cloned repositories.')
    def __is_valid_run_type(value):
        try:
            RunType[value]
        except KeyError:
            raise ArgumentTypeError(f"Invalid run type: {value}.")
        return value
    parser.add_argument('--run-type', dest='run_type_name', nargs='+', type=__is_valid_run_type, choices=[run_type.name for run_type in RunType], help='The type of run to perform. (Without "RunType" prefix)')
    parser.add_argument('--quiet', dest='verbose', action='store_false', help='Whether to not print verbose output.')
    
    # Arguments specifically for dependency generation and BDN
    parser.add_argument('--bdn-arguments', type=str, default="", help='Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes')
    parser.add_argument('--architecture', choices=['x64', 'x86', 'arm64', 'arm'], default=get_machine_architecture(), help='Specifies the SDK processor architecture')
    parser.add_argument('--os', choices=['windows', 'linux'], default=platform.system().lower(), help='Specifies the operating system of the system')
    parser.add_argument('--filter', type=str, default='*', help='Specifies the benchmark filter to pass to BenchmarkDotNet')
    parser.add_argument('-f', '--frameworks', choices=ChannelMap.get_supported_frameworks(), nargs='+', default='net8.0', help='The target framework(s) to run the benchmarks against.')
    parser.add_argument('--csproj', type=str, default='../src/benchmarks/micro/MicroBenchmarks.csproj', help='The path to the csproj file to run benchmarks against.')
    

def __main(args: list):
    # Define the ArgumentParser
    parser = ArgumentParser(description='Run local benchmarks for the Performance repo.')
    add_arguments(parser)

    # Parse the arguments
    parsed_args = parser.parse_args(args)

    setup_loggers(verbose=parsed_args.verbose)
    runtime_ref_type = RuntimeRefType.BRANCH

    if parsed_args.branches and parsed_args.hashes:
        raise Exception("Cannot specify both branches and hashes.")
    elif parsed_args.branches:
        runtime_ref_type = RuntimeRefType.BRANCH
        getLogger().info("Branches to test are: " + str(parsed_args.branches))
    elif parsed_args.hashes:
        runtime_ref_type = RuntimeRefType.HASH
        getLogger().info("Hashes to test are: " + str(parsed_args.hashes))
    else:
        raise Exception("Either a branch or hash must be specified.")

    branch_names = []
    commit_hashes = []
    if runtime_ref_type == RuntimeRefType.BRANCH:
        repo_url = parsed_args.repo
        branch_names = parsed_args.branches
        repo_dirs = ["runtime-" + branch_name.replace('/', '-') for branch_name in branch_names]
    elif runtime_ref_type == RuntimeRefType.HASH:
        repo_url = parsed_args.repo
        commit_hashes = parsed_args.hashes
        repo_dirs = ["runtime-" + commit_hash for commit_hash in commit_hashes]
    else:
        raise Exception("Invalid runtime ref type.")

    references = branch_names if runtime_ref_type == RuntimeRefType.BRANCH else commit_hashes
    getLogger().info("Checking if references " + str(references) + " exist in " + repo_url + ".")
    check_references_exist(repo_url, references, parsed_args.repo_storage_dir, repo_dirs[0])
    getLogger().info("References exist in " + repo_url + ".")
    for repo_dir, git_selector_attribute in zip(repo_dirs, references):
        if parsed_args.separate_repos:
            run_benchmark(parsed_args, runtime_ref_type, repo_url, repo_dir, git_selector_attribute)
        else:
            run_benchmark(parsed_args, runtime_ref_type, repo_url, "runtime", git_selector_attribute)

if __name__ == "__main__":
    __main(sys.argv[1:])