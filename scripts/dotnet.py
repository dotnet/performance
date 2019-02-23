#!/usr/bin/env python3

"""
Contains the functionality around DotNet Cli.
"""

from argparse import ArgumentParser
from collections import namedtuple
from glob import iglob
from json import loads
from logging import getLogger
from os import chmod, environ, makedirs, path, pathsep
from stat import S_IRWXU
from subprocess import check_output
from sys import argv, platform
from urllib.parse import urlparse
from urllib.request import urlopen, urlretrieve

from performance.common import get_repo_root_path
from performance.common import get_tools_directory
from performance.common import push_dir
from performance.common import RunCommand
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers


def info(verbose: bool) -> None:
    """
    Executes `dotnet --info` in order to get the .NET Core information from the
    dotnet executable.
    """
    cmdline = ['dotnet', '--info']
    RunCommand(cmdline, verbose=verbose).run()


CSharpProjFile = namedtuple('CSharpProjFile', [
    'file_name',
    'working_directory'
])


class CSharpProject:
    '''
    This is a class wrapper around the `dotnet` command line interface.
    Remark: It assumes dotnet is already in the PATH.
    '''

    def __init__(self, project: CSharpProjFile, bin_directory: str):
        if not project.file_name:
            raise TypeError('C# file name cannot be null.')
        if not project.working_directory:
            raise TypeError('C# working directory cannot be null.')
        if not bin_directory:
            raise TypeError('bin folder cannot be null.')

        self.__csproj_file = path.abspath(project.file_name)
        self.__working_directory = path.abspath(project.working_directory)
        self.__bin_directory = bin_directory

        if not path.isdir(self.__working_directory):
            raise ValueError(
                'Specified working directory: {}, does not exist.'.format(
                    self.__working_directory
                )
            )
        if not path.isfile(self.__csproj_file):
            raise ValueError(
                'Specified project file: {}, does not exist.'.format(
                    self.__csproj_file
                )
            )

    @property
    def working_directory(self) -> str:
        '''Gets the working directory for the dotnet process to be started.'''
        return self.__working_directory

    @property
    def csproj_file(self) -> str:
        '''Gets the project file to run the dotnet cli against.'''
        return self.__csproj_file

    @property
    def bin_path(self) -> str:
        '''Gets the directory in which the built binaries will be placed.'''
        return self.__bin_directory

    def restore(self, packages_path: str, verbose: bool) -> None:
        '''
        Calls dotnet to restore the dependencies and tools of the specified
        project.

        Keyword arguments:
        packages_path -- The directory to restore packages to.
        '''
        if not packages_path:
            raise TypeError('Unspecified packages directory.')
        cmdline = [
            'dotnet', 'restore',
            self.csproj_file,
            '--packages', packages_path
        ]
        RunCommand(cmdline, verbose=verbose).run(
            self.working_directory)

    def build(self,
              configuration: str,
              target_framework_monikers: list,
              verbose: bool,
              *args) -> None:
        '''Calls dotnet to build the specified project.'''
        if not target_framework_monikers:  # Build all supported frameworks.
            cmdline = [
                'dotnet', 'build',
                self.csproj_file,
                '--configuration', configuration,
                '--no-restore',
            ]
            if args:
                cmdline = cmdline + list(args)
            RunCommand(cmdline, verbose=verbose).run(
                self.working_directory)
        else:  # Only build specified frameworks
            for target_framework_moniker in target_framework_monikers:
                cmdline = [
                    'dotnet', 'build',
                    self.csproj_file,
                    '--configuration', configuration,
                    '--framework', target_framework_moniker,
                    '--no-restore',
                ]
                if args:
                    cmdline = cmdline + list(args)
                RunCommand(cmdline, verbose=verbose).run(
                    self.working_directory)

    def run(self,
            configuration: str,
            target_framework_moniker: str,
            verbose: bool,
            *args) -> None:
        '''
        Calls dotnet to run a .NET project output.
        '''

        cmdline = [
            'dotnet', 'run',
            '--project', self.csproj_file,
            '--configuration', configuration,
            '--framework', target_framework_moniker,
            '--no-restore', '--no-build',
        ]

        if args:
            cmdline = cmdline + list(args)
        RunCommand(cmdline, verbose=verbose).run(
            self.working_directory)


def get_host_commit_sha(dotnet_path: str = None) -> str:
    """Gets the dotnet Host commit sha from the `dotnet --info` command."""
    if not dotnet_path:
        dotnet_path = 'dotnet'

    output = check_output([dotnet_path, '--info'])

    foundHost = False
    for line in output.splitlines():
        decoded_line = line.decode('utf-8')

        # First look for the host information, since that is the sha we are
        # looking for. Then, grab the first Commit line we find, which will be
        # the sha of the framework we are testing
        #
        # Sample input for .NET Core 2.1+:
        # Host (useful for support):
        #   Version: 3.0.0-preview1-27018-05
        #   Commit:  7a7ca06512
        #
        # Sample input for .NET Core 2.0:
        # Product Information:
        #   Version:            2.1.202
        #   Commit SHA-1 hash:  281caedada
        if 'Host' in decoded_line:
            foundHost = True
        elif 'Product' in decoded_line:
            foundHost = True
        elif foundHost and 'Commit' in decoded_line:
            return decoded_line.strip().split()[-1]

    raise RuntimeError('.NET Host Commit sha not found.')


def get_commit_date(commit_sha: str, repository: str = None) -> str:
    '''
    Gets the .NET Core committer date using the GitHub Web API from the
    https://github.com/dotnet/core-setup repository.
    '''
    if not commit_sha:
        raise ValueError('.NET Commit sha was not defined.')

    if repository is None:
        urlformat = 'https://api.github.com/repos/dotnet/core-setup/commits/%s'
        url = urlformat % commit_sha
    else:
        url_path = urlparse(repository).path
        tokens = url_path.split("/")
        if len(tokens) != 3:
            raise ValueError('Unable to determine owner and repo from url.')
        owner = tokens[1]
        repo = tokens[2]
        urlformat = 'https://api.github.com/repos/%s/%s/commits/%s'
        url = urlformat % (owner, repo, commit_sha)

    with urlopen(url) as response:
        item = loads(response.read().decode('utf-8'))
        build_timestamp = item['commit']['committer']['date']

    if not build_timestamp:
        raise RuntimeError(
            'Could not get timestamp for commit %s' % commit_sha)
    return build_timestamp


def get_build_directory(
        bin_directory: str,
        configuration: str,
        target_framework_moniker: str) -> None:
    '''
    Gets the  output directory where the built artifacts are in with
    respect to the specified bin_directory.
    '''
    with push_dir(bin_directory):
        return path.join(
            bin_directory,
            __find_build_directory(
                configuration=configuration,
                target_framework_moniker=target_framework_moniker,
            )
        )


def __find_build_directory(
        configuration: str,
        target_framework_moniker: str) -> str:
    '''
    Attempts to get the output directory where the built artifacts are in
    with respect to the current working directory.
    '''
    pattern = '**/{Configuration}/{TargetFramework}'.format(
        Configuration=configuration,
        TargetFramework=target_framework_moniker
    )

    for path_name in iglob(pattern, recursive=True):
        if path.isdir(path_name):
            return path_name

    raise ValueError(
        'Unable to determine directory for the specified pattern.')


def __get_directory(architecture: str) -> str:
    '''Gets the default directory where dotnet is to be installed.'''
    return path.join(get_tools_directory(), 'dotnet', architecture)


def install(
        architecture: str,
        channels: list,
        verbose: bool,
        install_dir: str = None) -> None:
    '''
    Downloads dotnet cli into the tools folder.
    '''
    start_msg = "Downloading DotNet Cli"
    getLogger().info('-' * len(start_msg))
    getLogger().info(start_msg)
    getLogger().info('-' * len(start_msg))

    if not install_dir:
        install_dir = __get_directory(architecture)
    if not path.exists(install_dir):
        makedirs(install_dir)

    getLogger().info("DotNet Install Path: '%s'", install_dir)

    # Download appropriate dotnet install script
    dotnetInstallScriptExtension = '.ps1' if platform == 'win32' else '.sh'
    dotnetInstallScriptName = 'dotnet-install' + dotnetInstallScriptExtension
    url = 'https://dot.net/v1/'
    dotnetInstallScriptUrl = url + dotnetInstallScriptName

    dotnetInstallScriptPath = path.join(install_dir, dotnetInstallScriptName)

    getLogger().info('Downloading %s', dotnetInstallScriptUrl)
    urlretrieve(dotnetInstallScriptUrl, dotnetInstallScriptPath)

    if platform != 'win32':
        chmod(dotnetInstallScriptPath, S_IRWXU)

    dotnetInstallInterpreter = [
        'powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        dotnetInstallScriptPath
    ] if platform == 'win32' else [dotnetInstallScriptPath]

    # Install Runtime/SDKs
    for channel in channels:
        cmdline_args = dotnetInstallInterpreter + [
            '-InstallDir', install_dir,
            '-Architecture', architecture,
            '-Channel', channel,
        ]
        RunCommand(cmdline_args, verbose=verbose).run(
            get_repo_root_path()
        )

    # Set DotNet Cli environment variables.
    environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    environ['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    environ['UseSharedCompilation'] = 'false'

    # Add installed dotnet cli to PATH
    environ["PATH"] = install_dir + pathsep + environ["PATH"]


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''
    Adds new arguments to the specified ArgumentParser object.
    '''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    SUPPORTED_ARCHITECTURES = [
        'x64',  # Default architecture
        'x86',
        'arm32',
        'arm64',
    ]
    parser.add_argument(
        '--architecture',
        dest='architecture',
        required=False,
        default=SUPPORTED_ARCHITECTURES[0],
        choices=SUPPORTED_ARCHITECTURES,
        help='Architecture of DotNet Cli binaries to be installed.'
    )

    return parser


def __process_arguments(args: list):
    parser = ArgumentParser(
        description='DotNet Cli wrapper.',
        allow_abbrev=False
    )
    subparsers = parser.add_subparsers(
        title='Subcommands',
        description='Supported DotNet Cli subcommands'
    )
    install_parser = subparsers.add_parser(
        'install',
        allow_abbrev=False,
        help='Installs dotnet cli',
    )

    # TODO: Could pull this information from repository.
    SUPPORTED_CHANNELS = [
        'master',  # Default channel
        '2.2',
        '2.1',
        '2.0',
        'LTS',
    ]
    install_parser.add_argument(
        '--channels',
        dest='channels',
        required=False,
        nargs='+',
        default=[SUPPORTED_CHANNELS[0]],
        choices=SUPPORTED_CHANNELS,
        help='Download DotNet Cli from the Channel specified.'
    )

    install_parser = add_arguments(install_parser)

    # private install arguments.
    install_parser.add_argument(
        '--install-dir',
        dest='install_dir',
        required=False,
        type=str,
        help='''Path to where to install dotnet. Note that binaries will be '''
             '''placed directly in a given directory.''',
    )
    install_parser.add_argument(
        '-v', '--verbose',
        required=False,
        default=False,
        action='store_true',
        help='Turns on verbosity (default "False")',
    )
    return parser.parse_args(args)


def __main(args: list) -> int:
    validate_supported_runtime()
    args = __process_arguments(args)
    setup_loggers(verbose=args.verbose)
    install(
        architecture=args.architecture,
        channels=args.channels,
        verbose=args.verbose,
        install_dir=args.install_dir,
    )


if __name__ == "__main__":
    __main(argv[1:])
