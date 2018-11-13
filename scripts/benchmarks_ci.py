#!/usr/bin/env python3

"""
This wraps all the logic of how to build/run the .NET micro benchmarks,
acquire tools, gather data into BenchView format and upload it, archive
results, etc.

This is meant to be used on CI runs and available for local runs,
so developers can easily reproduce what runs in the lab.

The micro benchmarks themselves can be built and run using the DotNet tools.
"""

from argparse import ArgumentParser
from glob import iglob
from io import StringIO
from itertools import chain
from logging import getLogger

import csv
import os
import platform
import sys

from performance.common import get_tools_directory
from performance.common import push_dir
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers

import benchview
import dotnet
import micro_benchmarks


def init_tools(
        architecture: str,
        channel: str,
        verbose: bool) -> None:
    '''
    Install tools used by this repository into the tools folder.
    This function writes a semaphore file when tools have been successfully
    installed in order to avoid reinstalling them on every rerun.
    '''
    semaphore_file = os.path.join(get_tools_directory(), 'init_tools.sem')

    if not os.path.isfile(semaphore_file):
        getLogger().info('Installing tools.')
        dotnet.install(architecture, channel, verbose)
        benchview.install()
        with open(semaphore_file, 'w') as sem_file:
            sem_file.write('done')
    else:
        getLogger().info('Tools already installed.')

    # Add installed dotnet cli to PATH
    os.environ["PATH"] = dotnet.get_dotnet_directory() + os.pathsep + \
        os.environ["PATH"]


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''Adds new arguments to the specified ArgumentParser object.'''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    # Download DotNet Cli
    dotnet.add_arguments(parser)

    # Restore/Build/Run functionality for MicroBenchmarks.csproj
    micro_benchmarks.add_arguments(parser)

    def __get_bdn_arguments(user_input: str) -> list:
        file = StringIO(user_input)
        reader = csv.reader(file, delimiter=' ')
        for args in reader:
            return args
        return []

    parser.add_argument(
        '--bdn-arguments',
        dest='bdn_arguments',
        required=False,
        type=__get_bdn_arguments,
        help='''Command line arguments to be passed to the BenchmarkDotNet
        harness.'''
    )

    # .NET Runtime Options.
    parser.add_argument(
        '--optimization-level',
        dest='optimization_level',
        required=False,
        default='tiered',
        choices=['tiered', 'full_opt', 'min_opt']
    )

    # BenchView acquisition, and fuctionality
    parser.add_argument(
        '--generate-benchview-data',
        dest='generate_benchview_data',
        action='store_true',
        default=False,
        help='Flags indicating whether BenchView data should be generated.'
    )

    parser.add_argument(
        '--upload-to-benchview-container',
        dest='upload_to_benchview_container',
        required=False,
        type=str,
        help='Name of the Azure Storage Container to upload to.'
    )

    # TODO: Make these arguments dependent on `generate_benchview_data`?
    is_benchview_commit_name_defined = 'BenchviewCommitName' in os.environ
    default_submission_name = os.environ['BenchviewCommitName'] \
        if is_benchview_commit_name_defined else None
    parser.add_argument(
        '--benchview-submission-name',
        dest='benchview_submission_name',
        default=default_submission_name,
        required=False,
        type=str,
        help='BenchView submission name.'
    )
    parser.add_argument(
        '--benchview-run-type',
        dest='benchview_run_type',
        default='local',
        choices=['rolling', 'private', 'local'],
        type=str.lower,
        help='BenchView submission type.'
    )
    parser.add_argument(
        '--benchview-config-name',
        dest='benchview_config_name',  # Uses as default args.configuration
        required=False,
        type=str,
        help="BenchView's (user facing) configuration display name."
    )
    parser.add_argument(
        '--benchview-machinepool',
        dest='benchview_machinepool',
        default=platform.platform(),
        required=False,
        type=str,
        help="A logical name that groups test results into a single *machine*."
    )
    parser.add_argument(
        '--benchview-config',
        dest='benchview_config',
        metavar=('key', 'value'),
        action='append',
        required=False,
        nargs=2,
        help='''A configuration property defined as a {key:value} pair.
        This is used to describe the benchmark results. For example, some
        types of configurations can be: optimization level (tiered, full opt,
        min opt), configuration (debug/release), profile (on/off), etc.'''
    )

    # Generic arguments.
    parser.add_argument(
        '-q', '--quiet',
        required=False,
        default=False,
        action='store_true',
        help='Turns off verbosity.',
    )

    return parser


def __process_arguments(args: list):
    parser = ArgumentParser(
        description='Tool to run .NET micro benchmarks',
        allow_abbrev=False,
        epilog='''Additional information:
        ''')
    add_arguments(parser)
    return parser.parse_args(args)


def __get_coreclr_os_name():
    if sys.platform == 'win32':
        return 'Windows_NT'
    elif sys.platform == 'linux':
        def __linux_distribution():
            try:
                return platform.linux_distribution()
            except:
                platform_error_message = \
                    'Unable to determine OS. ' \
                    'Time to look into the distro module.'
                getLogger().error(platform_error_message)
                raise NotImplementedError(platform_error_message)
        os_name, os_version, _ = __linux_distribution()
        return '{}{}'.format(os_name, os_version)
    else:
        return platform.platform()


def __get_corefx_os_name():
    if sys.platform == 'win32':
        return 'Windows_NT'
    elif sys.platform == 'linux':
        return 'Linux'
    else:
        return platform.platform()


def __run_benchview_scripts(args: list, verbose: bool) -> None:
    '''Run BenchView scripts to collect performance data.'''
    if not args.generate_benchview_data:
        return

    scripts = benchview.BenchView(verbose)
    bin_directory = os.path.join(
        micro_benchmarks.BENCHMARKS_CSPROJ.working_directory,
        'bin'
    )

    # BenchView submission-metadata.py
    scripts.submission_metadata(
        working_directory=bin_directory,
        name=args.benchview_submission_name)

    # BenchView build.py
    # TODO: pass more parameters.
    scripts.build(
        working_directory=bin_directory,
        build_type=args.benchview_run_type,
        subparser='git')

    # BenchView machinedata.py
    scripts.machinedata(working_directory=bin_directory)

    for framework in args.frameworks:
        working_directory = dotnet.get_build_directory(
            bin_directory=bin_directory,
            configuration=args.configuration,
            framework=framework,
        )

        # BenchView measurement.py
        scripts.measurement(working_directory=working_directory)

    # Build the BenchView configuration data
    benchview_config_name = args.benchview_config_name \
        if args.benchview_config_name else args.configuration
    benchview_config = list(chain(args.benchview_config)) \
        if args.benchview_config else []
    i = iter(benchview_config)
    benchview_config = dict(zip(i, i))

    # Configuration
    if 'Configuration' not in benchview_config:
        benchview_config['Configuration'] = args.configuration

    # TODO: Generate existing configs. Maybe this is a good time to unify them?
    submission_architecture = args.architecture
    if args.category.casefold() == 'CoreClr'.casefold():
        benchview_config['JitName'] = 'ryujit'  # This is currently fixed.
        benchview_config['OS'] = __get_coreclr_os_name()
        benchview_config['OptLevel'] = args.optimization_level
        benchview_config['PGO'] = 'pgo'  # This is currently fixed.
        benchview_config['Profile'] = 'On' if args.enable_pmc else 'Off'
    elif args.category.casefold() == 'CoreFx'.casefold():
        submission_architecture = 'AnyCPU'
        benchview_config['OS'] = __get_corefx_os_name()
        benchview_config['RunType'] = \
            'Diagnostic' if args.enable_pmc else 'Profile'

    # Find all measurement.json
    measurement_jsons = []
    with push_dir(bin_directory):
        for measurement_json in iglob('**/measurement.json', recursive=True):
            measurement_jsons.append(measurement_json)

    scripts.submission(
        working_directory=bin_directory,
        measurement_jsons=measurement_jsons,
        architecture=submission_architecture,
        config_name=benchview_config_name,
        configs=benchview_config,
        machinepool=args.benchview_machinepool,
        jobgroup=args.category if args.category else '.NET Performance',
        jobtype=args.benchview_run_type
    )

    # Upload to a BenchView container.
    if args.upload_to_benchview_container:
        scripts.upload(
            working_directory=bin_directory,
            container=args.upload_to_benchview_container)


def __main(args: list) -> int:
    validate_supported_runtime()

    # TODO: Enable Linux
    if sys.platform != 'win32':
        raise NotImplementedError('Non-Windows platforms have not been tested')

    args = __process_arguments(args)
    verbose = not args.quiet
    setup_loggers(verbose=verbose)

    # This validation could be cleaner
    if args.generate_benchview_data and not args.benchview_submission_name:
        raise RuntimeError("""In order to generate BenchView data,
            `--benchview-submission-name` must be provided.""")

    # Set common environment variables.
    os.environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    os.environ['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    os.environ['UseSharedCompilation'] = 'false'

    # Line below due to: https://github.com/dotnet/cli/issues/10196
    os.environ['DOTNET_ROOT'] = dotnet.get_dotnet_directory()

    # Acquire necessary tools (dotnet, and BenchView)
    init_tools(
        architecture=args.architecture,
        channel=args.channel,
        verbose=verbose)

    # Configure .NET Runtime
    # TODO: Is this still correct?
    if args.optimization_level == 'min_opt':
        os.environ['COMPlus_JITMinOpts'] = '1'
        os.environ['COMPlus_TieredCompilation'] = '0'
    elif args.optimization_level == 'full_opt':
        os.environ['COMPlus_TieredCompilation'] = '0'

    # dotnet --info
    dotnet.info(verbose=verbose)

    # .NET micro-benchmarks
    # Restore and build micro-benchmarks
    micro_benchmarks.build(
        args.configuration,
        args.frameworks,
        verbose)

    # Run micro-benchmarks
    for framework in args.frameworks:
        run_args = [
            '--'
        ]
        if args.category:
            run_args += ['--allCategories', args.category]
        if args.corerun_path:
            run_args += ['--coreRun', args.corerun_path]
        if args.dotnet_path:
            run_args += ['--cli', args.dotnet_path]
        if args.enable_pmc:
            run_args += [
                '--counters',
                'BranchMispredictions+CacheMisses+InstructionRetired',
            ]
        run_args += [
            '--maxIterationCount', str(args.max_iteration_count),
            '--minIterationCount', str(args.min_iteration_count),
            # '--filter', '*Adams*',
        ]

        # Extra BenchmarkDotNet cli arguments.
        if args.bdn_arguments:
            run_args += args.bdn_arguments

        # FIXME: BenchmarkDotNet harness should return non-zero exit code
        #   when wrong argument is passed!
        micro_benchmarks.run(
            args.configuration,
            framework,
            verbose,
            *run_args
        )

    __run_benchview_scripts(args, verbose)
    # TODO: Archive artifacts.


if __name__ == "__main__":
    __main(sys.argv[1:])
