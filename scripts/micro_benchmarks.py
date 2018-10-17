#!/usr/bin/env python3

'''
Builds the Benchmarks
'''

from argparse import ArgumentParser, ArgumentTypeError, SUPPRESS
from logging import getLogger
from os import path
from subprocess import CalledProcessError
from traceback import format_exc
from typing import Tuple

import sys

from performance.common import get_repo_root_path
from performance.common import TargetFrameworkAction
from performance.common import remove_directory
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers
from performance.dotnet import DotNetProject, dotnet_info


def get_supported_configurations() -> list:
    '''
    The configuration to use for building the project. The default for most
    projects is 'Release'
    '''
    return ['Release', 'Debug']


def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    '''
    Adds new arguments to the specified ArgumentParser object.
    '''
    def dotnet_configuration(configuration: str) -> str:
        for config in get_supported_configurations():
            is_valid = config.casefold() == configuration.casefold()
            if is_valid:
                return config
        raise ArgumentTypeError(
            'Unknown configuration: {}.'.format(configuration))

    supported_configurations = get_supported_configurations()
    parser.add_argument(
        '-c', '--configuration',
        required=False,
        default=supported_configurations[0],
        choices=supported_configurations,
        type=dotnet_configuration,
        help=SUPPRESS,
    )

    supported_target_frameworks = TargetFrameworkAction\
        .get_supported_target_frameworks()
    parser.add_argument(
        '-f', '--frameworks',
        required=True,
        nargs='*',
        action=TargetFrameworkAction,
        choices=supported_target_frameworks,
        help='Target frameworks to publish for.',
    )

    # BenchmarkDotNet
    parser.add_argument(
        '--category',
        required=False,
        choices=['coreclr', 'corefx'],
        type=str.lower)
    parser.add_argument(
        '--max-iteration-count',
        dest='max_iteration_count',
        type=int,
        default=20)
    parser.add_argument(
        '--min-iteration-count',
        dest='min_iteration_count',
        type=int,
        default=15)

    def valid_file_path(file_path: str) -> str:
        '''Verifies that specified file path exists.'''
        file_path = path.abspath(file_path)
        if not path.isfile(file_path):
            raise ArgumentTypeError('{} does not exist.'.format(file_path))
        return file_path

    parser.add_argument(
        '--corerun-path',
        dest='corerun_path',
        required=False,
        type=valid_file_path,
        help='Path to CoreRun.exe')
    parser.add_argument(
        '--dotnet-path',
        dest='dotnet_path',
        required=False,
        type=valid_file_path,
        help='Path to dotnet.exe')

    return parser


def __process_arguments(args: list) -> Tuple[list, bool]:
    parser = ArgumentParser(
        description="Builds the benchmarks.",
        allow_abbrev=False)

    parser.add_argument(
        '-v', '--verbose',
        required=False,
        default=False,
        action='store_true',
        help='Turns on verbosity (default "False")',
    )

    parser = add_arguments(parser)
    return parser.parse_args(args)


def build(
        configuration: str,
        frameworks: list,
        verbose: bool) -> None:
    '''Restores and builds the benchmarks'''
    __log_script_header("Removing packages, bin and obj folders.")
    packages = path.join(get_repo_root_path(), 'packages')
    binary_folders = [
        packages,
        path.join(BENCHMARKS_CSPROJ.working_directory, 'bin'),
        path.join(BENCHMARKS_CSPROJ.working_directory, 'obj'),
    ]
    for binary_folder in binary_folders:
        remove_directory(path=binary_folder)

    # dotnet restore
    __log_script_header("Restoring .NET micro benchmarks")
    BENCHMARKS_CSPROJ.restore(packages_path=packages, verbose=verbose)

    # dotnet build
    build_title = "Building .NET micro benchmarks for '{}'".format(
        ' '.join(frameworks))
    __log_script_header(build_title)
    args = ['--no-restore']
    BENCHMARKS_CSPROJ.build(configuration, frameworks, verbose, *args)


def run(
        configuration: str,
        framework: str,
        verbose: bool,
        *args) -> None:
    '''Builds the benchmarks'''
    __log_script_header("Running .NET micro benchmarks for '{}'".format(
        framework
    ))
    # dotnet run
    BENCHMARKS_CSPROJ.run(configuration, framework, verbose, *args)


def __log_script_header(message: str):
    getLogger().info('-' * len(message))
    getLogger().info(message)
    getLogger().info('-' * len(message))


BENCHMARKS_CSPROJ = DotNetProject(
    working_directory=path.join(
        get_repo_root_path(), 'src', 'benchmarks', 'micro'),
    csproj_file='MicroBenchmarks.csproj'
)


def __main(args: list) -> int:
    try:
        validate_supported_runtime()
        args = __process_arguments(args)

        category = args.category
        configuration = args.configuration
        corerun_path = args.corerun_path
        dotnet_path = args.dotnet_path
        frameworks = args.frameworks
        verbose = args.verbose

        setup_loggers(verbose=verbose)

        # dotnet --info
        dotnet_info(verbose)

        # dotnet build
        build(configuration, frameworks, verbose)

        for framework in frameworks:
            run_args = [
                # '--no-restore', '--no-build',  # FIXME: netcoreapp2.1 broken? netcoreapp2.0 builds both 2.0 and 2.1?
                '--',
            ]
            if category:
                run_args += ['--allCategories', category]
            if corerun_path:
                run_args += ['--coreRun', corerun_path]
            if dotnet_path:
                run_args += ['--cli', dotnet_path]
            run_args += [
                '--maxIterationCount', str(args.max_iteration_count),
                '--minIterationCount', str(args.min_iteration_count)
            ]
            # dotnet run
            run(configuration, framework, verbose, *run_args)

        return 0
    except CalledProcessError as ex:
        getLogger().error(
            'Command: "%s", exited with status: %s', ex.cmd, ex.returncode)
    except IOError as ex:
        getLogger().error(
            "I/O error (%s): %s: %s", ex.errno, ex.strerror, ex.filename)
    except SystemExit:  # Argparse throws this exception when it exits.
        pass
    except Exception:
        getLogger().error('Unexpected error: %s', sys.exc_info()[0])
        getLogger().error(format_exc())
    return 1


if __name__ == "__main__":
    exit(__main(sys.argv[1:]))
