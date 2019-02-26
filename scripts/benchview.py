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

from performance.common import get_tools_directory
from performance.common import get_python_executable
from performance.common import make_directory
from performance.common import push_dir
from performance.common import remove_directory
from performance.common import RunCommand
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers

import dotnet
import micro_benchmarks


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
        return path.join(
            get_tools_directory(), 'Microsoft.BenchView.JSONFormat')

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
            working_directory: str,
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
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def machinedata(self, working_directory: str, architecture: str) -> None:
        '''Wrapper around BenchView's machinedata.py'''

        cmdline = [
            self.python, path.join(self.tools_directory, 'machinedata.py')
        ]
        # Workaround: https://github.com/workhorsy/py-cpuinfo/issues/112
        if architecture == 'arm64':
            cmdline += ['--machine-manufacturer', 'Unknown']
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def measurement(self, working_directory: str) -> None:
        '''Wrapper around BenchView's measurement.py'''

        common_cmdline = [
            self.python, path.join(self.tools_directory, 'measurement.py'),
            'bdn',
            '--append',
        ]

        full_json_files = []
        with push_dir(working_directory):
            pattern = "BenchmarkDotNet.Artifacts/**/*-full.json"
            getLogger().info(
                'Searching BenchmarkDotNet output files with: %s', pattern
            )

            for full_json_file in iglob(pattern, recursive=True):
                full_json_files.append(full_json_file)

        for full_json_file in full_json_files:
            cmdline = common_cmdline + [full_json_file]
            RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def submission_metadata(self, working_directory: str, name: str) -> None:
        '''Wrapper around BenchView's submission-metadata.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'submission-metadata.py'),
            '--name', name,
            '--user-email', 'dotnet-bot@microsoft.com'
        ]
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def submission(
            self,
            working_directory: str,
            measurement_jsons: list,
            architecture: str,
            config_name: str,
            configs: dict,
            machinepool: str,
            jobgroup: str,
            jobtype: str
    ) -> None:
        '''Wrapper around BenchView's submission.py'''
        if not measurement_jsons:
            raise ValueError("No `measurement.json` were specified.")

        cmdline = [
            self.python, path.join(self.tools_directory, 'submission.py'),

            *measurement_jsons,

            '--build', path.join(
                working_directory, 'build.json'),
            '--machine-data', path.join(
                working_directory, 'machinedata.json'),
            '--metadata', path.join(
                working_directory, 'submission-metadata.json'),

            '--group', jobgroup,
            '--type', jobtype,
            '--config-name', config_name,
            '--architecture', architecture,
            '--machinepool', machinepool
        ]
        for key, value in configs.items():
            cmdline += ['--config', key, value]
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)

    def upload(self, working_directory: str, container: str) -> None:
        '''Wrapper around BenchView's upload.py'''

        cmdline = [
            self.python,
            path.join(self.tools_directory, 'upload.py'),
            path.join(working_directory, 'submission.json'),
            '--container', container,
        ]
        RunCommand(cmdline, verbose=self.verbose).run(working_directory)


BuildInfo = namedtuple('BuildInfo', [
    'subparser',
    'branch',
    'commit_sha',
    'repository',
    'source_timestamp',
])


def install():
    '''
    Downloads scripts that serialize/upload performance data to BenchView.
    '''
    __log_script_header()

    url_str = __get_latest_benchview_script_version()
    benchview_path = BenchView.get_scripts_directory()

    if path.isdir(benchview_path):
        remove_directory(benchview_path)
    if not path.exists(benchview_path):
        make_directory(benchview_path)

    getLogger().info('%s -> %s', url_str, benchview_path)

    zipfile_path = __download_zip_file(url_str, benchview_path)
    __unzip_file(zipfile_path, benchview_path)


def __download_zip_file(url_str: str, output_path: str):
    if not url_str:
        raise ValueError('URL was not defined.')
    url = urlparse(url_str)
    if not url.scheme or not url.netloc or not url.path:
        raise ValueError('Invalid URL: {}'.format(url_str))
    if not output_path:
        raise ValueError('Invalid output directory: {}'.format(output_path))

    zip_file_name = path.basename(url.path)
    zip_file_name = path.join(output_path, zip_file_name)

    if not path.splitext(zip_file_name)[1] == '.zip':
        zip_file_name = '{}.zip'.format(zip_file_name)

    if path.isfile(zip_file_name):
        raise FileExistsError(EEXIST, 'File exists', zip_file_name)

    with urlopen(url_str) as response, open(zip_file_name, 'wb') as zipfile:
        zipfile.write(response.read())
    return zip_file_name


def __unzip_file(file_path: str, output_path: str):
    '''Extract all members from the archive to the specified directory.'''
    with ZipFile(file_path, 'r') as zipfile:
        zipfile.extractall(output_path)


def __log_script_header():
    start_msg = "Downloading BenchView scripts"
    getLogger().info('-' * len(start_msg))
    getLogger().info(start_msg)
    getLogger().info('-' * len(start_msg))


def __get_latest_benchview_script_version() -> str:
    scheme_authority = 'http://benchviewtestfeed.azurewebsites.net'
    fullpath = "/nuget/FindPackagesById()?id='Microsoft.BenchView.JSONFormat'"
    url_str = '{}{}'.format(scheme_authority, fullpath)
    with urlopen(url_str) as response:
        tree = ElementTree.parse(response)
        root = tree.getroot()
        namespace = root.tag[0:root.tag.index('}') + 1]
        xpath = '{0}entry/{0}content[@type="application/zip"]'.format(
            namespace)
        packages = [element.get('src') for element in tree.findall(xpath)]
        if not packages:
            raise RuntimeError('No BenchView packages found.')
        packages.sort()
        return packages[-1]


def __get_build_info(args, target_framework_moniker: str) -> BuildInfo:
    # TODO: Expand this to support: Mono, CoreRT, CoreRun, dotnet.
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

    return BuildInfo(
        subparser,
        branch,
        commit_sha,
        repository,
        source_timestamp
    )


def run_scripts(
        args: list,
        verbose: bool,
        BENCHMARKS_CSPROJ: dotnet.CSharpProject
) -> None:
    '''Run BenchView scripts to collect performance data.'''
    if not args.generate_benchview_data:
        return

    # TODO: Delete previously generated BenchView data (*.json)

    benchviewpy = BenchView(verbose)
    bin_directory = BENCHMARKS_CSPROJ.bin_path

    # BenchView submission-metadata.py
    submission_name = '%s (%s)' % (
        args.benchview_submission_name,
        args.benchview_run_type
    )

    rolling_data = args.benchview_run_type == 'rolling' and \
        'GIT_BRANCH_WITHOUT_ORIGIN' in environ and \
        'GIT_COMMIT' in environ

    if rolling_data:
        submission_name += ': %s:%s' % (
            environ['GIT_BRANCH_WITHOUT_ORIGIN'],
            environ['GIT_COMMIT']
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

    # TODO: Hardcoded Jit name. Mono have multiple Jit engines.
    #   This value should be optional, and set when applicable.
    # benchview_config['Jit'] = 'RyuJIT'

    benchview_config['.NET Compilation Mode'] = args.dotnet_compilation_mode

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

            jobGroup = '.NET Performance'
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

    return parser


def __main():
    validate_supported_runtime()
    setup_loggers(verbose=True)
    install()


if __name__ == "__main__":
    __main()
