#!/usr/bin/env python3

'''
Builds the Benchmarks
'''

from argparse import Action
from argparse import ArgumentError
from argparse import ArgumentParser
from argparse import ArgumentTypeError
from argparse import SUPPRESS
from io import StringIO
from logging import getLogger
from os import path
from subprocess import CalledProcessError
from traceback import format_exc
from typing import Tuple

import csv
import sys

from performance.common import get_repo_root_path
from performance.common import get_artifacts_directory
from performance.common import get_packages_directory
from performance.common import remove_directory
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers

import dotnet


class FrameworkAction(Action):
    '''
    Used by the ArgumentParser to represent the information needed to parse the
    supported .NET frameworks argument from the command line.
    '''

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            wrong_choices = []
            supported_frameworks = FrameworkAction\
                .get_supported_frameworks()

            for value in values:
                if value not in supported_frameworks:
                    wrong_choices.append(value)
            if wrong_choices:
                message = ', '.join(wrong_choices)
                message = 'Invalid choice(s): {}'.format(message)
                raise ArgumentError(self, message)
            setattr(namespace, self.dest, list(set(values)))

    @staticmethod
    def get_supported_frameworks() -> list:
        '''List of supported .NET frameworks.'''
        frameworks = list(
            FrameworkAction.__get_target_framework_moniker_channel_map().keys()
        )
        frameworks.append('corert')
        if sys.platform == 'win32' and 'net461' not in frameworks:
            frameworks.append('net461')
        return frameworks

    @staticmethod
    def __get_target_framework_moniker_channel_map() -> dict:
        return {
            'netcoreapp5.0': 'master',
            'netcoreapp3.0': 'release/3.1.1xx',
            'netcoreapp2.2': '2.2',
            'netcoreapp2.1': '2.1',
            # For Full Framework download the LTS for dotnet cli.
            'net461': 'LTS',
        }

    @staticmethod
    def get_channel(target_framework_moniker: str) -> str:
        '''
        Attemps to retrieve the channel that can be used to download the
        DotNet Cli tools.
        '''
        dct = FrameworkAction.__get_target_framework_moniker_channel_map()
        return dct[target_framework_moniker] \
            if target_framework_moniker in dct \
            else None

    @staticmethod
    def get_branch(target_framework_moniker: str) -> str:
        '''
        Attemps to retrieve the branch name for reporting purposes
        '''
        dct = {
            'netcoreapp5.0': 'master',
            'netcoreapp3.0': 'release/3.1.1xx',
            'netcoreapp2.2': 'release/2.2',
            'netcoreapp2.1': 'release/2.1',
            # For Full Framework download the LTS for dotnet cli.
            'net461': 'LTS',
        }
        return dct[target_framework_moniker] \
            if target_framework_moniker in dct \
            else None

    @staticmethod
    def get_target_framework_moniker(framework: str) -> str:
        '''
        Translates framework name to target framework moniker (TFM)
        To run CoreRT benchmarks we need to run the host BDN process as latest
        .NET Core the host process will build and run CoreRT benchmarks
        '''
        return 'netcoreapp5.0' if framework == 'corert' else framework

    @staticmethod
    def get_target_framework_monikers(frameworks: list) -> list:
        '''
        Translates framework names to target framework monikers (TFM)
        Required to run CoreRT benchmarks where the host process must be .NET
        Core, not CoreRT.
        '''
        monikers = [
            FrameworkAction.get_target_framework_moniker(framework)
            for framework in frameworks
        ]

        # ['netcoreapp5.0', 'corert'] should become ['netcoreapp5.0']
        return list(set(monikers))


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
    def __dotnet_configuration(configuration: str) -> str:
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
        type=__dotnet_configuration,
        help=SUPPRESS,
    )

    parser.add_argument(
        '-f', '--frameworks',
        required=True,
        nargs='+',
        action=FrameworkAction,
        choices=FrameworkAction.get_supported_frameworks(),
        help='''The framework to build/run for. '''
             '''The target framework must also be specified in the project '''
             '''file.''',
    )

    parser.add_argument(
        '--incremental',
        required=False,
        default='yes',
        choices=['yes', 'no'],
        type=str,
        help='''Controls whether previous packages/bin/obj folders should '''
             '''be kept or removed before the dotnet restore/build/run are '''
             '''executed (Default yes).''',
    )

    # BenchmarkDotNet
    parser.add_argument(
        '--enable-hardware-counters',
        dest='enable_pmc',
        required=False,
        default=False,
        action='store_true',
        help='''Enables the following performance metric counters: '''
             '''BranchMispredictions+CacheMisses+InstructionRetired''',
    )

    parser.add_argument(
        '--filter',
        required=False,
        nargs='+',
        help='Glob patterns to execute benchmarks that match.',
    )

    def __valid_file_path(file_path: str) -> str:
        '''Verifies that specified file path exists.'''
        file_path = path.abspath(file_path)
        if not path.isfile(file_path):
            raise ArgumentTypeError('{} does not exist.'.format(file_path))
        return file_path

    parser.add_argument(
        '--corerun',
        dest='corerun',
        required=False,
        nargs='+',
        type=__valid_file_path,
        help='Full path to CoreRun.exe (corerun on Unix)',
    )
    parser.add_argument(
        '--cli',
        dest='cli',
        required=False,
        type=__valid_file_path,
        help='Full path to dotnet.exe',
    )

    def __get_bdn_arguments(user_input: str) -> list:
        file = StringIO(user_input)
        reader = csv.reader(file, delimiter=' ')
        for args in reader:
            return args
        return []

    parser.add_argument(
        '--bdn-arguments',
        dest='bdn_arguments',
        required=False,
        type=__get_bdn_arguments,
        help='''Command line arguments to be passed to the BenchmarkDotNet '''
             '''harness.''',
    )

    parser.add_argument(
        '--bdn-artifacts',
        dest='bdn_artifacts',
        required=False,
        type=str,
        help='''Path to artifacts directory to be passed to the BenchmarkDotNet '''
             '''harness.''',
    )

    def __valid_dir_path(file_path: str) -> str:
        '''Verifies that specified file path exists.'''
        file_path = path.abspath(file_path)
        if not path.isdir(file_path):
            raise ArgumentTypeError('{} does not exist.'.format(file_path))
        return file_path

    def __csproj_file_path(file_path: str) -> dotnet.CSharpProjFile:
        file_path = __valid_file_path(file_path)
        return dotnet.CSharpProjFile(
            file_name=file_path,
            working_directory=path.dirname(file_path)
        )

    microbenchmarks_csproj = path.join(
        get_repo_root_path(), 'src', 'benchmarks', 'micro',
        'MicroBenchmarks.csproj'
    )
    parser.add_argument(
        '--csproj',
        dest='csprojfile',
        required=False,
        type=__csproj_file_path,
        default=dotnet.CSharpProjFile(
            file_name=microbenchmarks_csproj,
            working_directory=path.dirname(microbenchmarks_csproj)
        ),
        help='''C# project file name with the benchmarks to build/run. '''
             '''The default project is the MicroBenchmarks.csproj'''
    )

    def __absolute_path(file_path: str) -> str:
        '''
        Return a normalized absolutized version of the specified file_path
        path.
        '''
        return path.abspath(file_path)

    parser.add_argument(
        '--bin-directory',
        dest='bin_directory',
        required=False,
        default=path.join(get_repo_root_path(), 'artifacts', 'bin'),
        type=__absolute_path,
        help='Root of the bin directory',
    )

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


def __get_benchmarkdotnet_arguments(framework: str, args: tuple) -> list:
    run_args = ['--']
    if args.corerun:
        run_args += ['--coreRun'] + args.corerun
    if args.cli:
        run_args += ['--cli', args.cli]
    if args.enable_pmc:
        run_args += [
            '--counters',
            'BranchMispredictions+CacheMisses+InstructionRetired',
        ]
    if args.filter:
        run_args += ['--filter'] + args.filter

    # Extra BenchmarkDotNet cli arguments.
    if args.bdn_arguments:
        run_args += args.bdn_arguments

    if args.bdn_artifacts:
        run_args += ['--artifacts', args.bdn_artifacts] 

    # we need to tell BenchmarkDotNet where to restore the packages
    # if we don't it's gonna restore to default global folder
    run_args += ['--packages', get_packages_directory()]

    # Required for CoreRT where:
    #   host process framework != benchmark process framework
    run_args += ['--runtimes', framework]

    return run_args


def build(
        BENCHMARKS_CSPROJ: dotnet.CSharpProject,
        configuration: str,
        target_framework_monikers: list,
        incremental: str,
        verbose: bool) -> None:
    '''Restores and builds the benchmarks'''

    packages = get_packages_directory()

    if incremental == 'no':
        __log_script_header("Removing packages, bin and obj folders.")
        binary_folders = [
            packages,
            path.join(BENCHMARKS_CSPROJ.bin_path),
        ]
        for binary_folder in binary_folders:
            remove_directory(path=binary_folder)

    # dotnet restore
    __log_script_header("Restoring .NET micro benchmarks")
    BENCHMARKS_CSPROJ.restore(packages_path=packages, verbose=verbose)

    # dotnet build
    build_title = "Building .NET micro benchmarks for '{}'".format(
        ' '.join(target_framework_monikers))
    __log_script_header(build_title)
    BENCHMARKS_CSPROJ.build(
        configuration=configuration,
        target_framework_monikers=target_framework_monikers,
        verbose=verbose,
        packages_path=packages)


def run(
        BENCHMARKS_CSPROJ: dotnet.CSharpProject,
        configuration: str,
        framework: str,
        verbose: bool,
        *args) -> None:
    '''Runs the benchmarks'''
    __log_script_header("Running .NET micro benchmarks for '{}'".format(
        framework
    ))
    # dotnet run
    run_args = __get_benchmarkdotnet_arguments(framework, *args)
    target_framework_moniker = FrameworkAction.get_target_framework_moniker(
        framework
    )
    BENCHMARKS_CSPROJ.run(
        configuration,
        target_framework_moniker,
        verbose,
        *run_args
    )


def __log_script_header(message: str):
    getLogger().info('-' * len(message))
    getLogger().info(message)
    getLogger().info('-' * len(message))


def __main(args: list) -> int:
    try:
        validate_supported_runtime()
        args = __process_arguments(args)

        configuration = args.configuration
        frameworks = args.frameworks
        incremental = args.incremental
        verbose = args.verbose
        target_framework_monikers = FrameworkAction. \
            get_target_framework_monikers(frameworks)

        setup_loggers(verbose=verbose)

        # dotnet --info
        dotnet.info(verbose)

        BENCHMARKS_CSPROJ = dotnet.CSharpProject(
            project=args.csprojfile,
            bin_directory=args.bin_directory
        )

        # dotnet build
        build(
            BENCHMARKS_CSPROJ,
            configuration,
            target_framework_monikers,
            incremental,
            verbose
        )

        for framework in frameworks:
            # dotnet run
            run(
                BENCHMARKS_CSPROJ,
                configuration,
                framework,
                verbose,
                args
            )

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
