#!/usr/bin/env python3

'''
Additional information:

This script wraps all the logic of how to build/run the .NET micro benchmarks,
acquire tools, gather data into BenchView format and upload it, archive
results, etc.

This is meant to be used on CI runs and available for local runs,
so developers can easily reproduce what runs in the lab.

Note:

The micro benchmarks themselves can be built and run using the DotNet Cli tool.
For more information refer to: benchmarking-workflow.md

../docs/benchmarking-workflow.md
  - or -
https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow.md
'''

from argparse import ArgumentParser, ArgumentTypeError
from datetime import datetime
from glob import iglob
from itertools import chain
from logging import getLogger
from shutil import which

import os
import platform
import sys

from performance.common import push_dir
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers

import benchview
import dotnet
import micro_benchmarks


if sys.platform == 'linux' and "linux_distribution" not in dir(platform):
    message = '''The `linux_distribution` method is missing from ''' \
        '''the `platform` module, which is used to find out information ''' \
        '''about the OS flavor/version we are using.%s''' \
        '''The Python Docs state that `platform.linux_distribution` is ''' \
        '''"Deprecated since version 3.5, will be removed in version 3.8: ''' \
        '''See alternative like the distro package.%s"''' \
        '''Most systems in the lab have Python versions 3.5 and 3.6 ''' \
        '''installed, so we are good at the moment.%s''' \
        '''If we are hitting this issue, then it might be time to look ''' \
        '''into using the `distro` module, and possibly packaing as part ''' \
        '''of the dependencies of these scripts/repo.'''
    getLogger().error(message, os.linesep, os.linesep, os.linesep)
    exit(1)


def init_tools(
        architecture: str,
        frameworks: str,
        verbose: bool) -> None:
    '''
    Install tools used by this repository into the tools folder.
    This function writes a semaphore file when tools have been successfully
    installed in order to avoid reinstalling them on every rerun.
    '''
    getLogger().info('Installing tools.')
    channels = [
        micro_benchmarks.TargetFrameworkAction.get_channel(framework) or 'LTS'
        for framework in frameworks
    ]
    dotnet.install(
        architecture=architecture,
        channels=channels,
        verbose=verbose,
    )
    benchview.install()


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''Adds new arguments to the specified ArgumentParser object.'''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    # Download DotNet Cli
    dotnet.add_arguments(parser)

    # Restore/Build/Run functionality for MicroBenchmarks.csproj
    micro_benchmarks.add_arguments(parser)

    # .NET Runtime Options.
    parser.add_argument(
        '--optimization-level',
        dest='optimization_level',
        required=False,
        default='tiered',
        choices=['tiered', 'full_opt', 'min_opt']
    )

    PRODUCT_INFO = [
        'init-tools',  # Default
        'repo',
        'cli',
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
        # epilog=os.linesep.join(__doc__.splitlines())
        epilog=__doc__,
    )
    add_arguments(parser)
    return parser.parse_args(args)


def __get_coreclr_os_name():
    if sys.platform == 'win32':
        return 'Windows_NT'
    elif sys.platform == 'linux':
        os_name, os_version, _ = platform.linux_distribution()
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


def __get_build_info(args, framework: str) -> benchview.BuildInfo:
    # TODO: Improve complex scenarios.
    #   Could the --cli-* arguments take multiple build info objects from the
    #   command line interface?
    subparser = 'none'
    branch = args.cli_branch
    commit_sha = args.cli_commit_sha
    repository = args.cli_repository
    source_timestamp = args.cli_source_timestamp

    if args.cli_source_info == 'cli':
        # Retrieve data from the specified dotnet executable.
        commit_sha = dotnet.get_host_commit_sha(args.cli)
        source_timestamp = dotnet.get_commit_date(commit_sha, repository)
    elif args.cli_source_info == 'init-tools':
        # Retrieve data from the installed dotnet tools.
        branch = micro_benchmarks.TargetFrameworkAction.get_channel(
            framework
        )
        if not branch:
            err_msg = 'Cannot determine build information for "%s"' % framework
            getLogger().error(err_msg)
            getLogger().error(
                "Build information can be provided using the --cli-* options."
            )
            raise ValueError(err_msg)
        commit_sha = dotnet.get_host_commit_sha(which('dotnet'))
        repository = 'https://github.com/dotnet/core-setup'
        source_timestamp = dotnet.get_commit_date(commit_sha)
    elif args.cli_source_info == 'repo':
        # Retrieve data from current repository.
        subparser = 'git'
    else:
        raise ValueError('Unknown build source.')

    return benchview.BuildInfo(
        subparser,
        branch,
        commit_sha,
        repository,
        source_timestamp
    )


def __run_benchview_scripts(args: list, verbose: bool) -> None:
    '''Run BenchView scripts to collect performance data.'''
    if not args.generate_benchview_data:
        return

    # TODO: Delete previously generated BenchView data (*.json)

    benchviewpy = benchview.BenchView(verbose)
    bin_directory = micro_benchmarks.BENCHMARKS_CSPROJ.bin_path

    # BenchView submission-metadata.py
    submission_name = args.benchview_submission_name
    is_pr = args.benchview_run_type == 'private' and\
        'BenchviewCommitName' in os.environ
    rolling_data = args.benchview_run_type == 'rolling' and \
        'GIT_BRANCH_WITHOUT_ORIGIN' in os.environ and \
        'GIT_COMMIT' in os.environ
    if is_pr:
        submission_name = '%s %s %s' % (
            args.category,
            args.benchview_run_type,
            args.benchview_submission_name
        )
    elif rolling_data:
        submission_name += '%s %s %s %s' % (
            args.category,
            args.benchview_run_type,
            os.environ['GIT_BRANCH_WITHOUT_ORIGIN'],
            os.environ['GIT_COMMIT']
        )

    benchviewpy.submission_metadata(
        working_directory=bin_directory,
        name=submission_name)

    # BenchView machinedata.py
    benchviewpy.machinedata(working_directory=bin_directory)

    for framework in args.frameworks:
        buildinfo = __get_build_info(args, framework)

        # BenchView build.py
        benchviewpy.build(
            working_directory=bin_directory,
            build_type=args.benchview_run_type,
            subparser=buildinfo.subparser,
            branch=buildinfo.branch,
            commit=buildinfo.commit_sha,
            repository=buildinfo.repository,
            source_timestamp=buildinfo.source_timestamp
        )

        working_directory = dotnet.get_build_directory(
            bin_directory=bin_directory,
            configuration=args.configuration,
            framework=framework,
        )

        # BenchView measurement.py
        benchviewpy.measurement(working_directory=working_directory)

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

    # Generate existing configs. This may be a good time to unify them?
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
    with push_dir(bin_directory):
        for framework in args.frameworks:
            glob_format = '**/%s/%s/measurement.json' % (
                args.configuration,
                framework
            )

            measurement_jsons = []
            for measurement_json in iglob(glob_format, recursive=True):
                measurement_jsons.append(measurement_json)

            jobGroup = '.NET Performance (%s)' % args.category \
                if args.category \
                else '.NET Performance'

            if len(args.frameworks) > 1:
                benchview_config['Framework'] = framework

            # BenchView submission.py
            benchviewpy.submission(
                working_directory=bin_directory,
                measurement_jsons=measurement_jsons,
                architecture=submission_architecture,
                config_name=benchview_config_name,
                configs=benchview_config,
                machinepool=args.benchview_machinepool,
                jobgroup=jobGroup,
                jobtype=args.benchview_run_type
            )

            # Upload to a BenchView container (upload.py).
            # TODO: submission.py does not have an --append option,
            #   instead upload each build/config separately.
            if args.upload_to_benchview_container:
                benchviewpy.upload(
                    working_directory=bin_directory,
                    container=args.upload_to_benchview_container)


def __main(args: list) -> int:
    validate_supported_runtime()
    args = __process_arguments(args)
    verbose = not args.quiet
    setup_loggers(verbose=verbose)

    # This validation could be cleaner
    if args.generate_benchview_data and not args.benchview_submission_name:
        raise RuntimeError("""In order to generate BenchView data,
            `--benchview-submission-name` must be provided.""")

    # Acquire necessary tools (dotnet, and BenchView)
    init_tools(
        architecture=args.architecture,
        frameworks=args.frameworks,
        verbose=verbose
    )

    # Configure .NET Runtime
    # TODO: Is this still correct across releases?
    #   Does it belong in the script?
    if args.optimization_level == 'min_opt':
        os.environ['COMPlus_JITMinOpts'] = '1'
        os.environ['COMPlus_TieredCompilation'] = '0'
    elif args.optimization_level == 'full_opt':
        os.environ['COMPlus_TieredCompilation'] = '0'

    # The MicroBenchmarks.csproj targets .NET Core 2.0, 2.1, 2.2 and 3.0
    # to avoid a build failure when using older frameworks (error NETSDK1045: The current .NET SDK does not support targeting .NET Core $XYZ)
    # we set the TFM to what the user has provided
    os.environ['PYTHON_SCRIPT_TARGET_FRAMEWORKS'] = ';'.join(args.frameworks)

    # dotnet --info
    dotnet.info(verbose=verbose)

    # .NET micro-benchmarks
    # Restore and build micro-benchmarks
    micro_benchmarks.build(
        args.configuration,
        args.frameworks,
        args.incremental,
        verbose
    )

    # Run micro-benchmarks
    for framework in args.frameworks:
        micro_benchmarks.run(args.configuration, framework, verbose, args)

    __run_benchview_scripts(args, verbose)
    # TODO: Archive artifacts.


if __name__ == "__main__":
    __main(sys.argv[1:])
