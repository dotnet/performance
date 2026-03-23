'''
MAUI Desktop BenchmarkDotNet benchmarks.

Handles MAUI-specific setup (clone, branch mapping, dependency build) then
delegates to the shared BDNDesktopHelper for the generic BDN workflow
(patch, build benchmarks, run, collect results).

Usage: test.py --framework net11.0 --suite all
'''
import os
import subprocess
from argparse import ArgumentParser
from logging import getLogger
from performance.common import remove_directory
from performance.logger import setup_loggers
from shared.bdndesktop import BDNDesktopHelper

# ── MAUI-specific configuration ─────────────────────────────────────────────

MAUI_REPO_URL = 'https://github.com/dotnet/maui.git'
MAUI_REPO_DIR = 'maui_repo'

MAUI_BENCHMARK_PROJECTS = {
    'core': 'src/Core/tests/Benchmarks/Core.Benchmarks.csproj',
    'xaml': 'src/Controls/tests/Xaml.Benchmarks/Microsoft.Maui.Controls.Xaml.Benchmarks.csproj',
    'graphics': 'src/Graphics/tests/Graphics.Benchmarks/Graphics.Benchmarks.csproj',
}

MAUI_SPARSE_CHECKOUT_DIRS = [
    'src/Core', 'src/Controls', 'src/Graphics', 'src/SingleProject',
    'src/Workload', 'src/Essentials',
    'eng', '.config',
]

MAUI_BUILD_SOLUTION_FILTER = 'Microsoft.Maui.BuildTasks.slnf'

# MSBuild properties to disable non-desktop target frameworks.
# MAUI's Directory.Build.props sets these to true unconditionally at multiple
# points; MauiPlatforms is computed from them.  In-place replacement is
# required because appending overrides at the end doesn't work (MSBuild
# evaluates top-to-bottom).
DESKTOP_ONLY_PROPS = {
    'IncludeAndroidTargetFrameworks': 'false',
    'IncludeIosTargetFrameworks': 'false',
    'IncludeMacCatalystTargetFrameworks': 'false',
    'IncludeMacOSTargetFrameworks': 'false',
    'IncludeTizenTargetFrameworks': 'false',
}


def get_branch(framework: str) -> str:
    '''Map framework moniker to MAUI repo branch.'''
    if framework and framework.startswith('net'):
        return framework  # net11.0 -> net11.0, net10.0 -> net10.0
    return 'net11.0'


def clone_maui_repo(branch: str, repo_dir: str = MAUI_REPO_DIR):
    '''Sparse-clone dotnet/maui at the given branch.'''
    log = getLogger()
    log.info(f'Cloning dotnet/maui branch {branch} (sparse, depth 1)...')

    if os.path.exists(repo_dir):
        remove_directory(repo_dir)

    subprocess.run([
        'git', 'clone',
        '-c', 'core.longpaths=true',
        '--depth', '1',
        '--filter=blob:none',
        '--sparse',
        '--branch', branch,
        MAUI_REPO_URL,
        repo_dir
    ], check=True)

    subprocess.run(
        ['git', 'sparse-checkout', 'set'] + MAUI_SPARSE_CHECKOUT_DIRS,
        cwd=repo_dir, check=True)

    log.info('Clone complete.')


def build_maui_dependencies(repo_dir: str = MAUI_REPO_DIR):
    '''Restore dotnet tools and build MAUI's BuildTasks solution filter.'''
    log = getLogger()
    log.info('Restoring dotnet tools...')
    subprocess.run(['dotnet', 'tool', 'restore'], cwd=repo_dir, check=True)

    log.info(f'Building {MAUI_BUILD_SOLUTION_FILTER} (desktop TFMs only)...')
    subprocess.run([
        'dotnet', 'build',
        MAUI_BUILD_SOLUTION_FILTER,
        '-c', 'Release',
    ], cwd=repo_dir, check=True)

    log.info('MAUI dependencies built successfully.')


def parse_args():
    parser = ArgumentParser(description='Run MAUI desktop BDN benchmarks')
    parser.add_argument('--framework', '-f', default='net11.0',
                        help='Target .NET framework (determines MAUI repo branch)')
    parser.add_argument('--suite', choices=['core', 'xaml', 'graphics', 'all'],
                        default='all', help='Which benchmark suite to run')
    parser.add_argument('--bdn-args', nargs='*', default=[],
                        help='Additional arguments to pass to BenchmarkDotNet')
    parser.add_argument('--upload-to-perflab-container', action='store_true',
                        help='Upload results to perflab container')
    return parser.parse_args()


if __name__ == '__main__':
    setup_loggers(True)
    args = parse_args()

    # MAUI-specific: clone repo and build dependencies
    branch = get_branch(args.framework)
    clone_maui_repo(branch)

    # Generic BDN desktop workflow: patch, build benchmarks, run, collect
    helper = BDNDesktopHelper(
        repo_dir=MAUI_REPO_DIR,
        benchmark_projects=MAUI_BENCHMARK_PROJECTS,
        disable_props=DESKTOP_ONLY_PROPS,
    )

    # Patch Directory.Build.props BEFORE any builds (including MAUI deps)
    helper.patch_directory_build_props()

    # MAUI-specific: build BuildTasks solution filter
    build_maui_dependencies()

    # Run the generic BDN workflow
    helper.runtests(args.suite, args.bdn_args, args.upload_to_perflab_container)
