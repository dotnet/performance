'''
MAUI Desktop BenchmarkDotNet benchmarks.

Handles MAUI-specific setup (clone, branch mapping, dependency build) then
delegates to the shared BDNDesktopHelper for the generic BDN workflow
(patch, build benchmarks, run, collect results).

Usage: test.py --framework net11.0 --suite all
'''
import os
import shutil
import subprocess
import sys
import urllib.request
import zipfile
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


def _find_git() -> str:
    '''Find the git executable on PATH or at common Windows locations.'''
    git = shutil.which('git')
    if git:
        return git

    if sys.platform == 'win32':
        for candidate in [
            os.path.join(os.environ.get('ProgramFiles', r'C:\Program Files'), 'Git', 'cmd', 'git.exe'),
            os.path.join(os.environ.get('ProgramFiles(x86)', r'C:\Program Files (x86)'), 'Git', 'cmd', 'git.exe'),
            os.path.join(os.environ.get('ProgramW6432', r'C:\Program Files'), 'Git', 'cmd', 'git.exe'),
        ]:
            if os.path.isfile(candidate):
                return candidate

    return None


def _git_sparse_clone(git: str, branch: str, repo_dir: str):
    '''Clone using git sparse checkout (preferred — smaller download).'''
    log = getLogger()
    log.info(f'Using git at: {git}')

    subprocess.run([
        git, 'clone',
        '-c', 'core.longpaths=true',
        '--depth', '1',
        '--filter=blob:none',
        '--sparse',
        '--branch', branch,
        MAUI_REPO_URL,
        repo_dir
    ], check=True)

    subprocess.run(
        [git, 'sparse-checkout', 'set'] + MAUI_SPARSE_CHECKOUT_DIRS,
        cwd=repo_dir, check=True)


def _zip_download(branch: str, repo_dir: str):
    '''Download the repo as a zip archive and extract needed directories.

    Fallback when git is not available (e.g. Helix work items where git is
    not on PATH and not installed).
    '''
    log = getLogger()
    archive_url = f'https://github.com/dotnet/maui/archive/refs/heads/{branch}.zip'
    zip_path = 'maui_download.zip'

    log.info(f'git not found — downloading archive from {archive_url}')
    urllib.request.urlretrieve(archive_url, zip_path)
    log.info(f'Downloaded {os.path.getsize(zip_path) / (1024*1024):.1f} MB')

    os.makedirs(repo_dir, exist_ok=True)

    # Directories to extract (sparse checkout equivalent + root-level files)
    sparse_prefixes = [d.rstrip('/') + '/' for d in MAUI_SPARSE_CHECKOUT_DIRS]

    with zipfile.ZipFile(zip_path) as zf:
        # GitHub archives have a top-level dir like "maui-net11.0/"
        top_dir = zf.namelist()[0].split('/')[0] + '/'

        for member in zf.namelist():
            if not member.startswith(top_dir):
                continue
            rel_path = member[len(top_dir):]
            if not rel_path:
                continue

            # Include root-level files and our sparse directories
            is_root_file = '/' not in rel_path
            in_sparse_dir = any(rel_path.startswith(p) for p in sparse_prefixes)

            if not is_root_file and not in_sparse_dir:
                continue

            target = os.path.join(repo_dir, rel_path)
            if member.endswith('/'):
                os.makedirs(target, exist_ok=True)
            else:
                os.makedirs(os.path.dirname(target), exist_ok=True)
                with zf.open(member) as src, open(target, 'wb') as dst:
                    dst.write(src.read())

    os.remove(zip_path)
    log.info('Archive extracted.')


def clone_maui_repo(branch: str, repo_dir: str = MAUI_REPO_DIR):
    '''Clone or download dotnet/maui at the given branch.'''
    log = getLogger()
    log.info(f'Acquiring dotnet/maui branch {branch}...')

    if os.path.exists(repo_dir):
        remove_directory(repo_dir)

    git = _find_git()
    if git:
        _git_sparse_clone(git, branch, repo_dir)
    else:
        _zip_download(branch, repo_dir)

    log.info('MAUI source acquired.')


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
