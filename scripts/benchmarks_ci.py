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
from itertools import chain
from logging import getLogger

import os
import platform
import subprocess
import sys

from performance.common import get_tools_directory
from performance.common import validate_supported_runtime
from performance.dotnet import dotnet_info
from performance.logger import setup_loggers

import benchview
import dotnet_install
import micro_benchmarks


def get_dotnet_sha(dotnetPath):
    """ Discovers the dotnet sha
    Args:
        dotnetPath (str): dotnet.exe path
    """
    out = subprocess.check_output([dotnetPath, '--info'])

    foundHost = False
    for line in out.splitlines():
        decodedLine = line.decode('utf-8')

        # First look for the host information, since that is the sha we are
        # looking for. Then, grab the first Commit line we find, which will be
        # the sha of the framework we are testing
        if 'Host' in decodedLine:
            foundHost = True
        elif foundHost and 'Commit' in decodedLine:
            return decodedLine.strip().split()[1]

    raise RuntimeError('.NET host commit sha not found.')


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
        dotnet_install.install(architecture, channel, verbose)
        benchview.install()
        with open(semaphore_file, 'w') as sem_file:
            sem_file.write('done')
    else:
        getLogger().info('Tools already installed.')

    # Add installed dotnet cli to PATH
    os.environ["PATH"] = dotnet_install.get_dotnet_directory() + os.pathsep + \
        os.environ["PATH"]


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''Adds new arguments to the specified ArgumentParser object.'''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    # Download DotNet Cli
    dotnet_install.add_arguments(parser)

    # Restore/Build/Run functionality for MicroBenchmarks.csproj
    micro_benchmarks.add_arguments(parser)

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
        default=platform.processor(),
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
        '-v', '--verbose',
        required=False,
        default=False,
        action='store_true',
        help='Turns on verbosity (default "False")',
    )

    return parser


def __process_arguments(args: list):
    parser = ArgumentParser(
        description='Tool to run .NET micro benchmarks',
        allow_abbrev=False)
    add_arguments(parser)
    return parser.parse_args(args)


def __main(args: list) -> int:
    validate_supported_runtime()

    # TODO: Enable Linux
    if sys.platform != 'win32':
        raise NotImplementedError('Non-Windows platforms have not been tested')

    args = __process_arguments(args)
    setup_loggers(verbose=args.verbose)

    # This validation could be cleaner
    if args.generate_benchview_data and not args.benchview_submission_name:
        raise RuntimeError("""In order to generate BenchView data,
            `--benchview-submission-name` must be provided.""")

    # Set common environment variables.
    os.environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    os.environ['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    os.environ['UseSharedCompilation'] = 'false'

    # Line below due to: https://github.com/dotnet/cli/issues/10196
    os.environ['DOTNET_ROOT'] = dotnet_install.get_dotnet_directory()

    init_tools(
        architecture=args.architecture,
        channel=args.channel,
        verbose=args.verbose)

    # dotnet --info
    dotnet_info(verbose=args.verbose)

    # .NET micro-benchmarks
    # Restore and build micro-benchmarks
    # micro_benchmarks.build(
    #     args.configuration,
    #     args.frameworks,
    #     args.verbose)

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
        run_args += [
            '--maxIterationCount', str(args.max_iteration_count),
            '--minIterationCount', str(args.min_iteration_count),
            # '--filter', 'Adams',
        ]
        # micro_benchmarks.run(
        #     args.configuration,
        #     framework,
        #     args.verbose,
        #     *run_args
        # )

    # Run BenchView scripts to generate data.
    if args.generate_benchview_data:
        # TODO: Delete all existing files from bin before running?

        # TODO: (Submission-metadata + Build + MachineData) run once.
        # TODO: (Measurement) for each framework.
        # TODO: (Submission + Upload) run once.

        # def __find_first_glob_path(pattern: str) -> str:
        #     for path in iglob(pattern, recursive=True):
        #         if os.path.isdir(path):
        #             return path
        #     raise ValueError(
        #         'Unable to find directory for the specified pattern.')
        # pattern = 'bin/**/{Configuration}/{TargetFramework}/'.format(
        #     Configuration=args.configuration,
        #     TargetFramework=args.framework
        # )
        # benchview_working_directory = __find_first_glob_path(pattern)

        # benchview_files = [
        #     'submission-metadata.json',
        #     'build.json',
        #     'machinedata.json',
        #     'measurement.json',
        #     'submission.json',
        # ]

        benchview_working_directory = os.path.join(
            micro_benchmarks.BENCHMARKS_CSPROJ.working_directory,
            'bin',  # TODO: Should probably be bin/{configuration}/{framework}
            # args.configuration,
            # args.frameworks
        )
        benchview_scripts = benchview.BenchView(
            benchview_working_directory,
            args.verbose
        )

        # BenchView submission-metadata.py
        benchview_scripts.submission_metadata(
            name=args.benchview_submission_name)

        # BenchView build.py
        benchview_scripts.build(args.benchview_run_type)

        # BenchView machinedata.py
        benchview_scripts.machinedata()

        # BenchView measurement.py
        # FIXME: Iterate on framework? and add it to configuration?
        pattern = "{}/**/{}/{}/BenchmarkDotNet.Artifacts/**/*-full.json" \
            .format(
                benchview_scripts.working_directory,
                args.configuration,
                args.frameworks[0])  # TODO: To be deleted.
        getLogger().info(
            'Searching BenchmarkDotNet output files with glob: %s', pattern
        )
        for full_json_file in iglob(pattern, recursive=True):
            benchview_scripts.measurement(
                bdn_json_path=os.path.abspath(full_json_file))

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

        # TODO: Generate existing configs.
        submission_architecture = args.architecture
        if args.category.casefold() == 'CoreClr'.casefold():
            # JitName
            benchview_config['JitName'] = 'ryujit'

            # OS
            if sys.platform == 'win32':
                benchview_config['OS'] = 'Windows_NT'
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
                benchview_config['OS'] = '{}{}'.format(os_name, os_version)
            else:
                benchview_config['OS'] = platform.platform()

            #   OptLevel=[full_opt|min_opt|tiered]
            #   PGO=[pgo|nopogo]
            #   Profile=[Off|On]
        elif args.category.casefold() == 'CoreFx'.casefold():
            submission_architecture = 'AnyCPU'

            # OS
            if sys.platform == 'win32':
                benchview_config['OS'] = 'Windows_NT'
            elif sys.platform == 'linux':
                benchview_config['OS'] = 'Linux'
            else:
                benchview_config['OS'] = platform.platform()

            # RunType
            #   RunType=[Profile|Diagnostic]

        benchview_scripts.submission(
            architecture=submission_architecture,
            config_name=benchview_config_name,
            configs=benchview_config,
            machinepool=args.benchview_machinepool,
            jobgroup=args.category if args.category else '.NET Performance',
            jobtype=args.benchview_run_type
        )

        # Upload to a BenchView container.
        if args.upload_to_benchview_container:
            benchview_scripts.upload(args.upload_to_benchview_container)


if __name__ == "__main__":
    __main(sys.argv[1:])
