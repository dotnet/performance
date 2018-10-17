#!/usr/bin/env python3

"""
Installs dotnet cli
"""

from argparse import ArgumentParser
from logging import getLogger
from os import chmod, makedirs, path
from stat import S_IRWXU
from sys import argv, platform
from urllib.request import urlretrieve

from performance.common import get_repo_root_path
from performance.common import get_tools_directory
from performance.common import RunCommand
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers


def get_dotnet_directory() -> str:
    '''Gets the default directory where dotnet is to be installed.'''
    return path.join(get_tools_directory(), 'dotnet')


def install(
        architecture: str,
        channel: str,
        verbose: bool) -> None:
    '''
    Downloads dotnet cli into the tools folder.
    '''
    start_msg = "Downloading DotNet Cli"
    getLogger().info('-' * len(start_msg))
    getLogger().info(start_msg)
    getLogger().info('-' * len(start_msg))

    installDir = get_dotnet_directory()
    if not path.exists(installDir):
        makedirs(installDir)

    getLogger().info("Install path: '%s'", installDir)

    # Download appropriate dotnet install script
    dotnetInstallScriptExtension = '.ps1' if platform == 'win32' else '.sh'
    dotnetInstallScriptName = 'dotnet-install' + dotnetInstallScriptExtension
    url = 'https://raw.githubusercontent.com/dotnet/cli/{}/scripts/obtain/'
    dotnetInstallScriptUrl = url.format(channel) + dotnetInstallScriptName

    dotnetInstallScriptPath = path.join(installDir, dotnetInstallScriptName)

    urlretrieve(dotnetInstallScriptUrl, dotnetInstallScriptPath)

    if platform != 'win32':
        chmod(dotnetInstallScriptPath, S_IRWXU)

    dotnetInstallInterpreter = [
        'powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        dotnetInstallScriptPath
    ] if platform == 'win32' else [dotnetInstallScriptPath]

    sdk_channels = [
        'master',
        # 'release/2.2.1xx',
        # 'release/2.1',
        # 'release/2.0.0',
    ]

    # Install Runtime/SDKs
    for sdk_channel in sdk_channels:
        cmdline_args = dotnetInstallInterpreter + [
            '-InstallDir', installDir,
            '-Architecture', architecture,
            '-Channel', sdk_channel
        ]
        RunCommand(cmdline_args, verbose=verbose).run(
            get_repo_root_path()
        )


def __get_supported_channels() -> list:
    return [
        'master',  # Default channel
        'release/2.1.3xx',
        'release/2.0.0',
    ]


def __get_supported_architectures():
    return ['x64', 'x86']


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''
    Adds new arguments to the specified ArgumentParser object.
    '''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    supported_architectures = __get_supported_architectures()
    parser.add_argument(
        '--architecture',
        dest='architecture',
        required=False,
        default=supported_architectures[0],
        choices=supported_architectures,
        help='Architecture of dotnet binaries to be installed.')

    supported_channels = __get_supported_channels()
    parser.add_argument(
        '--channel',
        dest='channel',
        required=False,
        default=supported_channels[0],
        choices=supported_channels,
        help='Download from the Channel specified')

    return parser


def __process_arguments(args: list):
    parser = ArgumentParser(
        description='Downloads dotnet cli',
        allow_abbrev=False)
    parser = add_arguments(parser)
    parser.add_argument(
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
    install(args.architecture, args.channel, args.verbose)


if __name__ == "__main__":
    __main(argv[1:])
