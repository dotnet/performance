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

from argparse import Action, ArgumentParser, ArgumentTypeError
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


class DotNetPerformanceModes(Action):
    '''
    Default: (Currently Tiered)

    NoTiering: (Formerly called full_opt)
        COMPlus_TieredCompilation=0
            This includes R2R code, useful for comparison against Default and
            JitOnly for changes to R2R code or tiering.

    JitOnly: Maybe could be called FullOptJitOnly to make it clear what it does
        COMPlus_TieredCompilation=0
        COMPlus_ReadyToRun=0
            This is JIT-only, useful for comparison against Default and NoTier
            for changes to R2R code or tiering.

    MinOpt:
        COMPlus_TieredCompilation=0
        COMPlus_JITMinOpts=1
            Uses minopt-JIT for methods that do not have pregenerated code,
            useful for startup time comparisons in scenario benchmarks that
            include a startup time measurement (probably not for
            microbenchmarks), probably not useful for a PR.

    For PRs it is recommended to kick off Default, NoTiering, and JitOnly modes
    '''

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            # Remove potentially set environments.
            if 'COMPlus_TieredCompilation' in os.environ:
                os.environ.pop('COMPlus_TieredCompilation')
            if 'COMPlus_ReadyToRun' in os.environ:
                os.environ.pop('COMPlus_ReadyToRun')
            if 'COMPlus_JITMinOpts' in os.environ:
                os.environ.pop('COMPlus_JITMinOpts')

            # Configure .NET Runtime
            if values == 'Default':
                pass
            elif values == 'NoTiering':
                os.environ['COMPlus_TieredCompilation'] = '0'
            elif values == 'JitOnly':
                os.environ['COMPlus_TieredCompilation'] = '0'
                os.environ['COMPlus_ReadyToRun'] = '0'
            elif values == 'MinOpt':
                os.environ['COMPlus_TieredCompilation'] = '0'
                os.environ['COMPlus_JITMinOpts'] = '1'
            else:
                raise ArgumentTypeError(
                    'Unknown mode: {}'.format(values)
                )

            setattr(namespace, self.dest, values)

    @staticmethod
    def modes() -> list:
        '''Available .NET Performance modes.'''
        return ['Default', 'NoTiering', 'JitOnly', 'MinOpt']

    @staticmethod
    def validate(usr_mode: str) -> str:
        '''Default .NET performance mode.'''
        requested_mode = None
        for mode in DotNetPerformanceModes.modes():
            if usr_mode.casefold() == mode.casefold():
                requested_mode = mode
                break
        if not requested_mode:
            raise ArgumentTypeError('Unknown mode: {}'.format(usr_mode))
        return requested_mode

    @staticmethod
    def tiered() -> str:
        '''Default .NET performance mode.'''
        return DotNetPerformanceModes.modes()[0]


if sys.platform == 'linux' and "linux_distribution" not in dir(platform):
    MESSAGE = '''The `linux_distribution` method is missing from ''' \
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
    getLogger().error(MESSAGE, os.linesep, os.linesep, os.linesep)
    exit(1)


def init_tools(
        architecture: str,
        target_framework_monikers: list,
        verbose: bool) -> None:
    '''
    Install tools used by this repository into the tools folder.
    This function writes a semaphore file when tools have been successfully
    installed in order to avoid reinstalling them on every rerun.
    '''
    getLogger().info('Installing tools.')
    channels = [
        micro_benchmarks.FrameworkAction.get_channel(
            target_framework_moniker)
        for target_framework_moniker in target_framework_monikers
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
        '--dotnet-performance-mode',
        dest='dotnet_performance_mode',
        required=False,
        action=DotNetPerformanceModes,
        choices=DotNetPerformanceModes.modes(),
        default=DotNetPerformanceModes.tiered(),
        type=DotNetPerformanceModes.validate,
        help='''Different performance modes that can be set to change '''
             '''the .NET runtime behavior'''
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

    # BenchView acquisition, and fuctionality
    parser = benchview.add_arguments(parser)

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


def __get_build_info(
        args,
        target_framework_moniker: str
) -> benchview.BuildInfo:
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
        branch = micro_benchmarks.FrameworkAction.get_channel(
            target_framework_moniker
        )
        if not branch:
            err_msg = 'Cannot determine build information for "%s"' % \
                target_framework_moniker
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


def __run_benchview_scripts(
        args: list,
        verbose: bool,
        BENCHMARKS_CSPROJ: dotnet.CSharpProject
) -> None:
    '''Run BenchView scripts to collect performance data.'''
    if not args.generate_benchview_data:
        return

    # TODO: Delete previously generated BenchView data (*.json)

    benchviewpy = benchview.BenchView(verbose)
    bin_directory = BENCHMARKS_CSPROJ.bin_path

    # BenchView submission-metadata.py
    # TODO: Simplify logic. This should be removed and unify repo data.
    submission_name = args.benchview_submission_name
    is_pr = args.benchview_run_type == 'private' and \
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
    benchviewpy.machinedata(
        working_directory=bin_directory,
        architecture=args.architecture)

    for framework in args.frameworks:
        target_framework_moniker = micro_benchmarks \
            .FrameworkAction \
            .get_target_framework_moniker(framework)
        buildinfo = __get_build_info(args, target_framework_moniker)

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
            target_framework_moniker=target_framework_moniker,
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

    # Generate configurations.
    def __get_os_name():
        if sys.platform == 'win32':
            return '{} {}'.format(platform.system(), platform.release())
        elif sys.platform == 'linux':
            os_name, os_version, _ = platform.linux_distribution()
            return '{}{}'.format(os_name, os_version)
        else:
            return platform.platform()

    benchview_config['Jit'] = 'RyuJIT'  # TODO: Hardcoded Jit name.
    benchview_config['PerformanceMode'] = args.dotnet_performance_mode
    benchview_config['OS'] = __get_os_name()
    benchview_config['Profile'] = 'On' if args.enable_pmc else 'Off'

    # Find all measurement.json
    with push_dir(bin_directory):
        for framework in args.frameworks:
            target_framework_moniker = micro_benchmarks \
                .FrameworkAction \
                .get_target_framework_moniker(framework)
            glob_format = '**/%s/%s/measurement.json' % (
                args.configuration,
                target_framework_moniker
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
                architecture=args.architecture,
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

    target_framework_monikers = micro_benchmarks \
        .FrameworkAction \
        .get_target_framework_monikers(args.frameworks)
    # Acquire necessary tools (dotnet, and BenchView)
    init_tools(
        architecture=args.architecture,
        target_framework_monikers=target_framework_monikers,
        verbose=verbose
    )

    # WORKAROUND
    # The MicroBenchmarks.csproj targets .NET Core 2.0, 2.1, 2.2 and 3.0
    # to avoid a build failure when using older frameworks (error NETSDK1045:
    # The current .NET SDK does not support targeting .NET Core $XYZ)
    # we set the TFM to what the user has provided.
    os.environ['PYTHON_SCRIPT_TARGET_FRAMEWORKS'] = ';'.join(
        target_framework_monikers
    )

    # dotnet --info
    dotnet.info(verbose=verbose)

    BENCHMARKS_CSPROJ = dotnet.CSharpProject(
        project=args.csprojfile,
        bin_directory=args.bin_directory
    )

    if not args.run_only:
        # .NET micro-benchmarks
        # Restore and build micro-benchmarks
        micro_benchmarks.build(
            BENCHMARKS_CSPROJ,
            args.configuration,
            target_framework_monikers,
            args.incremental,
            verbose
        )

    # Run micro-benchmarks
    if not args.build_only:
        for framework in args.frameworks:
            micro_benchmarks.run(
                BENCHMARKS_CSPROJ,
                args.configuration,
                framework,
                verbose,
                args
            )

        __run_benchview_scripts(args, verbose, BENCHMARKS_CSPROJ)
        # TODO: Archive artifacts.


if __name__ == "__main__":
    __main(sys.argv[1:])
