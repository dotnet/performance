#!/usr/bin/env python3

'''
Builds the Benchmarks
'''

from argparse import ArgumentParser
from argparse import ArgumentTypeError
from argparse import SUPPRESS
from io import StringIO
from logging import getLogger
from opentelemetry import trace
from os import path
from subprocess import CalledProcessError
from traceback import format_exc
from typing import Any, List

import csv
import sys

from performance.common import get_repo_root_path
from performance.common import get_artifacts_directory
from performance.common import get_packages_directory
from performance.common import remove_directory
from performance.common import validate_supported_runtime
from performance.logger import setup_loggers, setup_trace_provider
from channel_map import ChannelMap

import dotnet

setup_trace_provider()
tracer = trace.get_tracer("dotnet.performance")

@tracer.start_as_current_span(name="get_supported_configurations")
def get_supported_configurations() -> List[str]:
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
        required=False,
        choices=ChannelMap.get_supported_frameworks(),
        nargs='+',
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

    def __get_bdn_arguments(user_input: str) -> List[str]:
        file = StringIO(user_input)
        reader = csv.reader(file, delimiter=' ')
        for args in reader:
            return args
        return []

    parser.add_argument(
        '--wasm',
        dest='wasm',
        required=False,
        default=False,
        action='store_true',
        help='Tests should be run with the wasm runtime'
    )

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

    parser.add_argument(
        '--run-isolated',
        dest='run_isolated',
        required=False,
        default=False,
        action='store_true',
        help='Move the binaries to a different directory for running',
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


def __process_arguments(args: List[str]):
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


def __get_benchmarkdotnet_arguments(framework: str, args: Any) -> List[str]:
    run_args: List[str] = []
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
    if args.resume:
        run_args += ['--resume']

    # Extra BenchmarkDotNet cli arguments.
    if args.bdn_arguments:
        run_args += args.bdn_arguments

    if args.bdn_artifacts:
        run_args += ['--artifacts', args.bdn_artifacts] 

    # we need to tell BenchmarkDotNet where to restore the packages
    # if we don't it's gonna restore to default global folder
    run_args += ['--packages', get_packages_directory()]

    # Required for WASM and NativeAOT where:
    #   host process framework != benchmark process framework
    if framework.startswith("nativeaot"):
        run_args += ['--runtimes', framework]
    if args.wasm:
        if framework == "net6.0":
            run_args += ['--runtimes', 'wasm']
        elif framework == "net7.0":
            run_args += ['--runtimes', 'wasmnet70']
        elif framework == "net8.0":
            run_args += ['--runtimes', 'wasmnet80']
        elif framework == "net9.0":
            run_args += ['--runtimes', 'wasmnet90']
        else:
            raise ArgumentTypeError('Framework {} is not supported for wasm'.format(framework))

    # Increase default 2 min build timeout to accommodate slow (or even very slow) hardware
    if not args.bdn_arguments or '--buildTimeout' not in args.bdn_arguments:
        run_args += ['--buildTimeout', '1200']

    return run_args

@tracer.start_as_current_span(name="get_bin_dir_to_use")
def get_bin_dir_to_use(csprojfile: dotnet.CSharpProjFile, bin_directory: str, run_isolated: bool) -> str:
    '''
    Gets the bin_directory, which might be different if run_isolate=True
    '''
    if run_isolated:
        return path.join(bin_directory, 'for-running', dotnet.get_project_name(csprojfile.file_name))
    else:
        return bin_directory

@tracer.start_as_current_span(name="build")
def build(
        BENCHMARKS_CSPROJ: dotnet.CSharpProject,
        configuration: str,
        target_framework_monikers: List[str],
        incremental: str,
        run_isolated: bool,
        for_wasm: bool,
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

    build_args: List[str] = []
    if for_wasm:
        build_args += ['/p:BuildingForWasm=true']

    # dotnet build
    build_title = "Building .NET micro benchmarks for '{}'".format(
        ' '.join(target_framework_monikers))
    __log_script_header(build_title)
    BENCHMARKS_CSPROJ.build(
        configuration=configuration,
        target_framework_monikers=target_framework_monikers,
        output_to_bindir=run_isolated,
        verbose=verbose,
        packages_path=packages,
        args=build_args)

    # When running isolated, artifacts/obj/{project_name} will still be
    # there, and would interfere with any subsequent builds. So, remove
    # that
    if run_isolated:
        objDir = path.join(get_artifacts_directory(), 'obj', BENCHMARKS_CSPROJ.project_name)
        remove_directory(objDir)

@tracer.start_as_current_span(name="run")
def run(
        BENCHMARKS_CSPROJ: dotnet.CSharpProject,
        configuration: str,
        framework: str,
        run_isolated: bool,
        verbose: bool,
        args: Any) -> bool:
    '''Runs the benchmarks, returns True for a zero status code and False otherwise.'''
    __log_script_header("Running .NET micro benchmarks for '{}'".format(
        framework
    ))

    # dotnet exec
    run_args = __get_benchmarkdotnet_arguments(framework, args)
    target_framework_moniker = dotnet.FrameworkAction.get_target_framework_moniker(
        framework
    )

    # 1 is treated as successful in that there were still some benchmarks that ran
    # but some of the runs may have failed.
    success_exit_codes=[0, 1]
    if run_isolated:
        runDir = BENCHMARKS_CSPROJ.bin_path
        asm_path=dotnet.get_main_assembly_path(runDir, BENCHMARKS_CSPROJ.project_name)
        status = dotnet.exec(asm_path, success_exit_codes, verbose, *run_args)
    else:
        # This is needed for `dotnet run`, but not for `dotnet exec`
        run_args = ['--'] + run_args
        status = BENCHMARKS_CSPROJ.run(
            configuration,
            target_framework_moniker,
            success_exit_codes,
            verbose,
            *run_args
        )
    
    return status == 0

def __log_script_header(message: str):
    getLogger().info('-' * len(message))
    getLogger().info(message)
    getLogger().info('-' * len(message))

@tracer.start_as_current_span("microbenchmarks.__main")
def __main(argv: List[str]) -> int:
    try:
        validate_supported_runtime()
        args = __process_arguments(argv)

        configuration = args.configuration
        frameworks = args.frameworks
        incremental = args.incremental
        verbose = args.verbose
        target_framework_monikers = dotnet.FrameworkAction. \
            get_target_framework_monikers(frameworks)

        setup_loggers(verbose=verbose)

        # dotnet --info
        dotnet.info(verbose)

        bin_dir_to_use=get_bin_dir_to_use(args.csprojfile, args.bin_directory, args.run_isolated)
        BENCHMARKS_CSPROJ = dotnet.CSharpProject(
            project=args.csprojfile,
            bin_directory=bin_dir_to_use
        )

        # dotnet build
        build(
            BENCHMARKS_CSPROJ,
            configuration,
            target_framework_monikers,
            incremental,
            args.run_isolated,
            for_wasm=args.wasm,
            verbose=verbose
        )

        for framework in frameworks:
            # dotnet run
            run(
                BENCHMARKS_CSPROJ,
                configuration,
                framework,
                args.run_isolated,
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
