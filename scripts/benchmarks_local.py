from argparse import ArgumentParser
from enum import Enum
from git.repo import Repo
import os
import sys

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

# Run tests on the local machine
def RunBenchmark(reference_type: RuntimeRefType, repo_url: str, repo_dir: str, branch_name_or_commit_hash: str):
    print("Running benchmark")
    # Clone runtime or checkout the correct commit or branch
    if not os.path.exists(repo_dir):
        if reference_type == RuntimeRefType.BRANCH:
            Repo.clone_from(repo_url, repo_dir, branch=branch_name_or_commit_hash)
        elif reference_type == RuntimeRefType.HASH:
            Repo.clone_from(repo_url, repo_dir)
            repo = Repo(repo_dir)
            repo.git.checkout(branch_name_or_commit_hash)
    # Determine what we need to generate for the local benchmarks
    # Run the correct benchmarks_ci.py test and save results

def add_arguments(parser):
    parser.add_argument('--branches', nargs='+', type=str, help='The branches to test.')
    parser.add_argument('--hashes', nargs='+', type=str, help='The hashes to test.')
    parser.add_argument('--repo', type=str, default='https://github.com/dotnet/runtime.git', help='The runtime repo to test from, used to get data for fork branches.')
    parser.add_argument('--separate-repos', action='store_false', help='Whether to test each runtime version from their own separate repo.')


def main(args: list):
    runtime_ref_type = RuntimeRefType.BRANCH

    # Define the ArgumentParser
    parser = ArgumentParser(description='Run local benchmarks for the Performance repo.')
    add_arguments(parser)

    # Parse the arguments
    parsed_args = parser.parse_args(args)

    if parsed_args.branches and parsed_args.hashes:
        raise Exception("Cannot specify both branches and hashes.")
    elif parsed_args.branches:
        runtime_ref_type = RuntimeRefType.BRANCH
        print("Branches to test are: " + str(parsed_args.branches))
    elif parsed_args.hashes:
        runtime_ref_type = RuntimeRefType.HASH
        print("Hashes to test are: " + str(parsed_args.hashes))
    else:
        raise Exception("Either a branch or hash must be specified.")

    branch_names = []
    commit_hashes = []
    if runtime_ref_type == RuntimeRefType.BRANCH:
        repo_url = parsed_args.repo
        branch_names = parsed_args.branches
        repo_dirs = ["runtime-" + branch_name for branch_name in branch_names]
    elif runtime_ref_type == RuntimeRefType.HASH:
        repo_url = parsed_args.repo
        commit_hashes = parsed_args.hashes
        repo_dirs = ["runtime-" + commit_hash for commit_hash in commit_hashes]
    else:
        raise Exception("Invalid runtime ref type.")

    for repo_dir, git_selector_attribute in zip(repo_dirs, branch_names if runtime_ref_type == RuntimeRefType.BRANCH else commit_hashes):
        if parsed_args.separate_repos:
            RunBenchmark(runtime_ref_type, repo_url, repo_dir, git_selector_attribute)
        else:
            RunBenchmark(runtime_ref_type, repo_url, "runtime", git_selector_attribute)

if __name__ == "__main__":
    main(sys.argv[1:])