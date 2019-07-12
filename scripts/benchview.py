#!/usr/bin/env python3

'''
Support script around BenchView script.
'''

from argparse import ArgumentParser
from collections import namedtuple
from errno import EEXIST
from glob import iglob
from itertools import chain
from logging import getLogger
from os import environ, path
from shutil import which
from urllib.parse import urlparse
from urllib.request import urlopen
from xml.etree import ElementTree
from zipfile import ZipFile

import platform
import sys

from micro_benchmarks import FrameworkAction
from performance.common import get_tools_directory
from performance.common import get_script_path
from performance.common import get_python_executable
from performance.common import make_directory
from performance.common import push_dir
from performance.common import remove_directory
from performance.common import RunCommand
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers

import dotnet


class BenchView:
    '''
    Wrapper class around the BenchView scripts used to serialize performance
    data.
    '''

    def __init__(self, verbose: bool):
        self.__python = get_python_executable()
        self.__tools = path.join(BenchView.get_scripts_directory(), 'tools')
        self.__verbose = verbose

        # TODO: Fix BenchView scripts to use `loggin` instead of `print`.
        #   BenchView scripts perform rudimentary logging using `print` and
        #   this causes `Bad descriptor` error when redirecting output to null.
        #   At the moment, BenchView scripts only output on error or when it
        #   has written the generated output file, so setting `verbose=True`
        #   does not pollute output. In addition, these scripts logging is
        #   more robust, and this flag on will not pollute overall output.
        self.__verbose = True

    @staticmethod
    def get_scripts_directory() -> str:
        '''BenchView scripts install directory.'''
        return get_script_path()

    @property
    def python(self):
        '''
        Gets the absolute path of the executable binary for the Python
        interpreter.
        '''
        return self.__python

    @property
    def tools_directory(self) -> str:
        '''BenchView tools directory.'''
        return self.__tools

    @property
    def verbose(self) -> bool:
        '''Enables/Disables verbosity.'''
        return self.__verbose

    def build(
            self,
            build_type: str,
            subparser: str = None,  # ['none', 'git']
            branch: str = None,
            commit: str = None,
            repository: str = None,
            source_timestamp: str = None) -> None:
        '''Wrapper around BenchView's build.py'''

        if not subparser:
            subparser = 'none'

        cmdline = [
            self.python, path.join(self.tools_directory, 'build.py'),
            subparser,
            '--type', build_type
        ]
        if branch:
            cmdline += ['--branch', branch]
        if commit:
            cmdline += ['--number', commit]
        if repository:
            cmdline += ['--repository', repository]
        if source_timestamp:
            cmdline += ['--source-timestamp', source_timestamp]
        RunCommand(cmdline, verbose=self.verbose).run()

    def machinedata(self, architecture: str) -> None:
        '''Wrapper around BenchView's machinedata.py'''

        cmdline = [
            self.python, path.join(self.tools_directory, 'machinedata.py')
        ]
        # Workaround: https://github.com/workhorsy/py-cpuinfo/issues/112
        if architecture == 'arm64':
            cmdline += ['--machine-manufacturer', 'Unknown']
        RunCommand(cmdline, verbose=self.verbose).run()

    def measurement(self) -> None:
        '''Wrapper around BenchView's measurement.py'''

        common_cmdline = [
            self.python, path.join(self.tools_directory, 'measurement.py'),
            'bdn',
            '--append',
        ]

        full_json_files = []
        pattern = "**/*-full.json"
        getLogger().info(
            'Searching BenchmarkDotNet output files with: %s', pattern
        )

        for full_json_file in iglob(pattern, recursive=True):
            full_json_files.append(full_json_file)

        for full_json_file in full_json_files:
            cmdline = common_cmdline + [full_json_file]
            RunCommand(cmdline, verbose=self.verbose).run()

    def submission_metadata(self, name: str) -> None:
        '''Wrapper around BenchView's submission-metadata.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'submission-metadata.py'),
            '--name', name,
            '--user-email', 'dotnet-bot@microsoft.com'
        ]
        RunCommand(cmdline, verbose=self.verbose).run()

    def submission(
            self,
            architecture: str,
            config_name: str,
            configs: dict,
            machinepool: str,
            jobgroup: str,
            jobtype: str
    ) -> None:
        '''Wrapper around BenchView's submission.py'''
        cmdline = [
            self.python, path.join(self.tools_directory, 'submission.py'),

            'measurement.json',

            '--build', 'build.json',
            '--machine-data', 'machinedata.json',
            '--metadata', 'submission-metadata.json',

            '--group', jobgroup,
            '--type', jobtype,
            '--config-name', config_name,
            '--architecture', architecture,
            '--machinepool', machinepool
        ]
        for key, value in configs.items():
            cmdline += ['--config', key, value]
        RunCommand(cmdline, verbose=self.verbose).run()

    def upload(self, container: str) -> None:
        '''Wrapper around BenchView's upload.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'upload.py'),
            'submission.json',
            '--container', container,
        ]
        RunCommand(cmdline, verbose=self.verbose).run()


BuildInfo = namedtuple('BuildInfo', [
    'subparser',
    'branch',
    'commit_sha',
    'repository',
    'source_timestamp',
])


def __log_script_header(message: str):
    message_length = len(message)
    getLogger().info('-' * message_length)
    getLogger().info(message)
    getLogger().info('-' * message_length)

def __get_build_info(framework: str, args) -> BuildInfo:
    # TODO: Expand this to support: Mono, CoreRT, CoreRun, dotnet.
    #   Could the --cli-* arguments take multiple build info objects from the
    #   command line interface?
    subparser = 'none'
    branch = args.cli_branch
    commit_sha = args.cli_commit_sha
    repository = args.cli_repository
    source_timestamp = args.cli_source_timestamp

    target_framework_moniker = FrameworkAction.get_target_framework_moniker(
        framework
    )

    if args.cli_source_info == 'cli':
        # Retrieve data from the specified dotnet executable.
        commit_sha = dotnet.get_dotnet_sdk(target_framework_moniker, args.cli)
        source_timestamp = dotnet.get_commit_date(
            framework,
            commit_sha,
            repository
        )
    elif args.cli_source_info == 'init-tools':
        # Retrieve data from the installed dotnet tools.
        branch = FrameworkAction.get_branch(target_framework_moniker)
        if not branch:
            err_msg = 'Cannot determine build information for "%s"' % \
                target_framework_moniker
            getLogger().error(err_msg)
            getLogger().error(
                "Build information can be provided using the --cli-* options."
            )
            raise ValueError(err_msg)
        commit_sha = dotnet.get_dotnet_sdk(
            target_framework_moniker,
            which('dotnet')
        )
        repository = 'https://github.com/dotnet/core-sdk'
        source_timestamp = dotnet.get_commit_date(framework, commit_sha)
    elif args.cli_source_info == 'repo':
        # Retrieve data from current repository.
        subparser = 'git'
    elif args.cli_source_info == 'args':
        # All of the required data should already be supplied in the parameters
        if not branch or not commit_sha or not source_timestamp:
            err_msg = 'Cannot determine build information for "%s"' % \
                target_framework_moniker
            getLogger().error(err_msg)
            getLogger().error(
                "Build information must be provided using the --cli-* options."
            )
            raise ValueError(err_msg)
        if not repository:
            repository = 'https://github.com/dotnet/core-sdk'
    else:
        raise ValueError('Unknown build source.')

    return BuildInfo(
        subparser,
        branch,
        commit_sha,
        repository,
        source_timestamp
    )


def __get_os_name():
    '''Attempts to get a uniform OS name across platforms.'''
    if sys.platform == 'win32':
        return '{} {}'.format(platform.system(), platform.release())
    elif sys.platform == 'linux':
        os_name, os_version, _ = platform.linux_distribution()
        return '{} {}'.format(os_name, os_version)
    else:
        return platform.platform()


def __get_working_directory(
        BENCHMARKS_CSPROJ: dotnet.CSharpProject,
        configuration: str,
        framework: str
) -> str:
    glob_fmt = '{BinPath}/{ProjectName}/**/{Configuration}/{TargetFramework}'
    target_framework_moniker = FrameworkAction.get_target_framework_moniker(
        framework
    )
    pattern = glob_fmt.format(
        BinPath=BENCHMARKS_CSPROJ.bin_path,
        ProjectName=BENCHMARKS_CSPROJ.project_name,
        Configuration=configuration,
        TargetFramework=target_framework_moniker
    )

    for path_name in iglob(pattern, recursive=True):
        if path.isdir(path_name):
            return path.abspath(path_name)

    raise RuntimeError(
        "Unable to find target directory for {}.".format(
            target_framework_moniker
        )
    )


def __run_scripts(
        args: list,
        benchviewpy: BenchView,
        framework: str
) -> None:
    '''Runs all BenchView scripts for the specified framework results.'''
    benchviewpy.machinedata(architecture=args.architecture)

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
    # TODO: Hardcoded Jit name. Mono have multiple Jit engines.
    #   This value should be optional, and set when applicable.
    # benchview_config['Jit'] = 'RyuJIT'
    benchview_config['Framework'] = framework
    benchview_config['.NET Compilation Mode'] = 'Tiered'
    benchview_config['OS'] = __get_os_name()
    benchview_config['PGO'] = 'Enabled'
    benchview_config['Profile'] = 'On' if args.enable_pmc else 'Off'

    buildinfo = __get_build_info(framework, args)

    benchviewpy.build(
        build_type=args.benchview_run_type,
        subparser=buildinfo.subparser,
        branch=buildinfo.branch,
        commit=buildinfo.commit_sha,
        repository=buildinfo.repository,
        source_timestamp=buildinfo.source_timestamp
    )

    if urlparse(buildinfo.repository).path and buildinfo.commit_sha:
        submission_name = '%s (%s): %s:%s' % (
            args.benchview_submission_name,
            args.benchview_run_type,
            urlparse(buildinfo.repository).path,
            buildinfo.commit_sha
        )
    else:
        submission_name = '%s (%s): %s' % (
            args.benchview_submission_name,
            args.benchview_run_type,
            framework
        )

    benchviewpy.submission_metadata(name=submission_name)

    benchviewpy.measurement()

    benchviewpy.submission(
        architecture=args.architecture,
        config_name=benchview_config_name,
        configs=benchview_config,
        machinepool=args.benchview_machinepool,
        jobgroup=args.benchview_job_group,
        jobtype=args.benchview_run_type
    )

    if args.upload_to_benchview_container:
        benchviewpy.upload(container=args.upload_to_benchview_container)


def run_scripts(
        args: list,
        verbose: bool,
        BENCHMARKS_CSPROJ: dotnet.CSharpProject
) -> None:
    '''Run BenchView scripts to collect performance data.'''
    if not args.generate_benchview_data:
        return

    __log_script_header('Running BenchView scripts')

    for framework in args.frameworks:
        
        working_directory = path.join(__get_working_directory(
            BENCHMARKS_CSPROJ,
            args.configuration,
            framework
        ), 'BenchmarkDotNet.Artifacts') if not args.bdn_artifacts else path.join(args.bdn_artifacts)

        with push_dir(working_directory):
            benchviewpy = BenchView(verbose)
            __run_scripts(args, benchviewpy, framework)


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''
    Adds new arguments to the specified ArgumentParser object.
    '''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

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
    is_benchview_commit_name_defined = 'BenchviewCommitName' in environ
    default_submission_name = environ['BenchviewCommitName'] \
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
        types of configurations can be: performance mode, configuration,
        profile, etc.'''
    )
    parser.add_argument(
        '--benchview-job-group',
        dest='benchview_job_group',
        required=False,
        default='.NET Performance',
        type=str,
        help='''Category to distinguish different batches of uploads.'''
    )

    return parser


def __main():
    validate_supported_runtime()
    setup_loggers(verbose=True)
    install()


if __name__ == "__main__":
    __main()
