#!/usr/bin/env python3

'''
Additional information:

This script wraps all the logic of how to build/run the .NET micro benchmarks,
acquire tools, gather data into perflab format and upload it, archive
results, etc.

This is meant to be used on CI runs and available for local runs,
so developers can easily reproduce what runs in the lab.

Note:

The micro benchmarks themselves can be built and run using the DotNet Cli tool.
For more information refer to: benchmarking-workflow.md

../docs/benchmarking-workflow.md
  - or -
https://github.com/dotnet/performance/blob/main/docs/benchmarking-workflow.md
'''

from argparse import ArgumentParser, ArgumentTypeError
from datetime import datetime
import json
from logging import getLogger

import os
import shutil
import sys
from typing import Any, List, Optional

from performance.common import validate_supported_runtime, get_artifacts_directory, helixuploadroot
from performance.logger import setup_loggers
from performance.tracer import setup_tracing, enable_trace_console_exporter, get_tracer
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_TOKEN_VAR, UPLOAD_QUEUE
from channel_map import ChannelMap
from subprocess import CalledProcessError
from glob import glob

import dotnet
import micro_benchmarks

setup_tracing()
tracer = get_tracer()

@tracer.start_as_current_span(name="benchmarks_ci_init_tools") # type: ignore
def init_tools(
        architecture: str,
        dotnet_versions: List[str],
        target_framework_monikers: List[str],
        verbose: bool,
        azure_feed_url: Optional[str] = None,
        internal_build_key: Optional[str] = None) -> None:
    '''
    Install tools used by this repository into the tools folder.
    This function writes a semaphore file when tools have been successfully
    installed in order to avoid reinstalling them on every rerun.
    '''
    getLogger().info('Installing tools.')
    channels = [
        ChannelMap.get_channel_from_target_framework_moniker(target_framework_moniker)
        for target_framework_moniker in target_framework_monikers
    ]

    dotnet.remove_dotnet(
        architecture=architecture
    )
    dotnet.install(
        architecture=architecture,
        channels=channels,
        versions=dotnet_versions,
        verbose=verbose,
        azure_feed_url=azure_feed_url,
        internal_build_key=internal_build_key
    )

def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''Adds new arguments to the specified ArgumentParser object.'''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    # Download DotNet Cli
    dotnet.add_arguments(parser)

    # Restore/Build/Run functionality for MicroBenchmarks.csproj
    micro_benchmarks.add_arguments(parser)

    PRODUCT_INFO = [
        'init-tools',  # Default
        'repo',
        'cli',
        'args',
    ]
    parser.add_argument(
        '--cli-source-info',
        dest='cli_source_info',
        required=False,
        default=PRODUCT_INFO[0],
        choices=PRODUCT_INFO,
        help='Specifies where the product information comes from.',
    )
    parser.add_argument(
        '--cli-branch',
        dest='cli_branch',
        required=False,
        type=str,
        help='Product branch.'
    )
    parser.add_argument(
        '--cli-commit-sha',
        dest='cli_commit_sha',
        required=False,
        type=str,
        help='Product commit sha.'
    )
    parser.add_argument(
        '--cli-repository',
        dest='cli_repository',
        required=False,
        type=str,
        help='Product repository.'
    )

    def __is_valid_dotnet_path(dp: str) -> str:
        if not os.path.isdir(dp):
            raise ArgumentTypeError('Path {} does not exist'.format(dp))
        if sys.platform == 'win32':
            if not os.path.isfile(os.path.join(dp, 'dotnet.exe')):
                raise ArgumentTypeError('Could not find dotnet.exe in {}'.format(dp))
        else:
            if not os.path.isfile(os.path.join(dp, 'dotnet')):
                raise ArgumentTypeError('Could not find dotnet in {}'.format(dp))
        return dp

    parser.add_argument(
        '--dotnet-path',
        dest='dotnet_path',
        required=False,
        type=__is_valid_dotnet_path,
        help='Path to a custom dotnet'
    )

    def __is_valid_datetime(dt: str) -> str:
        try:
            datetime.strptime(dt, '%Y-%m-%dT%H:%M:%SZ')
            return dt
        except ValueError:
            raise ArgumentTypeError(
                'Datetime "{}" is in the wrong format.'.format(dt))

    parser.add_argument(
        '--cli-source-timestamp',
        dest='cli_source_timestamp',
        required=False,
        type=__is_valid_datetime,
        help='''Product timestamp of the soruces used to generate this build
            (date-time from RFC 3339, Section 5.6.
            "%%Y-%%m-%%dT%%H:%%M:%%SZ").'''
    )

    parser.add_argument('--upload-to-perflab-container',
        dest="upload_to_perflab_container",
        required=False,
        help="Causes results files to be uploaded to perf container",
        action='store_true'
    )

    # Generic arguments.
    parser.add_argument(
        '-q', '--quiet',
        required=False,
        default=False,
        action='store_true',
        help='Turns off verbosity.',
    )
    parser.add_argument(
        '--build-only',
        dest='build_only',
        required=False,
        default=False,
        action='store_true',
        help='Builds the benchmarks but does not run them.',
    )
    parser.add_argument(
        '--run-only',
        dest='run_only',
        required=False,
        default=False,
        action='store_true',
        help='Attempts to run the benchmarks without building.',
    )
    parser.add_argument(
        '--resume',
        dest='resume',
        required=False,
        default=False,
        action='store_true',
        help='Resume a previous run from existing benchmark results',
    )
    parser.add_argument(
        '--skip-logger-setup',
        dest='skip_logger_setup',
        required=False,
        default=False,
        action='store_true',
        help='Skips the logger setup, for cases when invoked by another script that already sets logging up',
    )

    parser.add_argument(
        '--azure-feed-url',
        dest='azure_feed_url',
        required=False,
        default=None,
        help='Internal azure feed to fetch the build from',
    )

    parser.add_argument(
        '--internal-build-key',
        dest='internal_build_key',
        required=False,
        default=None,
        help='Key used to fetch the build from an internal azure feed',
    )

    parser.add_argument(
        '--partition',
        dest='partition',
        required=False,
        type=int,
        help='Partition Index of the run',
    )

    parser.add_argument(
        '--enable-open-telemetry-logger',
        dest='enable_open_telemetry_logger',
        required=False,
        default=False,
        action='store_true',
        help='Enables OpenTelemetry logging',
    )

    parser.add_argument(
        '--enable-open-telemetry-tracer-console',
        dest='enable_open_telemetry_tracer_console',
        required=False,
        default=False,
        action='store_true',
        help='Enables OpenTelemetry tracing',
    )

    return parser


def __process_arguments(args: List[str]):
    parser = ArgumentParser(
        description='Tool to run .NET micro benchmarks',
        allow_abbrev=False,
        # epilog=os.linesep.join(__doc__.splitlines())
        epilog=__doc__,
    )
    add_arguments(parser)
    return parser.parse_args(args)

@tracer.start_as_current_span("benchmarks_ci_main") # type: ignore
def main(argv: List[str]):
    validate_supported_runtime()
    args = __process_arguments(argv)
    verbose = not args.quiet

    if not args.skip_logger_setup:
        setup_loggers(verbose=verbose, enable_open_telemetry_logger=args.enable_open_telemetry_logger)
        if args.enable_open_telemetry_tracer_console:
            enable_trace_console_exporter()

    if not args.frameworks:
        raise Exception("Framework version (-f) must be specified.")

    target_framework_monikers = dotnet \
        .FrameworkAction \
        .get_target_framework_monikers(args.frameworks)
    # Acquire necessary tools (dotnet)
    if not args.dotnet_path:
        init_tools(
            architecture=args.architecture,
            dotnet_versions=args.dotnet_versions,
            target_framework_monikers=target_framework_monikers,
            verbose=verbose,
            azure_feed_url=args.azure_feed_url,
            internal_build_key=args.internal_build_key
        )
    else:
        dotnet.setup_dotnet(args.dotnet_path)

    # WORKAROUND
    # The MicroBenchmarks.csproj targets .NET Core 2.1, 3.0, 3.1 and 5.0
    # to avoid a build failure when using older frameworks (error NETSDK1045:
    # The current .NET SDK does not support targeting .NET Core $XYZ)
    # we set the TFM to what the user has provided.
    os.environ['PERFLAB_TARGET_FRAMEWORKS'] = ';'.join(
        target_framework_monikers
    )

    # dotnet --info
    dotnet.info(verbose=verbose)

    bin_dir_to_use=micro_benchmarks.get_bin_dir_to_use(args.csprojfile, args.bin_directory, args.run_isolated)
    BENCHMARKS_CSPROJ = dotnet.CSharpProject(
        project=args.csprojfile,
        bin_directory=bin_dir_to_use
    )

    if not args.run_only:
        # .NET micro-benchmarks
        # Restore and build micro-benchmarks
        micro_benchmarks.build(
            BENCHMARKS_CSPROJ,
            args.configuration,
            target_framework_monikers,
            args.incremental,
            args.run_isolated,
            args.wasm,
            verbose
        )

    # Run micro-benchmarks
    if not args.build_only:
        run_contains_errors = False
        upload_container = UPLOAD_CONTAINER
        try:
            for framework in args.frameworks:
                is_success = micro_benchmarks.run(
                    BENCHMARKS_CSPROJ,
                    args.configuration,
                    framework,
                    args.run_isolated,
                    verbose,
                    args
                )

                if not is_success:
                    getLogger().warning(f"Benchmark run for framework '{framework}' contains errors")
                    run_contains_errors = True

            artifacts_dir = get_artifacts_directory() if not args.bdn_artifacts else args.bdn_artifacts

            combined_file_prefix = "" if args.partition is None else f"Partition{args.partition}-"
            globpath = os.path.join(artifacts_dir, '**', '*perf-lab-report.json')
            all_reports: List[Any] = []
            for file in glob(globpath, recursive=True):
                with open(file, 'r', encoding="utf8") as report_file:
                    all_reports.append(json.load(report_file))

            with open(os.path.join(artifacts_dir, f"{combined_file_prefix}combined-perf-lab-report.json"), "w", encoding="utf8") as all_reports_file:
                json.dump(all_reports, all_reports_file)

            helix_upload_root = helixuploadroot()
            if helix_upload_root is not None:
                for file in glob(globpath, recursive=True):
                    shutil.copy(file, os.path.join(helix_upload_root, file.split(os.sep)[-1]))

                shutil.make_archive(os.path.join(helix_upload_root, "bdn-artifacts"), 'zip', artifacts_dir)
                getLogger().info("Created \"bdn-artifacts\".zip")
            else:
                getLogger().info("Skipping upload of artifacts to Helix as HELIX_WORKITEM_UPLOAD_ROOT environment variable is not set.")

        except CalledProcessError:
            getLogger().info("Run failure registered")
            # rethrow the caught CalledProcessError exception so that the exception being bubbled up correctly.
            raise

        dotnet.shutdown_server(verbose)

        if args.upload_to_perflab_container:
            globpath = os.path.join(artifacts_dir, '**', '*perf-lab-report.json')
            import upload
            upload_code = upload.upload(globpath, upload_container, UPLOAD_QUEUE, UPLOAD_TOKEN_VAR, UPLOAD_STORAGE_URI)
            getLogger().info("Benchmarks Upload Code: " + str(upload_code))
            if upload_code != 0:
                sys.exit(upload_code)
        # TODO: Archive artifacts.

        # Still return 1 so that the build pipeline shows failures even though there were some successful results
        if run_contains_errors:
            sys.exit(1)

if __name__ == "__main__":
    main(sys.argv[1:])
