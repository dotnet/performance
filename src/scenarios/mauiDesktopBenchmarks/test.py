'''
Test runner for MAUI Desktop BenchmarkDotNet benchmarks.

Handles the full lifecycle: clone dotnet/maui, build dependencies, patch
benchmark projects for PerfLabExporter, run BDN suites, and collect results.
All heavy work lives here (not in pre.py) to keep the correlation payload small.
'''
import os
import sys
import glob
import json
import shutil
import subprocess
import xml.etree.ElementTree as ET
from performance.logger import setup_loggers, getLogger

setup_loggers(True)
log = getLogger(__name__)

MAUI_REPO_DIR = 'maui_repo'
MAUI_REPO_URL = 'https://github.com/dotnet/maui.git'

BENCHMARK_PROJECTS = {
    'core': 'src/Core/tests/Benchmarks/Core.Benchmarks.csproj',
    'xaml': 'src/Controls/tests/Xaml.Benchmarks/Microsoft.Maui.Controls.Xaml.Benchmarks.csproj',
    'graphics': 'src/Graphics/tests/Graphics.Benchmarks/Graphics.Benchmarks.csproj',
}

# MSBuild properties to disable non-desktop target frameworks.
# Used by patch_directory_build_props() to replace true→false in-place
# in MAUI's Directory.Build.props. This ensures ALL builds (including BDN's
# internal auto-generated project builds) are desktop-only.
DESKTOP_ONLY_PROPS = {
    'IncludeAndroidTargetFrameworks': 'false',
    'IncludeIosTargetFrameworks': 'false',
    'IncludeMacCatalystTargetFrameworks': 'false',
    'IncludeMacOSTargetFrameworks': 'false',
    'IncludeTizenTargetFrameworks': 'false',
}

# ── Branch mapping ──────────────────────────────────────────────────────────

def get_maui_branch(framework: str) -> str:
    """Map .NET framework version to MAUI repo branch name."""
    if framework and framework.startswith('net'):
        return framework  # net11.0 -> net11.0, net10.0 -> net10.0
    return 'net11.0'

# ── Clone & build ───────────────────────────────────────────────────────────

def clone_maui_repo(branch: str):
    """Sparse-clone the maui repo with only the directories needed for benchmarks."""
    log.info(f'Cloning dotnet/maui branch {branch} (sparse, depth 1)...')

    if os.path.exists(MAUI_REPO_DIR):
        shutil.rmtree(MAUI_REPO_DIR)

    subprocess.run([
        'git', 'clone',
        '-c', 'core.longpaths=true',
        '--depth', '1',
        '--filter=blob:none',
        '--sparse',
        '--branch', branch,
        MAUI_REPO_URL,
        MAUI_REPO_DIR
    ], check=True)

    subprocess.run([
        'git', 'sparse-checkout', 'set',
        'src/Core', 'src/Controls', 'src/Graphics', 'src/SingleProject',
        'src/Workload', 'src/Essentials',
        'eng', '.config'
    ], cwd=MAUI_REPO_DIR, check=True)

    log.info('Clone complete.')


def patch_directory_build_props():
    """Disable non-desktop TFMs in Directory.Build.props.

    MAUI's props set Include*TargetFrameworks to true at multiple points.
    MauiPlatforms (which controls TargetFrameworks) is computed from these.
    We must replace ALL true→false in-place so the platform lists are never
    populated, not just append overrides at the end (too late for evaluation).
    """
    import re
    props_path = os.path.join(MAUI_REPO_DIR, 'Directory.Build.props')
    log.info('Patching Directory.Build.props to disable non-desktop TFMs...')

    with open(props_path, 'r', encoding='utf-8-sig') as f:
        content = f.read()

    for prop_name in DESKTOP_ONLY_PROPS:
        # Replace all occurrences of <PropName ...>true</PropName> with false
        pattern = rf'(<{prop_name}\b[^>]*>)true(</{prop_name}>)'
        content, count = re.subn(pattern, r'\g<1>false\g<2>', content)
        if count > 0:
            log.info(f'  {prop_name}: replaced {count} occurrence(s)')

    with open(props_path, 'w', encoding='utf-8') as f:
        f.write(content)

    log.info('  Directory.Build.props patched.')


def build_maui_dependencies():
    """Build MAUI BuildTasks solution filter — the core libraries benchmarks depend on."""
    log.info('Restoring dotnet tools...')
    subprocess.run(['dotnet', 'tool', 'restore'], cwd=MAUI_REPO_DIR, check=True)

    log.info('Building Microsoft.Maui.BuildTasks.slnf (desktop TFMs only)...')
    subprocess.run([
        'dotnet', 'build',
        'Microsoft.Maui.BuildTasks.slnf',
        '-c', 'Release',
    ], cwd=MAUI_REPO_DIR, check=True)

    log.info('MAUI dependencies built successfully.')

# ── BDN.Extensions injection ────────────────────────────────────────────────

def _find_bdn_extensions() -> str:
    """
    Return the absolute path to BenchmarkDotNet.Extensions.csproj.
    On Helix the full perf repo lives under the correlation payload;
    for local runs we walk up from this script's location.
    """
    correlation = os.environ.get('HELIX_CORRELATION_PAYLOAD', '')
    if correlation:
        # Helix: repo is at <correlation>/performance/
        candidate = os.path.join(
            correlation, 'performance', 'src', 'harness',
            'BenchmarkDotNet.Extensions', 'BenchmarkDotNet.Extensions.csproj')
    else:
        # Local: navigate from this script's directory
        scenario_dir = os.path.dirname(os.path.abspath(__file__))
        candidate = os.path.normpath(os.path.join(
            scenario_dir, '..', '..', 'harness',
            'BenchmarkDotNet.Extensions', 'BenchmarkDotNet.Extensions.csproj'))

    if not os.path.exists(candidate):
        raise FileNotFoundError(
            f'BenchmarkDotNet.Extensions.csproj not found at {candidate}. '
            f'HELIX_CORRELATION_PAYLOAD={correlation!r}')

    log.info(f'BDN.Extensions located at: {candidate}')
    return candidate


def inject_bdn_extensions(csproj_path: str, bdn_ext_abs: str):
    """Add a ProjectReference to BenchmarkDotNet.Extensions into a benchmark csproj.
    Also removes the existing BenchmarkDotNet PackageReference to avoid version conflicts
    (BDN.Extensions brings in the correct version transitively)."""
    log.info(f'Injecting BDN.Extensions reference into {os.path.basename(csproj_path)}')

    csproj_dir = os.path.dirname(os.path.abspath(csproj_path))
    bdn_ext_rel = os.path.relpath(bdn_ext_abs, csproj_dir)

    tree = ET.parse(csproj_path)
    root = tree.getroot()

    # SDK-style projects typically have no XML namespace
    ns = ''
    if root.tag.startswith('{'):
        ns = root.tag.split('}')[0] + '}'

    # Remove existing BenchmarkDotNet PackageReference to avoid NU1605 downgrade error
    for item_group in root.findall(f'{ns}ItemGroup'):
        for pkg_ref in item_group.findall(f'{ns}PackageReference'):
            include = pkg_ref.get('Include', '')
            if include.startswith('BenchmarkDotNet'):
                item_group.remove(pkg_ref)
                log.info(f'  Removed PackageReference: {include}')

    # Add ProjectReference to BDN.Extensions
    item_group = ET.SubElement(root, f'{ns}ItemGroup')
    item_group.set('Label', 'PerfLabInjected')
    proj_ref = ET.SubElement(item_group, f'{ns}ProjectReference')
    proj_ref.set('Include', bdn_ext_rel)

    tree.write(csproj_path, xml_declaration=True, encoding='utf-8')
    log.info(f'  Added ProjectReference: {bdn_ext_rel}')


def patch_program_cs(program_cs_path: str):
    """Patch Program.cs to add PerfLabExporter via ManualConfig."""
    log.info(f'Patching {os.path.basename(program_cs_path)} for PerfLabExporter')

    with open(program_cs_path, 'r', encoding='utf-8-sig') as f:
        content = f.read()

    # Add required using statements at the top
    usings_to_add = [
        'using BenchmarkDotNet.Configs;',
        'using BenchmarkDotNet.Extensions;',
        'using System;',
        'using System.IO;',
    ]
    insert_block = ''
    for u in usings_to_add:
        if u not in content:
            insert_block += u + '\n'
    if insert_block:
        content = insert_block + content

    # Build a minimal config that adds PerfLabExporter without the
    # MandatoryCategoryValidator (MAUI benchmarks don't use BenchmarkCategory).
    new_run_call = (
        'var config = ManualConfig.Create(DefaultConfig.Instance)\n'
        '                .WithArtifactsPath(Path.Combine(\n'
        '                    Path.GetDirectoryName(typeof(Program).Assembly.Location),\n'
        '                    "BenchmarkDotNet.Artifacts"));\n'
        '            if (Environment.GetEnvironmentVariable("PERFLAB_INLAB") == "1")\n'
        '                config = config.AddExporter(new PerfLabExporter());\n'
        '            BenchmarkSwitcher\n'
        '                .FromAssembly(typeof(Program).Assembly)\n'
        '                .Run(args, config)'
    )

    # Known patterns from MAUI benchmark Program.cs files
    patterns = [
        'BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);',
        'BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)',
        'BenchmarkSwitcher.FromAssembly (typeof (Program).Assembly).Run (args);',
        'BenchmarkSwitcher.FromAssembly (typeof (Program).Assembly).Run (args)',
    ]

    replaced = False
    for pattern in patterns:
        if pattern in content:
            suffix = ';' if pattern.endswith(';') else ''
            content = content.replace(pattern, new_run_call + suffix)
            replaced = True
            break

    if not replaced:
        log.warning(f'  Could not find BenchmarkSwitcher.Run pattern — may need manual patching')
        return

    with open(program_cs_path, 'w', encoding='utf-8') as f:
        f.write(content)

    log.info(f'  Patched successfully.')


def patch_benchmark_projects():
    """Inject BDN.Extensions and RecommendedConfig into all benchmark projects."""
    bdn_ext_abs = _find_bdn_extensions()

    for name, csproj_rel in BENCHMARK_PROJECTS.items():
        csproj_path = os.path.join(MAUI_REPO_DIR, csproj_rel)
        project_dir = os.path.dirname(csproj_path)
        program_cs = os.path.join(project_dir, 'Program.cs')

        if not os.path.exists(csproj_path):
            log.warning(f'Benchmark project not found: {csproj_path}')
            continue

        inject_bdn_extensions(csproj_path, bdn_ext_abs)

        if os.path.exists(program_cs):
            patch_program_cs(program_cs)
        else:
            log.warning(f'Program.cs not found for {name}')

# ── Build benchmarks ────────────────────────────────────────────────────────

def build_benchmark_projects():
    """Build each benchmark project in Release mode."""
    for name, csproj_rel in BENCHMARK_PROJECTS.items():
        csproj_path = os.path.join(MAUI_REPO_DIR, csproj_rel)
        if not os.path.exists(csproj_path):
            continue

        log.info(f'Building benchmark: {name}')
        subprocess.run([
            'dotnet', 'build',
            csproj_rel,
            '-c', 'Release',
        ], cwd=MAUI_REPO_DIR, check=True)

    log.info('All benchmark projects built successfully.')

# ── Run benchmarks ──────────────────────────────────────────────────────────

def run_benchmark(name: str, csproj_rel: str, extra_bdn_args: list):
    """Run a single BDN benchmark suite."""
    csproj_path = os.path.join(MAUI_REPO_DIR, csproj_rel)
    if not os.path.exists(csproj_path):
        log.warning(f'Benchmark project not found: {csproj_path}')
        return False

    log.info(f'Running benchmark suite: {name}')

    cmd = [
        'dotnet', 'run',
        '-c', 'Release',
        '--no-build',
        '--project', csproj_rel,
        '--',
        '--filter', '*',
    ] + extra_bdn_args

    result = subprocess.run(cmd, cwd=MAUI_REPO_DIR)
    if result.returncode != 0:
        log.error(f'Benchmark suite {name} failed with exit code {result.returncode}')
        return False

    log.info(f'Benchmark suite {name} completed successfully.')
    return True

# ── Result collection ───────────────────────────────────────────────────────

def collect_results():
    """
    Collect perf-lab-report.json files from BDN artifacts and copy them
    to HELIX_WORKITEM_UPLOAD_ROOT for the infrastructure to pick up.
    """
    upload_root = os.environ.get('HELIX_WORKITEM_UPLOAD_ROOT', '')

    # Search recursively under maui_repo for perf-lab-report files
    report_pattern = os.path.join(MAUI_REPO_DIR, '**', '*-perf-lab-report.json')
    report_files = glob.glob(report_pattern, recursive=True)

    if not report_files:
        log.warning('No perf-lab-report.json files found. '
                     'PerfLabExporter may not have been active (PERFLAB_INLAB not set?).')
        return

    log.info(f'Found {len(report_files)} perf-lab-report.json file(s)')

    # Combine all reports into a single file
    combined = []
    for report_file in report_files:
        log.info(f'  Collecting: {report_file}')
        try:
            with open(report_file, 'r', encoding='utf-8') as f:
                data = json.load(f)
                if isinstance(data, list):
                    combined.extend(data)
                else:
                    combined.append(data)
        except (json.JSONDecodeError, IOError) as e:
            log.warning(f'  Failed to read {report_file}: {e}')

    if combined:
        combined_path = 'combined-perf-lab-report.json'
        with open(combined_path, 'w', encoding='utf-8') as f:
            json.dump(combined, f, indent=2)
        log.info(f'Combined report: {combined_path} ({len(combined)} result(s))')

        if upload_root:
            dest = os.path.join(upload_root, combined_path)
            shutil.copy2(combined_path, dest)
            log.info(f'Copied combined report to {dest}')

            for report_file in report_files:
                basename = os.path.basename(report_file)
                dest = os.path.join(upload_root, basename)
                shutil.copy2(report_file, dest)
                log.info(f'  Copied {basename} to upload root')

# ── Main ────────────────────────────────────────────────────────────────────

if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description='MAUI Desktop BDN Benchmarks - Test runner')
    parser.add_argument('-f', '--framework', default='net11.0',
                        help='Target .NET framework (determines MAUI branch)')
    parser.add_argument('--suite', choices=['core', 'xaml', 'graphics', 'all'],
                        default='all', help='Which benchmark suite to run')
    parser.add_argument('--bdn-args', nargs='*', default=[],
                        help='Additional arguments to pass to BenchmarkDotNet')
    args = parser.parse_args()

    # ── Setup ───────────────────────────────────────────────────────────────
    branch = get_maui_branch(args.framework)
    clone_maui_repo(branch)
    patch_directory_build_props()
    build_maui_dependencies()
    patch_benchmark_projects()
    build_benchmark_projects()

    # ── Run ─────────────────────────────────────────────────────────────────
    if args.suite == 'all':
        suites = BENCHMARK_PROJECTS.items()
    else:
        suites = [(args.suite, BENCHMARK_PROJECTS[args.suite])]

    all_passed = True
    for name, csproj_rel in suites:
        if not run_benchmark(name, csproj_rel, args.bdn_args):
            all_passed = False

    # ── Collect ─────────────────────────────────────────────────────────────
    collect_results()

    if not all_passed:
        log.error('One or more benchmark suites failed.')
        sys.exit(1)

    log.info('All benchmark suites completed.')
