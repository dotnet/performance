from argparse import ArgumentParser, ArgumentTypeError, Namespace
from enum import Enum
from logging import getLogger
from git.repo import Repo
from git import GitCommandError
from performance.common import get_machine_architecture
from performance.logger import setup_loggers
from subprocess import CalledProcessError

import benchmarks_ci
import sys
import os

# Assumptions: We are only testing this Performance repo, should allow single run or multiple runs
# For dotnet_version based runs, use the benchmarks_monthly .py script instead
# Verify the input commands
# What do we want to be able to test on: git commits, branches, etc?
# What are supported default cases: MonoJIT, MonoAOT, MonoInter, CoreCLR, etc. 

class RuntimeRefType(Enum):
    BRANCH = 1
    HASH = 2

class RunType(Enum):
    MonoAOT = 1
    MonoInterpreter = 2
    MonoJIT = 3
    CoreCLR = 4

def generate_runtype_dependencies(parsed_args: Namespace, repo_path: str):
    getLogger().info("Generating dependencies for " + RunType[parsed_args.run_type_name].name + " run type in " + repo_path + ".")

def generate_benchmark_ci_args(parsed_args: Namespace, repo_path: str) -> list:
    bdn_args = []
    bdn_args += ['--architecture', parsed_args.architecture]
    bdn_args += ['-f', parsed_args.framework]
    bdn_args += ['--filter', parsed_args.filter]
    bdn_args += parsed_args.bdn_arguments
    getLogger().info("Generating benchmark_ci.py arguments for " + RunType[parsed_args.run_type_name].name + " run type in " + repo_path + ".")
    return bdn_args

# Run tests on the local machine
def run_benchmark(parsed_args: Namespace, reference_type: RuntimeRefType, repo_url: str, repo_dir: str, branch_name_or_commit_hash: str):
    repo_path = os.path.join(parsed_args.repo_storage_dir, repo_dir)
    getLogger().info("Running benchmark for " + repo_path + " at " + branch_name_or_commit_hash + ".")
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
    generate_runtype_dependencies(parsed_args, repo_path)

    # Generate the correct benchmarks_ci.py arguments for the run type
    benchmark_ci_args = generate_benchmark_ci_args(parsed_args, repo_path)

    # Run the benchmarks_ci.py test and save results
    try:
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
        try:
            repo.git.ls_remote(repo_url, reference)
        except GitCommandError:
            # If a reference does not exist, raise an exception
            raise Exception(f"Reference {reference} does not exist in {repo_url}.")

def add_arguments(parser):
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
    parser.add_argument('--run-type', dest='run_type_name', type=__is_valid_run_type, choices=[run_type.name for run_type in RunType], help='The type of run to perform. (Without "RunType" prefix)')
    parser.add_argument('--verbose', action='store_true', help='Whether to print verbose output.')
    
    # Arguments specifically for BDN
    parser.add_argument('--bdn-arguments', nargs=1, type=str, default="", help='Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes')
    parser.add_argument('--architecture', choices=['x64', 'x86', 'arm64', 'arm'], default=get_machine_architecture(), help='Specifies the SDK processor architecture')
    parser.add_argument('--filter', nargs=1, type=str, default='*', help='Specifies the benchmark filter to pass to BenchmarkDotNet')
    parser.add_argument('-f', '--framework', default='net8.0', help='The target framework to run the benchmarks against.')

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
    check_references_exist(repo_url, references, parsed_args.repo_storage_dir, repo_dirs[0])
    for repo_dir, git_selector_attribute in zip(repo_dirs, references):
        if parsed_args.separate_repos:
            run_benchmark(parsed_args, runtime_ref_type, repo_url, repo_dir, git_selector_attribute)
        else:
            run_benchmark(parsed_args, runtime_ref_type, repo_url, "runtime", git_selector_attribute)

if __name__ == "__main__":
    __main(sys.argv[1:])