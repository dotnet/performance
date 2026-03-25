'''
Reusable helper for running BenchmarkDotNet benchmarks from external repos
on desktop.

Handles the generic workflow:
  1. Patch Directory.Build.props to disable unwanted target frameworks
  2. Inject BDN.Extensions (PerfLabExporter) into benchmark projects
  3. Build benchmark projects
  4. Run BDN suites
  5. Collect and upload results

Callers (e.g. test.py in each scenario) are responsible for repo-specific
setup such as cloning, branch selection, and building repo dependencies.
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


class BDNDesktopHelper(object):
    '''Generic helper for running BDN desktop benchmarks from a local repo checkout.

    Args:
        repo_dir:            Path to the cloned repository root.
        benchmark_projects:  Dict mapping suite name to csproj relative path
                             (e.g. {'graphics': 'src/Graphics/.../Graphics.Benchmarks.csproj'}).
        disable_props:       Optional dict of MSBuild property names to replacement values
                             to patch in Directory.Build.props (e.g. disable mobile TFMs).
    '''

    def __init__(self, repo_dir: str, benchmark_projects: dict,
                 disable_props: dict = None):
        self.repo_dir = repo_dir
        self.benchmark_projects = benchmark_projects
        self.disable_props = disable_props or {}

    # ── Public entry point ──────────────────────────────────────────────────

    def runtests(self, suite: str, bdn_args: list,
                 upload_to_perflab_container: bool):
        '''
        Patch benchmark projects, build, run, and collect BDN results.

        Assumes the caller has already:
          - Cloned the repo and built repo-specific dependencies
          - Called patch_directory_build_props() if needed (must happen
            before any builds, including dependency builds)
        '''
        log = getLogger()

        # Patch benchmark csprojs + Program.cs for PerfLabExporter
        self.patch_benchmark_projects()

        # Build
        built_suites = self.build_benchmark_projects(suite)

        # Run (only suites that built successfully)
        if suite == 'all':
            suites = [(n, p) for n, p in self.benchmark_projects.items() if n in built_suites]
        else:
            suites = [(suite, self.benchmark_projects[suite])]

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

    # ── Patch Directory.Build.props ─────────────────────────────────────────

    def patch_directory_build_props(self):
        '''Disable unwanted TFMs by replacing property values in-place.

        Handles repos (like MAUI) that set Include*TargetFrameworks=true at
        multiple points before computing TargetFrameworks.  Appending
        overrides at the end doesn't work because MSBuild evaluates
        top-to-bottom, so we regex-replace ALL occurrences in-place.
        '''
        log = getLogger()
        props_path = os.path.join(self.repo_dir, 'Directory.Build.props')
        if not os.path.exists(props_path):
            log.info('No Directory.Build.props found — skipping TFM patching.')
            return

        log.info('Patching Directory.Build.props to disable unwanted TFMs...')

        with open(props_path, 'r', encoding='utf-8-sig') as f:
            content = f.read()

        for prop_name, new_value in self.disable_props.items():
            pattern = rf'(<{prop_name}\b[^>]*>)\s*true\s*(</{prop_name}>)'
            content, count = re.subn(pattern, rf'\g<1>{new_value}\g<2>', content)
            if count > 0:
                log.info(f'  {prop_name}: replaced {count} occurrence(s)')
            else:
                log.warning(f'  {prop_name}: no occurrences of "true" found to replace')

        with open(props_path, 'w', encoding='utf-8') as f:
            f.write(content)

        log.info('  Directory.Build.props patched.')

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

        # Remove the primary BenchmarkDotNet PackageReference to avoid version conflicts.
        # Only remove exact 'BenchmarkDotNet' to preserve optional subpackages
        # (e.g. BenchmarkDotNet.Annotations, BenchmarkDotNet.Diagnostics.*).
        bdn_packages_to_remove = {'BenchmarkDotNet', 'BenchmarkDotNet.Annotations'}
        for item_group in root.findall(f'{ns}ItemGroup'):
            for pkg_ref in item_group.findall(f'{ns}PackageReference'):
                include = pkg_ref.get('Include', '')
                if include in bdn_packages_to_remove:
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

        # ManualConfig without MandatoryCategoryValidator (external benchmarks
        # may not use [BenchmarkCategory])
        new_run_call = (
            'var config = ManualConfig.Create(DefaultConfig.Instance)\n'
            '                .WithArtifactsPath(Path.Combine(\n'
            '                    Path.GetDirectoryName(typeof(Program).Assembly.Location),\n'
            '                    "BenchmarkDotNet.Artifacts"));\n'
            '            if (Environment.GetEnvironmentVariable("PERFLAB_INLAB") == "1")\n'
            '                config = config.AddExporter(new PerfLabExporter());\n'
            '            BenchmarkSwitcher\n'
            '                .FromAssembly(typeof(Program).Assembly)\n'
            '                .Run(args, config);'
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
                content = content.replace(pattern, new_run_call)
                replaced = True
                break

        if not replaced:
            log.warning('  Could not find BenchmarkSwitcher.Run pattern — may need manual patching')
            return

        with open(program_cs_path, 'w', encoding='utf-8') as f:
            f.write(content)

        log.info('  Patched successfully.')

    def patch_benchmark_projects(self):
        '''Inject BDN.Extensions and PerfLabExporter into all benchmark projects.'''
        bdn_ext_abs = self._find_bdn_extensions()

        for name, csproj_rel in self.benchmark_projects.items():
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

    def build_benchmark_projects(self, suite: str) -> set:
        '''Build benchmark projects. Returns the set of suite names that built successfully.'''
        log = getLogger()
        if suite == 'all':
            projects = list(self.benchmark_projects.items())
        else:
            projects = [(suite, self.benchmark_projects[suite])]

        built = set()
        for name, csproj_rel in projects:
            csproj_path = os.path.join(self.repo_dir, csproj_rel)
            if not os.path.exists(csproj_path):
                log.warning(f'Benchmark project not found, skipping: {csproj_path}')
                continue

            log.info(f'Building benchmark: {name}')
            result = subprocess.run([
                'dotnet', 'build',
                csproj_rel,
                '-c', 'Release',
            ], cwd=self.repo_dir)

            if result.returncode == 0:
                built.add(name)
            else:
                log.warning(f'Build failed for {name} (exit code {result.returncode}) — skipping this suite')

        if built:
            log.info(f'Successfully built: {", ".join(sorted(built))}')
        else:
            log.error('No benchmark projects built successfully.')
            sys.exit(1)

        failed = set(name for name, _ in projects) - built
        if failed:
            log.warning(f'WARNING: The following suites failed to build and will be skipped: {", ".join(sorted(failed))}')

        return built

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

        report_pattern = os.path.join(self.repo_dir, '**', '*perf-lab-report.json')
        report_files = glob.glob(report_pattern, recursive=True)

        if not report_files:
            log.warning('No *perf-lab-report.json files found. '
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
