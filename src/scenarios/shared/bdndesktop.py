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
import xml.etree.ElementTree as ET
from logging import getLogger
from subprocess import CalledProcessError
from performance.common import (
    RunCommand,
    get_repo_root_path,
    helixpayload,
    helixuploadroot,
    runninginlab,
)


# Default BDN run arguments.  Tuned to match RecommendedConfig: short
# warmup, capped iterations, 250ms iteration time.  Override per-helper via
# the bdn_run_params constructor argument.
DEFAULT_BDN_RUN_PARAMS = [
    '--filter', '*',
    '--warmupCount', '1',
    '--minIterationCount', '15',
    '--maxIterationCount', '20',
    '--iterationTime', '250',
]


class BDNDesktopHelper(object):
    '''Generic helper for running BDN desktop benchmarks from a local repo checkout.

    Args:
        repo_dir:            Path to the cloned repository root.  Stored as
                             an absolute path so subprocess cwd is stable
                             regardless of cwd changes by the caller.
        benchmark_projects:  Dict mapping suite name to csproj relative path
                             (e.g. {'graphics': 'src/Graphics/.../Graphics.Benchmarks.csproj'}).
        disable_props:       Optional dict of MSBuild property names to replacement values
                             to patch in Directory.Build.props (e.g. disable mobile TFMs).
        bdn_run_params:      Optional list of BDN command-line arguments used
                             when running each suite.  Defaults to
                             DEFAULT_BDN_RUN_PARAMS.
        strict_build:        If True, fail the run when *any* listed suite
                             fails to build.  Default False preserves the
                             tolerant behaviour (skip broken suites, run the
                             rest) which is convenient while iterating.
    '''

    def __init__(self, repo_dir: str, benchmark_projects: dict,
                 disable_props: dict = None,
                 bdn_run_params: list = None,
                 strict_build: bool = False):
        self.repo_dir = os.path.abspath(repo_dir)
        self.benchmark_projects = benchmark_projects
        self.disable_props = disable_props or {}
        self.bdn_run_params = list(bdn_run_params) if bdn_run_params else list(DEFAULT_BDN_RUN_PARAMS)
        self.strict_build = strict_build

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
        correlation = helixpayload()
        if correlation:
            candidate = os.path.join(
                correlation, 'performance', 'src', 'harness',
                'BenchmarkDotNet.Extensions', 'BenchmarkDotNet.Extensions.csproj')
        else:
            # Resolve against the performance repo root via the shared
            # helper so this works no matter where bdndesktop.py lives.
            candidate = os.path.join(
                get_repo_root_path(), 'src', 'harness',
                'BenchmarkDotNet.Extensions', 'BenchmarkDotNet.Extensions.csproj')

        if not os.path.exists(candidate):
            raise FileNotFoundError(
                f'BenchmarkDotNet.Extensions.csproj not found at {candidate}. '
                f'HELIX_CORRELATION_PAYLOAD={correlation!r}')

        getLogger().info(f'BDN.Extensions located at: {candidate}')
        return candidate

    def _inject_bdn_extensions(self, csproj_path: str, bdn_ext_abs: str):
        '''Add a ProjectReference to BDN.Extensions and remove existing BDN PackageRef.

        Idempotent: removes any prior ItemGroup with Label="PerfLabInjected"
        before adding the new one so re-running against an existing
        checkout does not accumulate duplicate ProjectReference entries
        (which produce build errors).
        '''
        log = getLogger()
        log.info(f'Injecting BDN.Extensions reference into {os.path.basename(csproj_path)}')

        csproj_dir = os.path.dirname(os.path.abspath(csproj_path))
        bdn_ext_rel = os.path.relpath(bdn_ext_abs, csproj_dir)

        tree = ET.parse(csproj_path)
        root = tree.getroot()

        ns = ''
        if root.tag.startswith('{'):
            ns = root.tag.split('}')[0] + '}'

        # Remove any prior PerfLabInjected ItemGroup so re-runs don't
        # accumulate duplicate ProjectReferences.
        existing_injected = [ig for ig in root.findall(f'{ns}ItemGroup')
                             if ig.get('Label') == 'PerfLabInjected']
        for ig in existing_injected:
            root.remove(ig)
        if existing_injected:
            log.info(f'  Removed {len(existing_injected)} prior PerfLabInjected ItemGroup(s)')

        # Remove BDN package references that conflict with the injected
        # ProjectReference to BenchmarkDotNet.Extensions (which itself
        # references BenchmarkDotNet + BenchmarkDotNet.Annotations as
        # ProjectReferences).  Leaving them as PackageReferences in the
        # benchmark csproj would pull in a different (likely older) BDN
        # version and produce duplicate-type errors at build time.
        # Optional sub-packages (e.g. BenchmarkDotNet.Diagnostics.*) are
        # left in place because BDN.Extensions does not reference them.
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
            'using System.Collections.Generic;',
            'using System.IO;',
            'using System.Linq;',
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
            'var argsList = args.ToList();\n'
            '            argsList = CommandLineOptions.ParseAndRemoveStringsParameter(\n'
            '                argsList, "--exclusion-filter", out var exclusionFilterValue);\n'
            '            var config = ManualConfig.Create(DefaultConfig.Instance)\n'
            '                .WithArtifactsPath(Path.Combine(\n'
            '                    Path.GetDirectoryName(typeof(Program).Assembly.Location),\n'
            '                    "BenchmarkDotNet.Artifacts"))\n'
            '                .AddFilter(new ExclusionFilter(exclusionFilterValue));\n'
            '            if (Environment.GetEnvironmentVariable("PERFLAB_INLAB") == "1")\n'
            '                config = config.AddExporter(new PerfLabExporter());\n'
            '            BenchmarkSwitcher\n'
            '                .FromAssembly(typeof(Program).Assembly)\n'
            '                .Run(argsList.ToArray(), config);'
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
            raise RuntimeError(
                f'Could not find a BenchmarkSwitcher.Run pattern in '
                f'{program_cs_path}. PerfLabExporter would not be wired up '
                f'and the run would silently produce no perf-lab-report.json. '
                f'Update _patch_program_cs patterns to match this file.')

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
            try:
                RunCommand([
                    'dotnet', 'build',
                    csproj_rel,
                    '-c', 'Release',
                ], verbose=True).run(self.repo_dir)
                built.add(name)
            except CalledProcessError as e:
                log.warning(f'Build failed for {name} (exit code {e.returncode}) — skipping this suite')

        if built:
            log.info(f'Successfully built: {", ".join(sorted(built))}')
        else:
            log.error('No benchmark projects built successfully.')
            sys.exit(1)

        failed = set(name for name, _ in projects) - built
        if failed:
            msg = f'The following suites failed to build: {", ".join(sorted(failed))}'
            if self.strict_build:
                log.error(msg)
                sys.exit(1)
            log.warning(f'{msg} — skipping (strict_build=False)')

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
        ] + list(self.bdn_run_params) + extra_bdn_args

        try:
            RunCommand(cmd, verbose=True).run(self.repo_dir)
        except CalledProcessError as e:
            log.error(f'Benchmark suite {name} failed with exit code {e.returncode}')
            return False

        log.info(f'Benchmark suite {name} completed successfully.')
        return True

    # ── Result collection ───────────────────────────────────────────────────

    def _collect_results(self, upload_to_perflab_container: bool):
        '''Collect perf-lab-report.json files from BDN artifacts.

        Mirrors the canonical pattern in scripts/benchmarks_ci.py: copy each
        per-suite report to HELIX_WORKITEM_UPLOAD_ROOT under its basename
        (BDN already namespaces reports by project name) and write a
        combined-perf-lab-report.json directly into the upload root.
        '''
        log = getLogger()
        upload_root = helixuploadroot()

        reports_globpath = os.path.join(self.repo_dir, '**', '*perf-lab-report.json')
        report_files = glob.glob(reports_globpath, recursive=True)

        if not report_files:
            log.warning('No *perf-lab-report.json files found. '
                        'PerfLabExporter may not have been active (PERFLAB_INLAB not set?).')
            return

        log.info(f'Found {len(report_files)} perf-lab-report.json file(s)')

        if upload_root is not None:
            # Copy individual reports.  BDN names each artifact after the
            # project + benchmark type, so basenames are unique across our
            # suites in practice; warn if that ever stops being true.
            seen = set()
            for report_file in report_files:
                basename = os.path.basename(report_file)
                if basename in seen:
                    log.warning(f'  Basename collision in upload root, overwriting: {basename}')
                seen.add(basename)
                shutil.copy(report_file, os.path.join(upload_root, basename))
                log.info(f'  Copied {basename} to upload root')

            # Write the combined report directly to the upload root (no
            # local intermediate file to clean up afterwards).
            combined_path = os.path.join(upload_root, 'combined-perf-lab-report.json')
            with open(combined_path, 'w', encoding='utf-8') as out:
                combined = []
                for report_file in report_files:
                    try:
                        with open(report_file, 'r', encoding='utf-8') as f:
                            data = json.load(f)
                            if isinstance(data, list):
                                combined.extend(data)
                            else:
                                combined.append(data)
                    except (json.JSONDecodeError, IOError) as e:
                        log.warning(f'  Failed to read {report_file}: {e}')
                json.dump(combined, out)
            log.info(f'Combined report: {combined_path} ({len(combined)} result(s))')
        else:
            log.info('HELIX_WORKITEM_UPLOAD_ROOT not set — skipping upload-root copy.')

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
