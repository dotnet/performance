'''
Helper/Runner for desktop BenchmarkDotNet scenarios that build from external repos.

Currently supports MAUI desktop benchmarks from dotnet/maui. The pattern
(clone → patch → build → run BDN → collect results) can be extended to
other repos by passing different configuration.
'''
import os
import re
import sys
import glob
import json
import shutil
import subprocess
import xml.etree.ElementTree as ET
from logging import getLogger
from performance.common import runninginlab
from shared.const import TRACEDIR

# ── Default MAUI configuration ──────────────────────────────────────────────

MAUI_REPO_URL = 'https://github.com/dotnet/maui.git'

MAUI_BENCHMARK_PROJECTS = {
    'core': 'src/Core/tests/Benchmarks/Core.Benchmarks.csproj',
    'xaml': 'src/Controls/tests/Xaml.Benchmarks/Microsoft.Maui.Controls.Xaml.Benchmarks.csproj',
    'graphics': 'src/Graphics/tests/Graphics.Benchmarks/Graphics.Benchmarks.csproj',
}

MAUI_BUILD_SOLUTION_FILTER = 'Microsoft.Maui.BuildTasks.slnf'

MAUI_SPARSE_CHECKOUT_DIRS = [
    'src/Core', 'src/Controls', 'src/Graphics', 'src/SingleProject',
    'src/Workload', 'src/Essentials',
    'eng', '.config',
]

# MSBuild properties to disable non-desktop target frameworks.
# Used by _patch_directory_build_props() to replace true→false in-place
# so ALL builds (including BDN's internal auto-generated project builds)
# are desktop-only.
DESKTOP_ONLY_PROPS = {
    'IncludeAndroidTargetFrameworks': 'false',
    'IncludeIosTargetFrameworks': 'false',
    'IncludeMacCatalystTargetFrameworks': 'false',
    'IncludeMacOSTargetFrameworks': 'false',
    'IncludeTizenTargetFrameworks': 'false',
}


class BDNDesktopHelper(object):

    def __init__(self):
        self.repo_dir = 'maui_repo'

    # ── Public entry point ──────────────────────────────────────────────────

    def runtests(self, framework: str, suite: str, bdn_args: list,
                 upload_to_perflab_container: bool):
        '''
        Full lifecycle: clone dotnet/maui, build dependencies, patch benchmark
        projects for PerfLabExporter, run BDN suites, and collect results.
        '''
        log = getLogger()
        branch = self._get_branch(framework)

        # Setup
        self._clone_repo(branch)
        self._patch_directory_build_props()
        self._build_dependencies()
        self._patch_benchmark_projects()
        self._build_benchmark_projects(suite)

        # Run
        if suite == 'all':
            suites = MAUI_BENCHMARK_PROJECTS.items()
        else:
            suites = [(suite, MAUI_BENCHMARK_PROJECTS[suite])]

        all_passed = True
        for name, csproj_rel in suites:
            if not self._run_benchmark(name, csproj_rel, bdn_args):
                all_passed = False

        # Collect
        self._collect_results(upload_to_perflab_container)

        if not all_passed:
            log.error('One or more benchmark suites failed.')
            sys.exit(1)

        log.info('All benchmark suites completed.')

    # ── Branch mapping ──────────────────────────────────────────────────────

    @staticmethod
    def _get_branch(framework: str) -> str:
        if framework and framework.startswith('net'):
            return framework  # net11.0 -> net11.0, net10.0 -> net10.0
        return 'net11.0'

    # ── Clone & build ───────────────────────────────────────────────────────

    def _clone_repo(self, branch: str):
        log = getLogger()
        log.info(f'Cloning dotnet/maui branch {branch} (sparse, depth 1)...')

        if os.path.exists(self.repo_dir):
            shutil.rmtree(self.repo_dir)

        subprocess.run([
            'git', 'clone',
            '-c', 'core.longpaths=true',
            '--depth', '1',
            '--filter=blob:none',
            '--sparse',
            '--branch', branch,
            MAUI_REPO_URL,
            self.repo_dir
        ], check=True)

        subprocess.run(
            ['git', 'sparse-checkout', 'set'] + MAUI_SPARSE_CHECKOUT_DIRS,
            cwd=self.repo_dir, check=True)

        log.info('Clone complete.')

    def _patch_directory_build_props(self):
        '''Disable non-desktop TFMs in Directory.Build.props.

        MAUI's props set Include*TargetFrameworks to true at multiple points.
        MauiPlatforms (which controls TargetFrameworks) is computed from these.
        We must replace ALL true→false in-place so the platform lists are never
        populated.
        '''
        log = getLogger()
        props_path = os.path.join(self.repo_dir, 'Directory.Build.props')
        log.info('Patching Directory.Build.props to disable non-desktop TFMs...')

        with open(props_path, 'r', encoding='utf-8-sig') as f:
            content = f.read()

        for prop_name in DESKTOP_ONLY_PROPS:
            pattern = rf'(<{prop_name}\b[^>]*>)true(</{prop_name}>)'
            content, count = re.subn(pattern, r'\g<1>false\g<2>', content)
            if count > 0:
                log.info(f'  {prop_name}: replaced {count} occurrence(s)')

        with open(props_path, 'w', encoding='utf-8') as f:
            f.write(content)

        log.info('  Directory.Build.props patched.')

    def _build_dependencies(self):
        log = getLogger()
        log.info('Restoring dotnet tools...')
        subprocess.run(['dotnet', 'tool', 'restore'], cwd=self.repo_dir, check=True)

        log.info(f'Building {MAUI_BUILD_SOLUTION_FILTER} (desktop TFMs only)...')
        subprocess.run([
            'dotnet', 'build',
            MAUI_BUILD_SOLUTION_FILTER,
            '-c', 'Release',
        ], cwd=self.repo_dir, check=True)

        log.info('MAUI dependencies built successfully.')

    # ── BDN.Extensions injection ────────────────────────────────────────────

    def _find_bdn_extensions(self) -> str:
        '''Return the absolute path to BenchmarkDotNet.Extensions.csproj.'''
        correlation = os.environ.get('HELIX_CORRELATION_PAYLOAD', '')
        if correlation:
            candidate = os.path.join(
                correlation, 'performance', 'src', 'harness',
                'BenchmarkDotNet.Extensions', 'BenchmarkDotNet.Extensions.csproj')
        else:
            scenario_dir = os.path.dirname(os.path.abspath(__file__))
            candidate = os.path.normpath(os.path.join(
                scenario_dir, '..', '..', 'harness',
                'BenchmarkDotNet.Extensions', 'BenchmarkDotNet.Extensions.csproj'))

        if not os.path.exists(candidate):
            raise FileNotFoundError(
                f'BenchmarkDotNet.Extensions.csproj not found at {candidate}. '
                f'HELIX_CORRELATION_PAYLOAD={correlation!r}')

        getLogger().info(f'BDN.Extensions located at: {candidate}')
        return candidate

    def _inject_bdn_extensions(self, csproj_path: str, bdn_ext_abs: str):
        '''Add a ProjectReference to BDN.Extensions and remove existing BDN PackageRef.'''
        log = getLogger()
        log.info(f'Injecting BDN.Extensions reference into {os.path.basename(csproj_path)}')

        csproj_dir = os.path.dirname(os.path.abspath(csproj_path))
        bdn_ext_rel = os.path.relpath(bdn_ext_abs, csproj_dir)

        tree = ET.parse(csproj_path)
        root = tree.getroot()

        ns = ''
        if root.tag.startswith('{'):
            ns = root.tag.split('}')[0] + '}'

        # Remove existing BenchmarkDotNet PackageReference to avoid version conflicts
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

    def _patch_program_cs(self, program_cs_path: str):
        '''Patch Program.cs to add PerfLabExporter via ManualConfig.'''
        log = getLogger()
        log.info(f'Patching {os.path.basename(program_cs_path)} for PerfLabExporter')

        with open(program_cs_path, 'r', encoding='utf-8-sig') as f:
            content = f.read()

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

        # ManualConfig without MandatoryCategoryValidator (MAUI benchmarks
        # don't use [BenchmarkCategory])
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
            log.warning('  Could not find BenchmarkSwitcher.Run pattern — may need manual patching')
            return

        with open(program_cs_path, 'w', encoding='utf-8') as f:
            f.write(content)

        log.info('  Patched successfully.')

    def _patch_benchmark_projects(self):
        '''Inject BDN.Extensions and PerfLabExporter into all benchmark projects.'''
        bdn_ext_abs = self._find_bdn_extensions()

        for name, csproj_rel in MAUI_BENCHMARK_PROJECTS.items():
            csproj_path = os.path.join(self.repo_dir, csproj_rel)
            project_dir = os.path.dirname(csproj_path)
            program_cs = os.path.join(project_dir, 'Program.cs')

            if not os.path.exists(csproj_path):
                getLogger().warning(f'Benchmark project not found: {csproj_path}')
                continue

            self._inject_bdn_extensions(csproj_path, bdn_ext_abs)

            if os.path.exists(program_cs):
                self._patch_program_cs(program_cs)
            else:
                getLogger().warning(f'Program.cs not found for {name}')

    # ── Build benchmarks ────────────────────────────────────────────────────

    def _build_benchmark_projects(self, suite: str):
        log = getLogger()
        if suite == 'all':
            projects = MAUI_BENCHMARK_PROJECTS.items()
        else:
            projects = [(suite, MAUI_BENCHMARK_PROJECTS[suite])]

        for name, csproj_rel in projects:
            csproj_path = os.path.join(self.repo_dir, csproj_rel)
            if not os.path.exists(csproj_path):
                continue

            log.info(f'Building benchmark: {name}')
            subprocess.run([
                'dotnet', 'build',
                csproj_rel,
                '-c', 'Release',
            ], cwd=self.repo_dir, check=True)

        log.info('All benchmark projects built successfully.')

    # ── Run benchmarks ──────────────────────────────────────────────────────

    def _run_benchmark(self, name: str, csproj_rel: str, extra_bdn_args: list) -> bool:
        log = getLogger()
        csproj_path = os.path.join(self.repo_dir, csproj_rel)
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

        result = subprocess.run(cmd, cwd=self.repo_dir)
        if result.returncode != 0:
            log.error(f'Benchmark suite {name} failed with exit code {result.returncode}')
            return False

        log.info(f'Benchmark suite {name} completed successfully.')
        return True

    # ── Result collection ───────────────────────────────────────────────────

    def _collect_results(self, upload_to_perflab_container: bool):
        '''Collect perf-lab-report.json files from BDN artifacts.'''
        log = getLogger()
        upload_root = os.environ.get('HELIX_WORKITEM_UPLOAD_ROOT', '')

        report_pattern = os.path.join(self.repo_dir, '**', '*-perf-lab-report.json')
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

        # Also upload via perflab container if requested
        if upload_to_perflab_container and runninginlab():
            try:
                from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_QUEUE
                import upload
                globpath = os.path.join(self.repo_dir, '**', '*perf-lab-report.json')
                upload_code = upload.upload(globpath, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_STORAGE_URI)
                log.info(f'BDN Desktop Benchmarks Upload Code: {upload_code}')
                if upload_code != 0:
                    sys.exit(upload_code)
            except ImportError:
                log.warning('Upload module not available — skipping perflab container upload')
