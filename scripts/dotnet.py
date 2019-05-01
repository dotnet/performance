#!/usr/bin/env python3

"""
Contains the functionality around DotNet Cli.
"""

from argparse import Action, ArgumentParser, ArgumentTypeError
from collections import namedtuple
from glob import iglob
from json import loads
from logging import getLogger
from os import chmod, environ, listdir, makedirs, path, pathsep
from stat import S_IRWXU
from subprocess import check_output
from sys import argv, platform
from urllib.parse import urlparse
from urllib.request import urlopen, urlretrieve

import re

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


def __log_script_header(message: str):
    message_length = len(message)
    getLogger().info('-' * message_length)
    getLogger().info(message)
    getLogger().info('-' * message_length)


CSharpProjFile = namedtuple('CSharpProjFile', [
    'file_name',
    'working_directory'
])


class CompilationAction(Action):
    '''
    Tiered: (Default)

    NoTiering: Tiering is disabled, but R2R code is not disabled.
        This includes R2R code, useful for comparison against Tiered and
        FullyJittedNoTiering for changes to R2R code or tiering.

    FullyJittedNoTiering: Tiering and R2R are disabled.
        This is JIT-only, useful for comparison against Tiered and NoTiering
        for changes to R2R code or tiering.

    MinOpt:
        Uses minopt-JIT for methods that do not have pregenerated code, useful
        for startup time comparisons in scenario benchmarks that include a
        startup time measurement (probably not for microbenchmarks), probably
        not useful for a PR.

    For PRs it is recommended to kick off a Tiered run, and being able to
    manually kick-off NoTiering and FullyJittedNoTiering modes when needed.
    '''
    # TODO: Would 'Default' make sense for .NET Framework / CoreRT / Mono?
    # TODO: Should only be required for benchmark execution under certain tools

    TIERED = 'Tiered'
    NO_TIERING = 'NoTiering'
    FULLY_JITTED_NO_TIERING = 'FullyJittedNoTiering'
    MIN_OPT = 'MinOpt'

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            if values not in CompilationAction.modes():
                raise ArgumentTypeError('Unknown mode: {}'.format(values))
            setattr(namespace, self.dest, values)

    @staticmethod
    def __set_mode(mode: str) -> None:
        # Remove potentially set environments.
        COMPLUS_ENVIRONMENTS = [
            'COMPlus_JITMinOpts',
            'COMPlus_ReadyToRun',
            'COMPlus_TieredCompilation',
            'COMPlus_ZapDisable',
        ]
        for complus_environment in COMPLUS_ENVIRONMENTS:
            if complus_environment in environ:
                environ.pop(complus_environment)

        # Configure .NET Runtime
        if mode == CompilationAction.TIERED:
            environ['COMPlus_TieredCompilation'] = '1'
        elif mode == CompilationAction.NO_TIERING:
            environ['COMPlus_TieredCompilation'] = '0'
        elif mode == CompilationAction.FULLY_JITTED_NO_TIERING:
            environ['COMPlus_ReadyToRun'] = '0'
            environ['COMPlus_TieredCompilation'] = '0'
            environ['COMPlus_ZapDisable'] = '1'
        elif mode == CompilationAction.MIN_OPT:
            environ['COMPlus_JITMinOpts'] = '1'
            environ['COMPlus_TieredCompilation'] = '0'
        else:
            raise ArgumentTypeError('Unknown mode: {}'.format(mode))

    @staticmethod
    def validate(usr_mode: str) -> str:
        '''Validate user input.'''
        requested_mode = None
        for mode in CompilationAction.modes():
            if usr_mode.casefold() == mode.casefold():
                requested_mode = mode
                break
        if not requested_mode:
            raise ArgumentTypeError('Unknown mode: {}'.format(usr_mode))
        CompilationAction.__set_mode(requested_mode)
        return requested_mode

    @staticmethod
    def modes() -> list:
        '''Available .NET Performance modes.'''
        return [
            CompilationAction.TIERED,
            CompilationAction.NO_TIERING,
            CompilationAction.FULLY_JITTED_NO_TIERING,
            CompilationAction.MIN_OPT
        ]

    @staticmethod
    def tiered() -> str:
        '''Default .NET performance mode.'''
        return CompilationAction.modes()[0]  # Tiered

    @staticmethod
    def help_text() -> str:
        '''Gets the help string describing the different compilation modes.'''
        return '''Different compilation modes that can be set to change the
        .NET compilation behavior. The different modes are: {}: (Default);
        {}: tiering is disabled, but includes R2R code, and it is useful for
        comparison against Tiered; {}: This is JIT-only, useful for comparison
        against Tiered and NoTier for changes to R2R code or tiering; {}: uses
        minopt-JIT for methods that do not have pregenerated code, and useful
        for startup time comparisons in scenario benchmarks that include a
        startup time measurement (probably not for microbenchmarks), probably
        not useful for a PR.'''.format(
            CompilationAction.TIERED,
            CompilationAction.NO_TIERING,
            CompilationAction.FULLY_JITTED_NO_TIERING,
            CompilationAction.MIN_OPT
        )


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
    def project_name(self) -> str:
        '''Gets the project name.'''
        return path.splitext(path.basename(self.__csproj_file))[0]

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

    @staticmethod
    def __print_complus_environment() -> None:
        getLogger().info('-' * 50)
        getLogger().info('Dumping COMPlus environment:')
        COMPLUS_PREFIX = 'COMPlus'
        for env in environ:
            if env[:len(COMPLUS_PREFIX)].lower() == COMPLUS_PREFIX.lower():
                getLogger().info('  "%s=%s"', env, environ[env])
        getLogger().info('-' * 50)

    def run(self,
            configuration: str,
            target_framework_moniker: str,
            verbose: bool,
            *args) -> None:
        '''
        Calls dotnet to run a .NET project output.
        '''
        CSharpProject.__print_complus_environment()
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


def get_dotnet_sdk(framework: str, dotnet_path: str = None) -> str:
    """Gets the dotnet Host commit sha from the `dotnet --info` command."""
    if not framework:
        raise TypeError(
            "The target framework to get information for was not specified."
        )
    if not dotnet_path:
        dotnet_path = 'dotnet'

    groups = re.search(r"^netcoreapp(\d)\.(\d)$", framework)
    if not groups:
        raise ValueError("Unknown target framework: {}".format(framework))

    FrameworkVersion = namedtuple('FrameworkVersion', ['major', 'minor'])
    version = FrameworkVersion(int(groups.group(1)), int(groups.group(2)))

    output = check_output([dotnet_path, '--info'])

    for line in output.splitlines():
        decoded_line = line.decode('utf-8')

        # The .NET Command Line Tools `--info` had a different output in 2.0
        # This line seems commons in all Cli, so we can use the base path to
        # get information about the .NET SDK/Runtime
        groups = re.search(r"^ +Base Path\: +(\S+)$", decoded_line)
        if groups:
            break

    if not groups:
        raise RuntimeError(
            'Did not find "Base Path:" entry on the `dotnet --info` command'
        )

    base_path = groups.group(1)
    sdk_path = path.abspath(path.join(base_path, '..'))
    sdks = [
        d for d in listdir(sdk_path) if path.isdir(path.join(sdk_path, d))
    ]
    sdks.sort(reverse=True)

    # Determine the SDK being used.
    # Attempt 1: Try to use exact match.
    sdk = next((f for f in sdks if f.startswith(
        "{}.{}".format(version.major, version.minor))), None)
    if not sdk:
        # Attempt 2: Increase the minor version by 1 and retry.
        sdk = next((f for f in sdks if f.startswith(
            "{}.{}".format(version.major, version.minor + 1))), None)

    if not sdk:
        raise RuntimeError(
            "Unable to determine the .NET SDK used for {}".format(framework)
        )

    with open(path.join(sdk_path, sdk, '.version')) as sdk_version_file:
        return sdk_version_file.readline().strip()
    raise RuntimeError("Unable to retrieve information about the .NET SDK.")


def get_commit_date(
        framework: str,
        commit_sha: str,
        repository: str = None
) -> str:
    '''
    Gets the .NET Core committer date using the GitHub Web API from the
    repository.
    '''
    if not framework:
        raise ValueError('Target framework was not defined.')
    if not commit_sha:
        raise ValueError('.NET Commit sha was not defined.')

    url = None
    urlformat = 'https://api.github.com/repos/%s/%s/commits/%s'
    if repository is None:
        # The origin of the repo where the commit belongs to has changed
        # between release. Here we attempt to naively guess the repo.
        repo = 'core-sdk' if framework == 'netcoreapp3.0' else 'cli'
        url = urlformat % ('dotnet', repo, commit_sha)
    else:
        url_path = urlparse(repository).path
        tokens = url_path.split("/")
        if len(tokens) != 3:
            raise ValueError('Unable to determine owner and repo from url.')
        owner = tokens[1]
        repo = tokens[2]
        url = urlformat % (owner, repo, commit_sha)

    build_timestamp = None
    with urlopen(url) as response:
        getLogger().info("Commit: %s", url)
        item = loads(response.read().decode('utf-8'))
        build_timestamp = item['commit']['committer']['date']

    if not build_timestamp:
        raise RuntimeError(
            'Could not get timestamp for commit %s' % commit_sha)
    return build_timestamp


def get_build_directory(
        bin_directory: str,
        project_name: str,
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
                project_name=project_name,
                target_framework_moniker=target_framework_moniker,
            )
        )


def __find_build_directory(
        configuration: str,
        project_name: str,
        target_framework_moniker: str) -> str:
    '''
    Attempts to get the output directory where the built artifacts are in
    with respect to the current working directory.
    '''
    pattern = '**/{ProjectName}/**/{Configuration}/{TargetFramework}'.format(
        ProjectName=project_name,
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
        version: str,
        verbose: bool,
        install_dir: str = None) -> None:
    '''
    Downloads dotnet cli into the tools folder.
    '''
    __log_script_header("Downloading DotNet Cli")

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

    # If Version is supplied, pull down the specified version

    cmdline_args = dotnetInstallInterpreter + [
            '-InstallDir', install_dir,
            '-Architecture', architecture
    ]
    if version is not None:
        cmdline_args = cmdline_args + [
            '-Version', version,
        ]
        RunCommand(cmdline_args, verbose=verbose).run(
            get_repo_root_path()
        )
    else:
        # Install Runtime/SDKs
        for channel in channels:
            cmdline_args = cmdline_args + [
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

    # .NET Compilation modes.
    parser.add_argument(
        '--dotnet-compilation-mode',
        dest='dotnet_compilation_mode',
        required=False,
        action=CompilationAction,
        choices=CompilationAction.modes(),
        default=CompilationAction.tiered(),
        type=CompilationAction.validate,
        help='{}'.format(CompilationAction.help_text())
    )

    def __is_valid_sdk_version(version:str) -> str:
        try:
            if version is None or re.search('\d\.\d+\.\d+', version):
                return version
            else:
                raise ValueError
        except ValueError:
            raise ArgumentTypeError(
                'Version "{}" is in the wrong format'.format(version))

    parser.add_argument(
        '--dotnet-version',
        dest="dotnet_version",
        required=False,
        default=None,
        type=__is_valid_sdk_version,
        help='Version of the dotnet cli to install in the A.B.C format'
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

    install_parser.add_argument(
        '--version',
        dest='version',
        required=False,
        default=None,
        help='Download DotNet Cli at the specified version.'
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
        version=args.dotnet_version,
        verbose=args.verbose,
        install_dir=args.install_dir,
    )


if __name__ == "__main__":
    __main(argv[1:])
