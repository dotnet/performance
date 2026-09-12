'''
MAUI Desktop BenchmarkDotNet benchmarks.

Handles MAUI-specific setup (clone, branch mapping, dependency build) then
delegates to the shared BDNDesktopHelper for the generic BDN workflow
(patch, build benchmarks, run, collect results).

Usage: test.py --framework net11.0 --suite all
'''
import os
import re
import shutil
import urllib.parse
import urllib.request
import zipfile
from argparse import ArgumentParser
from logging import getLogger
from typing import Optional
from performance.common import RunCommand, iswin, remove_directory
from performance.logger import setup_loggers
from shared.bdndesktop import BDNDesktopHelper

# ── MAUI-specific configuration ─────────────────────────────────────────────

MAUI_REPO_URL = 'https://github.com/dotnet/maui.git'
MAUI_REPO_DIRNAME = 'maui_repo'
# Anchor the clone target to an absolute path so subsequent subprocess
# calls (which may set cwd) keep finding the same checkout.  Resolved at
# import time against the current working directory of test.py.
MAUI_REPO_DIR = os.path.abspath(MAUI_REPO_DIRNAME)

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

# Benchmarks to exclude: these emit millions of log lines per iteration,
# bloating output and slowing runs (BindableProperty readonly errors).
EXCLUDED_BENCHMARKS = [
    '*MauiLoggerWithLoggerMinLevelErrorBenchmarker*',
]


_FRAMEWORK_BRANCH_RE = re.compile(r'^(net\d+\.\d+)')


def get_branch(framework: str) -> str:
    '''Map framework moniker to MAUI repo branch.

    Strips OS/architecture suffixes so values like "net8.0-windows" and
    "net11.0-windows10.0.19041.0" map to "net8.0" / "net11.0".
    Falls back to "net11.0" for unrecognised input.
    '''
    if framework:
        m = _FRAMEWORK_BRANCH_RE.match(framework)
        if m:
            return m.group(1)
    return 'net11.0'


def _find_git() -> Optional[str]:
    '''Find the git executable on PATH or at common Windows locations.'''
    git = shutil.which('git')
    if git:
        return git

    if iswin():
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
    getLogger().info(f'Using git at: {git}')

    RunCommand([
        git, 'clone',
        '-c', 'core.longpaths=true',
        '--depth', '1',
        '--filter=blob:none',
        '--sparse',
        '--branch', branch,
        MAUI_REPO_URL,
        repo_dir,
    ], verbose=True).run()

    RunCommand(
        [git, 'sparse-checkout', 'set'] + MAUI_SPARSE_CHECKOUT_DIRS,
        verbose=True).run(repo_dir)


def _zip_download(branch: str, repo_dir: str):
    '''Download the repo as a zip archive and extract needed directories.

    Fallback when git is not available (e.g. Helix work items where git is
    not on PATH and not installed).

    Uses curl.exe (built into Windows 10+) for the download because Python's
    bundled SSL certificates may not include the CA certs trusted by the
    machine (common on Helix/corporate environments).
    '''
    log = getLogger()
    # URL-encode the branch so refs like "release/10.0" round-trip correctly.
    archive_url = (
        'https://github.com/dotnet/maui/archive/refs/heads/'
        f'{urllib.parse.quote(branch, safe="")}.zip'
    )
    zip_path = 'maui_download.zip'

    log.info(f'git not found — downloading archive from {archive_url}')

    # Use curl.exe (ships with Windows 10+/Server 2016+) which uses the
    # Windows certificate store, avoiding Python SSL cert issues on Helix.
    curl = shutil.which('curl') or shutil.which('curl.exe')
    if curl:
        RunCommand([curl, '-L', '-o', zip_path, '--fail', '-s', '-S', archive_url],
                   verbose=True).run()
    else:
        # Last resort: try urllib with default certs
        urllib.request.urlretrieve(archive_url, zip_path)

    log.info(f'Downloaded {os.path.getsize(zip_path) / (1024*1024):.1f} MB')

    os.makedirs(repo_dir, exist_ok=True)
    repo_root_real = os.path.realpath(repo_dir)

    # Directories to extract (sparse checkout equivalent + root-level files).
    # Also include files directly in parent directories of sparse dirs
    # (e.g. src/MultiTargeting.targets, src/PublicAPI.targets) since git
    # sparse-checkout includes parent-level files automatically.
    sparse_prefixes = [d.rstrip('/') + '/' for d in MAUI_SPARSE_CHECKOUT_DIRS]
    parent_dirs = set()
    for d in MAUI_SPARSE_CHECKOUT_DIRS:
        parts = d.strip('/').split('/')
        for i in range(1, len(parts)):
            parent_dirs.add('/'.join(parts[:i]) + '/')
    parent_dirs = list(parent_dirs)  # e.g. ['src/']

    with zipfile.ZipFile(zip_path) as zf:
        # GitHub archives have a top-level dir like "maui-net11.0/"
        top_dir = zf.namelist()[0].split('/')[0] + '/'

        for member in zf.namelist():
            if not member.startswith(top_dir):
                continue
            rel_path = member[len(top_dir):]
            if not rel_path:
                continue

            # Include: root-level files, sparse directories, and files
            # directly in parent directories (not recursing into subdirs)
            is_root_file = '/' not in rel_path
            in_sparse_dir = any(rel_path.startswith(p) for p in sparse_prefixes)
            in_parent_dir = any(
                rel_path.startswith(p) and '/' not in rel_path[len(p):]
                for p in parent_dirs
            )

            if not is_root_file and not in_sparse_dir and not in_parent_dir:
                continue

            target = os.path.join(repo_dir, rel_path)
            # Zip-slip guard: refuse to write outside repo_dir even if a
            # malicious archive contains entries with ../ or absolute paths.
            target_real = os.path.realpath(target)
            if not (target_real == repo_root_real or
                    target_real.startswith(repo_root_real + os.sep)):
                log.warning(f'Skipping zip entry outside repo_dir: {member}')
                continue

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
    RunCommand(['dotnet', 'tool', 'restore'], verbose=True).run(repo_dir)

    slnf_path = os.path.join(repo_dir, MAUI_BUILD_SOLUTION_FILTER)
    if not os.path.exists(slnf_path):
        raise FileNotFoundError(
            f'Expected MAUI build solution filter not found: {slnf_path}. '
            f'The dotnet/maui branch layout may have changed; update '
            f'MAUI_BUILD_SOLUTION_FILTER or MAUI_SPARSE_CHECKOUT_DIRS.')

    log.info(f'Building {MAUI_BUILD_SOLUTION_FILTER} (desktop TFMs only)...')
    RunCommand([
        'dotnet', 'build',
        MAUI_BUILD_SOLUTION_FILTER,
        '-c', 'Release',
    ], verbose=True).run(repo_dir)

    log.info('MAUI dependencies built successfully.')


def parse_args():
    parser = ArgumentParser(
        description='Run MAUI desktop BDN benchmarks',
        epilog='Any unrecognized arguments are forwarded to BenchmarkDotNet '
               '(e.g. --filter MyBenchmark*).')
    parser.add_argument('--framework', '-f', default='net11.0',
                        help='Target .NET framework (determines MAUI repo branch)')
    parser.add_argument('--suite', choices=['core', 'xaml', 'graphics', 'all'],
                        default='all', help='Which benchmark suite to run')
    parser.add_argument('--upload-to-perflab-container', action='store_true',
                        help='Upload results to perflab container')
    # Forward unknown args to BenchmarkDotNet.  argparse's nargs='*' would
    # not capture values that start with '--' (BDN flags), so use
    # parse_known_args() instead and pass the remainder through.
    return parser.parse_known_args()


if __name__ == '__main__':
    setup_loggers(True)
    args, extra_bdn_args = parse_args()

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

    # Run the generic BDN workflow.  Forward any args we didn't consume
    # (e.g. --filter, --maxIterationCount overrides) to BenchmarkDotNet.
    bdn_args = list(extra_bdn_args)
    if EXCLUDED_BENCHMARKS:
        bdn_args.extend(['--exclusion-filter'] + EXCLUDED_BENCHMARKS)
    helper.runtests(args.suite, bdn_args, args.upload_to_perflab_container)
