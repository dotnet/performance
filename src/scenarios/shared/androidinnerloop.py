'''
Helper for Android Inner Loop (build-deploy-startup) measurements.
'''
import subprocess
import os
import json
import re
import sys
from logging import getLogger
from shutil import copytree
from performance.common import RunCommand, runninginlab
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_QUEUE
from shared.startup import StartupWrapper
from shared.util import helixuploaddir, xharness_adb
from shared import const
import upload

class AndroidInnerLoopHelper(object):
    '''
    Measures Android inner-loop build+deploy+startup times.
    '''

    def __init__(self):
        pass

    def _measure_startup(self, packagename, activityname):
        """Measure app startup time (ms). Prefers TotalTime from am start, falls back to logcat."""
        stop_app_cmd = xharness_adb() + ['shell', 'am', 'force-stop', packagename]
        start_app_cmd = xharness_adb() + ['shell', 'am', 'start-activity', '-W', '-S', '-n', activityname]
        clear_logs_cmd = xharness_adb() + ['logcat', '-c']
        retrieve_time_cmd = xharness_adb() + [
            'shell',
            f"logcat -d | grep -E 'ActivityManager|ActivityTaskManager' | grep ': Displayed {activityname}'"
        ]

        RunCommand(clear_logs_cmd, verbose=True).run()
        start_result = RunCommand(start_app_cmd, verbose=True)
        start_result.run()

        startup_ms = None

        # Primary: parse WaitTime or TotalTime from am start -W output
        total_match = re.search(r"TotalTime:\s*(\d+)", start_result.stdout)
        wait_match = re.search(r"WaitTime:\s*(\d+)", start_result.stdout)
        if total_match:
            startup_ms = int(total_match.group(1))
            getLogger().info("Startup time (TotalTime): %d ms" % startup_ms)
        elif wait_match:
            startup_ms = int(wait_match.group(1))
            getLogger().info("Startup time (WaitTime): %d ms" % startup_ms)

        # Fallback: parse 'Displayed' time from logcat (stop app first, matching DEVICESTARTUP pattern)
        if startup_ms is None:
            RunCommand(stop_app_cmd, verbose=True).run()
            retrieve_result = RunCommand(retrieve_time_cmd, verbose=True)
            retrieve_result.run()
            dirty_capture = re.search(r"\+(\d*s?\d+)ms", retrieve_result.stdout)
            if not dirty_capture:
                raise Exception("Failed to capture startup time from am start output or logcat!")
            capture_list = dirty_capture.group(1).split('s')
            if len(capture_list) == 1:
                startup_ms = int(capture_list[0])
            elif len(capture_list) == 2:
                startup_ms = int(capture_list[0]) * 1000 + int(capture_list[1].zfill(3))
            else:
                raise Exception("Android time capture failed! Unexpected format: %s" % dirty_capture.group(0))
            getLogger().info("Startup time (logcat): %d ms" % startup_ms)
        else:
            RunCommand(stop_app_cmd, verbose=True).run()

        return startup_ms

    def _merge_build_and_startup(self, build_report_path, startup_results, final_report_path):
        """Load the build metrics report, append a startup time counter, write to final path."""
        with open(build_report_path, 'r') as f:
            report = json.load(f)
        startup_counter = {
            "name": "Time to Main",
            "topCounter": True,
            "defaultCounter": False,
            "higherIsBetter": False,
            "metricName": "ms",
            "results": startup_results
        }
        # Report structure: { "tests": [ { "counters": [...] } ] }
        report["tests"][0]["counters"].append(startup_counter)
        with open(final_report_path, 'w') as f:
            json.dump(report, f, indent=2)
        getLogger().info("Merged report written to: %s" % final_report_path)

    def run(self, csprojpath, scenarioname, configuration, framework, msbuildargs,
            packagename, innerloopiterations, editsrc, editdest, traits):
        '''
        Runs the full Android inner-loop measurement: first build+deploy, then N
        incremental edit-build-deploy-startup iterations, producing perf-lab reports.
        '''
        if not csprojpath:
            raise Exception("For Android inner loop measurements, --csproj-path must be provided.")
        scenarioprefix = scenarioname or "MAUI Android Build and Deploy"

        os.makedirs(const.TRACEDIR, exist_ok=True)
        first_binlog = os.path.join(const.TRACEDIR, 'first-build-and-deploy.binlog')

        # Build the base MSBuild command.
        base_cmd = ['dotnet', 'build', csprojpath, '-t:Install']
        if configuration:
            base_cmd.extend(['-c', configuration])
        if framework:
            base_cmd.extend(['-f', framework])
        if msbuildargs:
            for arg in re.split(r'[;\s]+', msbuildargs):
                if arg.strip():
                    base_cmd.append(arg.strip())

        # Validate package name early (needed after first deploy for activity resolution)
        if not packagename:
            raise Exception("For Android inner loop measurements, --package-name must be provided.")

        # Step 1: First build+deploy + single startup measurement
        first_cmd = base_cmd + [f'-bl:{first_binlog}']
        getLogger().info("First build+deploy: %s" % ' '.join(first_cmd))
        subprocess.run(first_cmd, check=True)

        # Resolve the Android activity name for startup measurement (must happen after install)
        getLogger().info("Resolving activity name for package: %s" % packagename)
        resolve_cmd = xharness_adb() + [
            'shell',
            f'cmd package resolve-activity --brief {packagename} | tail -n 1'
        ]
        resolve_result = RunCommand(resolve_cmd, verbose=True)
        resolve_result.run()
        activityname = resolve_result.stdout.strip()
        getLogger().info("Resolved activity: %s" % activityname)

        first_startup_ms = self._measure_startup(packagename, activityname)
        getLogger().info("First deploy startup: %d ms" % first_startup_ms)

        # Step 2: Parse first deploy binlog → temp build metrics
        startup = StartupWrapper()
        first_build_report = os.path.join(const.TRACEDIR, 'first-build-and-deploy-perf-lab-report.json')
        startup.reportjson = first_build_report
        saved_upload = traits.upload_to_perflab_container
        traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.ANDROIDINNERLOOP,
                                   tracename='first-build-and-deploy.binlog',
                                   scenarioname=scenarioprefix + " - First Build and Deploy",
                                   upload_to_perflab_container=False)
        startup.parsetraces(traits)

        # Step 3: Merge first build metrics + startup → first e2e report
        first_e2e_report = os.path.join(const.TRACEDIR, 'first-debug-e2e-perf-lab-report.json')
        self._merge_build_and_startup(first_build_report, [first_startup_ms], first_e2e_report)

        # Step 4: Incremental loop — N iterations of edit → build+deploy → startup
        num_iterations = innerloopiterations
        getLogger().info("Starting incremental loop: %d iterations" % num_iterations)

        # Save original destination file content for toggling
        original_content = None
        if editsrc and editdest:
            if os.path.exists(editdest):
                with open(editdest, 'r') as f:
                    original_content = f.read()
            else:
                getLogger().warning("edit-dest %s does not exist; will only copy edit-src" % editdest)
        else:
            getLogger().warning("No edit-src/edit-dest specified; incremental builds will be no-change rebuilds")

        # Read modified content once
        modified_content = None
        if editsrc and os.path.exists(editsrc):
            with open(editsrc, 'r') as f:
                modified_content = f.read()

        incremental_startup_results = []
        aggregated_counters = {}  # counter_name -> list of result values
        report_template = None  # test metadata from first parsed report
        intermediate_files = []  # track files to clean up

        for iteration in range(1, num_iterations + 1):
            getLogger().info("=== Incremental iteration %d/%d ===" % (iteration, num_iterations))

            # 4a: Toggle source file
            if editsrc and editdest:
                if iteration % 2 == 1:
                    # Odd iterations: apply modified content
                    if modified_content is not None:
                        with open(editdest, 'w') as f:
                            f.write(modified_content)
                        getLogger().info("Applied modified source: %s" % editdest)
                    else:
                        getLogger().warning("Modified source content not available, skipping edit")
                else:
                    # Even iterations: restore original content
                    if original_content is not None:
                        with open(editdest, 'w') as f:
                            f.write(original_content)
                        getLogger().info("Restored original source: %s" % editdest)
                    else:
                        getLogger().warning("Original content not available, skipping edit")

            # 4b: Incremental build+deploy with per-iteration binlog
            iter_binlog_name = 'incremental-build-and-deploy-%d.binlog' % iteration
            iter_binlog = os.path.join(const.TRACEDIR, iter_binlog_name)
            incremental_cmd = base_cmd + [f'-bl:{iter_binlog}']
            getLogger().info("Incremental build+deploy: %s" % ' '.join(incremental_cmd))
            subprocess.run(incremental_cmd, check=True)
            intermediate_files.append(iter_binlog)

            # 4c: Measure startup once
            ms = self._measure_startup(packagename, activityname)
            getLogger().info("Incremental iteration %d/%d: build+deploy done, startup: %d ms" % (iteration, num_iterations, ms))
            incremental_startup_results.append(ms)

            # 4d: Parse this iteration's binlog → temp build report
            iter_report_name = 'incremental-build-report-%d.json' % iteration
            iter_report = os.path.join(const.TRACEDIR, iter_report_name)
            startup.reportjson = iter_report
            traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.ANDROIDINNERLOOP,
                                       tracename=iter_binlog_name,
                                       scenarioname=scenarioprefix + " - Incremental Build and Deploy",
                                       upload_to_perflab_container=False)
            startup.parsetraces(traits)
            intermediate_files.append(iter_report)

            # 4e: Extract build metrics from temp report and aggregate
            with open(iter_report, 'r') as f:
                iter_data = json.load(f)
            if report_template is None:
                # Save the test metadata (categories, etc.) from the first iteration
                report_template = iter_data["tests"][0].copy()
                report_template["counters"] = []
            for counter in iter_data["tests"][0]["counters"]:
                name = counter["name"]
                if name not in aggregated_counters:
                    # Initialize with counter metadata from first occurrence
                    aggregated_counters[name] = {
                        "name": name,
                        "topCounter": counter.get("topCounter", False),
                        "defaultCounter": counter.get("defaultCounter", False),
                        "higherIsBetter": counter.get("higherIsBetter", False),
                        "metricName": counter.get("metricName", "ms"),
                        "results": []
                    }
                # Each counter in a single-iteration report has a "results" list;
                # extend our aggregated list with those values.
                aggregated_counters[name]["results"].extend(counter.get("results", []))

            # 4f: Clean up temp report from TRACEDIR (leave binlog for now)
            if os.path.exists(iter_report):
                os.remove(iter_report)
                getLogger().info("Removed temp report: %s" % iter_report)

        # Step 5: Create final incremental E2E report with all collected results
        incremental_e2e_report = os.path.join(const.TRACEDIR, 'incremental-debug-e2e-perf-lab-report.json')
        final_counters = list(aggregated_counters.values())
        # Add startup counter
        final_counters.append({
            "name": "Time to Main",
            "topCounter": True,
            "defaultCounter": False,
            "higherIsBetter": False,
            "metricName": "ms",
            "results": incremental_startup_results
        })
        if report_template is not None:
            report_template["counters"] = final_counters
            final_report_data = {"tests": [report_template]}
        else:
            # Fallback: should not happen if at least 1 iteration ran
            final_report_data = {"tests": [{"counters": final_counters}]}
        with open(incremental_e2e_report, 'w') as f:
            json.dump(final_report_data, f, indent=2)
        getLogger().info("Final incremental E2E report written to: %s" % incremental_e2e_report)

        # Step 6: Cleanup intermediate files and upload final reports
        # Remove intermediate build report from first deploy
        getLogger().info("Removing intermediate first build report: %s" % first_build_report)
        if os.path.exists(first_build_report):
            os.remove(first_build_report)

        # Remove intermediate incremental binlogs from TRACEDIR
        for f_path in intermediate_files:
            if os.path.exists(f_path):
                os.remove(f_path)
                getLogger().info("Removed intermediate file: %s" % f_path)

        # Clean up helix upload dir: parsetraces() copies TRACEDIR contents there
        # on every call, so intermediate files accumulate. Remove them all, then
        # do a final copy of only the reports we want.
        if runninginlab():
            helix_upload_dir = helixuploaddir()
            if helix_upload_dir is not None:
                traces_upload = os.path.join(helix_upload_dir, 'traces')
                if os.path.exists(traces_upload):
                    # Remove all intermediate files from the upload dir
                    for fname in os.listdir(traces_upload):
                        fpath = os.path.join(traces_upload, fname)
                        if os.path.isfile(fpath):
                            basename = fname
                            # Keep only the final e2e reports and first binlog
                            if basename not in [os.path.basename(first_e2e_report),
                                                os.path.basename(incremental_e2e_report),
                                                os.path.basename(first_binlog)]:
                                os.remove(fpath)
                                getLogger().info("Removed uploaded intermediate: %s" % fpath)

        # Final upload
        traits.add_traits(overwrite=True, upload_to_perflab_container=saved_upload)
        helix_upload_dir = helixuploaddir()
        if runninginlab() and helix_upload_dir is not None:
            copytree(const.TRACEDIR, os.path.join(helix_upload_dir, 'traces'), dirs_exist_ok=True)
            if traits.upload_to_perflab_container:
                for report_path in [first_e2e_report, incremental_e2e_report]:
                    upload_code = upload.upload(report_path, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_STORAGE_URI)
                    getLogger().info("Upload code for %s: %s" % (os.path.basename(report_path), upload_code))
                    if upload_code != 0:
                        sys.exit(upload_code)
