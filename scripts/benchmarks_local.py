# This is a script for testing the performance of the different dotnet/runtime build types locally
# Example usage from the performance/scripts folder: 
# python .\benchmarks_local.py --local-test-repo "<absolute path to runtime folder>/runtime" --run-type MonoJIT --filter *Span.IndexerBench.CoveredIndex2* --bdn-arguments='-i'
# or if you want remotes:
# python .\benchmarks_local.py --branches main --repo-storage-dir "<absolute path to where you want to store runtime clones>" --run-type MonoJIT --filter *Span.IndexerBench.CoveredIndex2* --bdn-arguments='-i'

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
import shutil
import sys
import os


# Assumptions: We are only testing this Performance repo, should allow single run or multiple runs
# For dotnet_version based runs, use the benchmarks_monthly .py script instead
# Verify the input commands
# What do we want to be able to test on: git commits, branches, etc?
# What are supported default cases: MonoJIT, MonoAOT, MonoInter, Corerun, etc. 

class RuntimeRefType(Enum):
    COMMITISH = 1
    LOCAL_ONLY = 2

class RunType(Enum):
    CoreRun = 1
    MonoAOT = 2
    MonoInterpreter = 3
    MonoJIT = 4

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

# Builds libs and corerun by default
def build_runtime_dependency(parsed_args: Namespace, repo_path: str, subset: str = "clr+libs+libs.tests", configuration: str = "Release", additional_args: list = []):    
    # Run the command
    build_libs_and_corerun_command = [
                "powershell.exe", # Will need to see if this works on Linux powershell core
                "-File",
                f"build.ps1", 
                "-subset", subset, 
                "-configuration", configuration, 
                "-os", f"{parsed_args.os}",
                "-arch", f"{parsed_args.architecture}", 
                "-framework", f"{parsed_args.frameworks}"
            ] + additional_args
    RunCommand(build_libs_and_corerun_command, verbose=True).run(os.path.join(repo_path, "eng"))

def generate_layout(parsed_args: Namespace, repo_path: str, additional_args: list = []):
    # Run the command
    if parsed_args.os == "windows":
        build_script = "build.cmd"
    else:
        build_script = "build.sh"
    generate_layout_command = [
                f"{build_script}",
                "release",
                parsed_args.architecture,
                "generatelayoutonly",
                "/p:LibrariesConfiguration=Release"
            ] + additional_args
    RunCommand(generate_layout_command, verbose=True).run(os.path.join(repo_path, "src/tests"))

# Try to generate all of a single runs dependencies at once to save time
def generate_all_runtype_dependencies(parsed_args: Namespace, repo_path: str):
    getLogger().info("Generating dependencies for " + ' '.join(map(str, parsed_args.run_type_names)) + " run types in " + repo_path + ".")
    
    if check_for_runtype_specified(parsed_args, [RunType.CoreRun, RunType.MonoInterpreter, RunType.MonoJIT]):
        build_runtime_dependency(parsed_args, repo_path) # Build libs and corerun by default TODO: Check if we actually need to build these for MonoInterpreter and MonoJIT

    if check_for_runtype_specified(parsed_args, [RunType.MonoInterpreter, RunType.MonoJIT]):
        build_runtime_dependency(parsed_args, repo_path, "mono+libs+host+packs") 
        build_runtime_dependency(parsed_args, repo_path, "libs.pretest", additional_args=['-testscope', 'innerloop', '/p:RuntimeFlavor=mono', f"/p:RuntimeArtifactsPath={os.path.join(repo_path, 'artifacts', 'bin', 'mono', f'{parsed_args.os}.{parsed_args.architecture}.Release')}"])
        # Create the mono-dotnet
        src_dir = os.path.join(repo_path, "artifacts", "bin", "runtime", f"net8.0-{parsed_args.os}-Release-{parsed_args.architecture}")
        dest_dir = os.path.join(repo_path, "artifacts", "bin", "testhost", f"net8.0-{parsed_args.os}-Release-{parsed_args.architecture}", "shared", "Microsoft.NETCore.App", "8.0.0")
        shutil.rmtree(dest_dir, ignore_errors=True)
        shutil.copytree(src_dir, dest_dir)
        src_dir = os.path.join(repo_path, "artifacts", "bin", "testhost", f"net8.0-{parsed_args.os}-Release-{parsed_args.architecture}")
        dest_dir = os.path.join(repo_path, "artifacts", "dotnet-mono")
        shutil.rmtree(dest_dir, ignore_errors=True)
        shutil.copytree(src_dir, dest_dir)
        src_file = os.path.join(repo_path, "artifacts", "bin", "coreclr", f"{parsed_args.os}.{parsed_args.architecture}.Release", f"corerun{'.exe' if parsed_args.os == 'windows' else ''}")
        dest_dir = os.path.join(repo_path, "artifacts", "dotnet-mono", "shared", "Microsoft.NETCore.App", "8.0.0")
        dest_file = os.path.join(dest_dir, f"corerun{'.exe' if parsed_args.os == 'windows' else ''}")
        shutil.rmtree(dest_file, ignore_errors=True)
        os.makedirs(dest_dir, exist_ok=True)
        shutil.copy2(src_file, dest_file)
        # Create the core root
        generate_layout(parsed_args, repo_path)

    if check_for_runtype_specified(parsed_args, [RunType.MonoAOT]):
        build_runtime_dependency(parsed_args, repo_path, "mono+libs+host+packs", additional_args=['/p:CrossBuild=false' '/p:MonoLLVMUseCxx11Abi=false']) 
    
    getLogger().info("Finished generating dependencies for " + ' '.join(map(str, parsed_args.run_type_names)) + " run types in " + repo_path + ".")

def generate_benchmark_ci_args(parsed_args: Namespace, repo_path: str, specific_run_type: RunType) -> list:
    getLogger().info("Generating benchmark_ci.py arguments for " + specific_run_type.name + " run type in " + repo_path + ".")
    benchmark_ci_args = []
    bdn_args_unescaped = []
    benchmark_ci_args += ['--architecture', parsed_args.architecture]
    benchmark_ci_args += ['--frameworks', parsed_args.frameworks]
    benchmark_ci_args += ['--filter', parsed_args.filter]
    benchmark_ci_args += ['--csproj', parsed_args.csproj]
    benchmark_ci_args += ['--incremental', "no"]
    benchmark_ci_args += ['--bdn-artifacts', os.path.join(repo_path, "artifacts", "BenchmarkDotNet.Artifacts")]

    if specific_run_type == RunType.CoreRun:
        raise NotImplementedError("CoreRun is not yet implemented.")
    elif specific_run_type == RunType.MonoAOT:
        raise NotImplementedError("MonoAOT is not yet implemented.")
    elif specific_run_type == RunType.MonoInterpreter:
        bdn_args_unescaped += [
                                '--anyCategories', 'Libraries', 'Runtime', 
                                '--category-exclusion-filter', 'NoInterpreter', 'NoMono', 
                                '--logBuildOutput', 
                                '--generateBinLog', 
                                '--corerun', f'{repo_path}/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/8.0.0/corerun{".exe" if parsed_args.os == "windows" else ""}'
                              ]
        bdn_args_unescaped += ['--envVars', 'MONO_ENV_OPTIONS=--interpreter']
    elif specific_run_type == RunType.MonoJIT:
        bdn_args_unescaped += [ 
                                '--anyCategories', 'Libraries', 'Runtime', 
                                '--category-exclusion-filter', 'NoInterpreter', 'NoMono', 
                                '--logBuildOutput', 
                                '--generateBinLog', 
                                '--corerun', f'{repo_path}/artifacts/dotnet-mono/shared/Microsoft.NETCore.App/8.0.0/corerun{".exe" if parsed_args.os == "windows" else ""}'
                            ]
    bdn_args_unescaped += [parsed_args.bdn_arguments]
    benchmark_ci_args += [f'--bdn-arguments={" ".join(bdn_args_unescaped)}']
    getLogger().info("Finished generating benchmark_ci.py arguments for " + specific_run_type.name + " run type in " + repo_path + ".")
    return benchmark_ci_args

# Run tests on the local machine
def run_benchmark(parsed_args: Namespace, reference_type: RuntimeRefType, repo_url: str, repo_dir: str, commitish_value: str, is_local: bool = False) -> None:
    # Clone runtime or checkout the correct commit or branch
    if is_local:
        repo_path = repo_dir
        if(not os.path.exists(repo_path)):
            raise RuntimeError(f"The specified local path {repo_path} does not exist.")
        getLogger().info("Running for " + repo_path + " at " + commitish_value + ".")
    else:
        repo_path = os.path.join(parsed_args.repo_storage_dir, repo_dir)
        getLogger().info("Running for " + repo_path + " at " + commitish_value + ".")
        if not os.path.exists(repo_path):
            if reference_type == RuntimeRefType.COMMITISH:
                Repo.clone_from(repo_url, repo_path)
                repo = Repo(repo_path)
                repo.git.checkout(commitish_value)
        else:
            repo = Repo(repo_path)
            repo.remotes.origin.fetch()
            repo.git.checkout(commitish_value)

    # Determine what we need to generate for the local benchmarks
    generate_all_runtype_dependencies(parsed_args, repo_path)

    # Generate the correct benchmarks_ci.py arguments for the run type
    for run_type in enum_name_list_to_enum_list(RunType, parsed_args.run_type_names):
        # Run the benchmarks_ci.py test and save results
        try:
            benchmark_ci_args = generate_benchmark_ci_args(parsed_args, repo_path, run_type)
            getLogger().info("Running benchmarks_ci.py for " + repo_path + " at " + commitish_value + " with arguments \"" + ' '.join(map(str, benchmark_ci_args)) + "\".")
            benchmarks_ci.__main(benchmark_ci_args)
            # TODO: Save the results
        except CalledProcessError:
            getLogger().error('benchmarks_ci exited with non zero exit code, please check the log and report benchmark failure')
            raise

    getLogger().info("Finished running benchmark for " + repo_path + " at " + commitish_value + ".")

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
    parser.add_argument('--commitishs', nargs='+', type=str, help='The commitish values to test.')
    parser.add_argument('--repo', type=str, default='https://github.com/dotnet/runtime.git', help='The runtime repo to test from, used to get data for a fork.')
    parser.add_argument('--local-test-repo', type=str, help='Path to a local repo with the runtime source code to test from.') 
    parser.add_argument('--separate-repos', action='store_true', help='Whether to test each runtime version from their own separate repo directory.')
    parser.add_argument('--repo-storage-dir', type=str, default='.', help='The directory to store the cloned repositories.')
    def __is_valid_run_type(value):
        try:
            RunType[value]
        except KeyError:
            raise ArgumentTypeError(f"Invalid run type: {value}.")
        return value
    parser.add_argument('--run-type', dest='run_type_names', nargs='+', type=__is_valid_run_type, choices=[run_type.name for run_type in RunType], help='The type of run to perform. (Without "RunType" prefix)')
    parser.add_argument('--quiet', dest='verbose', action='store_false', help='Whether to not print verbose output.')
    
    # Arguments specifically for dependency generation and BDN
    parser.add_argument('--bdn-arguments', type=str, default="", help='Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes')
    parser.add_argument('--architecture', choices=['x64', 'x86', 'arm64', 'arm'], default=get_machine_architecture(), help='Specifies the SDK processor architecture')
    parser.add_argument('--os', choices=['windows', 'linux'], default=platform.system().lower(), help='Specifies the operating system of the system')
    parser.add_argument('--filter', type=str, default='*', help='Specifies the benchmark filter to pass to BenchmarkDotNet')
    parser.add_argument('-f', '--frameworks', choices=ChannelMap.get_supported_frameworks(), nargs='+', default='net8.0', help='The target framework(s) to run the benchmarks against.') # TODO: Should we accept a list of repo, branch, and framework tuples?
    parser.add_argument('--csproj', type=str, default='../src/benchmarks/micro/MicroBenchmarks.csproj', help='The path to the csproj file to run benchmarks against.')
    

def __main(args: list):
    # Define the ArgumentParser
    parser = ArgumentParser(description='Run local benchmarks for the Performance repo.')
    add_arguments(parser)

    # Parse the arguments
    parsed_args = parser.parse_args(args)
    
    setup_loggers(verbose=parsed_args.verbose)
    runtime_ref_type = RuntimeRefType.COMMITISH

    if parsed_args.commitishs:
        runtime_ref_type = RuntimeRefType.COMMITISH
        getLogger().info("Commitishs to test are: " + str(parsed_args.commitishs))
    elif parsed_args.local_test_repo:
        runtime_ref_type = RuntimeRefType.LOCAL_ONLY
        getLogger().info("Local repo to test is: " + str(parsed_args.local_test_repo))
    else:
        raise Exception("Either a branch, hash, or local repo must be specified.")

    getLogger().info("Input arguments: " + str(parsed_args))

    commitish_values = []
    if runtime_ref_type == RuntimeRefType.COMMITISH:
        repo_url = parsed_args.repo
        commitish_values = parsed_args.commitishs
        repo_dirs = ["runtime-" + branch_name.replace('/', '-') for branch_name in commitish_values]
    elif not runtime_ref_type == RuntimeRefType.LOCAL_ONLY:
        raise Exception("Invalid runtime ref type.")

    # Run the test for each of the remote versions to test
    if not runtime_ref_type == RuntimeRefType.LOCAL_ONLY:
        references = commitish_values
        getLogger().info("Checking if references " + str(references) + " exist in " + repo_url + ".")
        check_references_exist(repo_url, references, parsed_args.repo_storage_dir, repo_dirs[0])
        getLogger().info("References exist in " + repo_url + ".")
        for repo_dir, git_selector_attribute in zip(repo_dirs, references):
            if parsed_args.separate_repos:
                run_benchmark(parsed_args, runtime_ref_type, repo_url, repo_dir, git_selector_attribute)
            else:
                run_benchmark(parsed_args, runtime_ref_type, repo_url, "runtime", git_selector_attribute)

    # Run the test for the local version to test
    if parsed_args.local_test_repo:
        run_benchmark(parsed_args, runtime_ref_type, "local", parsed_args.local_test_repo, "local", True)

    # TODO: Compare the results of the benchmarks

if __name__ == "__main__":
    __main(sys.argv[1:])