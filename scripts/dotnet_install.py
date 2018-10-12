#!/usr/bin/env python3

from argparse import ArgumentParser
from os import chmod, makedirs, path
from stat import S_IRWXU
from subprocess import Popen
from sys import argv, platform
from urllib.request import urlretrieve

from build.common import get_dotnet_directory


def install(architecture: str, channel: str, runtime_id: str) -> None:
    installDir = get_dotnet_directory()
    if not path.exists(installDir):
        makedirs(installDir)

    print("Install path: {}".format(installDir))

    # Download appropriate dotnet install script
    dotnetInstallScriptExtension = '.ps1' if platform == 'win32' else '.sh'
    dotnetInstallScriptName = 'dotnet-install' + dotnetInstallScriptExtension
    url = 'https://raw.githubusercontent.com/dotnet/cli/{}/scripts/obtain/'
    dotnetInstallScriptUrl = url.format(channel) + dotnetInstallScriptName

    dotnetInstallScriptPath = path.join(installDir, dotnetInstallScriptName)

    urlretrieve(dotnetInstallScriptUrl, dotnetInstallScriptPath)

    if platform == 'win32':
        chmod(dotnetInstallScriptPath, S_IRWXU)

    # run dotnet-install script
    rid = [] if not runtime_id else ['--runtime-id', runtime_id]
    dotnetInstallInterpreter = [
        'powershell.exe',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        dotnetInstallScriptPath
    ] if platform == 'win32' else [dotnetInstallScriptPath]

    runArgs = dotnetInstallInterpreter + [
        '-Runtime', 'dotnet',
        '-Architecture', architecture,
        '-InstallDir', installDir,
        '-Channel', channel
    ] + rid

    p = Popen(' '.join(runArgs), shell=True)
    p.communicate()

    runArgs = dotnetInstallInterpreter + [
        '-Architecture', architecture,
        '-InstallDir', installDir,
        '-Channel', channel
    ]

    p = Popen(' '.join(runArgs), shell=True)
    p.communicate()


def get_supported_channels() -> list:
    return [
        'master',  # Default channel
        'release/2.1.3xx',
        'release/2.0.0',
        'release/1.1.0'
    ]


def get_supported_architectures():
    return ['x64', 'x86']


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')
    supported_architectures = get_supported_architectures()
    parser.add_argument(
        '--architecture',
        dest='architecture',
        required=False,
        default=supported_architectures[0],
        choices=supported_architectures,
        help='Architecture of dotnet binaries to be installed.')
    parser.add_argument(
        '--runtime-id',
        dest='runtime_id',
        required=False,
        default=None,
        help='Installs just a shared runtime, not the entire SDK.')
    supported_channels = get_supported_channels()
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
    return parser.parse_args(args)


def __main(args: list) -> int:
    args = __process_arguments(args)
    install(args.architecture, args.channel, args.runtime_id)
    # TODO: Add to PATH?


if __name__ == "__main__":
    __main(argv[1:])
