#!/usr/bin/env python3

'''
This script provides simple execution of the .NET micro benchmarks
for a .NET preview release, making it easier to contribute to our
monthly manual performance runs.
'''

from performance.common import get_machine_architecture
from performance.logger import setup_loggers
from argparse import ArgumentParser, ArgumentTypeError
from datetime import datetime
from logging import getLogger
from subprocess import CalledProcessError

import benchmarks_ci
import tarfile
import shutil
import sys
import os

VERSIONS = {
    'net7.0-rc1': { 'tfm': 'net7.0', 'build': '7.0.100-rc.1.22425.9' },
    'net7.0-preview7': { 'tfm': 'net7.0', 'build': '7.0.100-preview.7.22370.3' },
    'net7.0-preview5': { 'tfm': 'net7.0', 'build': '7.0.100-preview.5.22276.3' },
    'nativeaot7.0-preview4': { 'tfm': 'nativeaot7.0', 'build': '7.0.100-preview.4.22227.3', 'ilc': '7.0.0-preview.4.22222.4' },
    'net7.0-preview4': { 'tfm': 'net7.0', 'build': '7.0.100-preview.4.22227.3' },
    'nativeaot7.0-preview3': { 'tfm': 'nativeaot7.0', 'build': '7.0.100-preview.3.22179.4', 'ilc': '7.0.0-preview.3.22175.4' },
    'net7.0-preview3': { 'tfm': 'net7.0', 'build': '7.0.100-preview.3.22179.4' },
    'net7.0-preview2': { 'tfm': 'net7.0', 'build': '7.0.100-preview.2.22124.4' },
    'net7.0-preview1': { 'tfm': 'net7.0', 'build': '7.0.100-preview.1.22077.12' },
    'nativeaot6.0': { 'tfm': 'nativeaot6.0', 'ilc': '6.0.0-rc.1.21420.1' },
    'net6.0': { 'tfm': 'net6.0' }
}

def get_version_from_name(name: str) -> str:
    for version in VERSIONS:
        if version == name:
            return VERSIONS[version]

    raise Exception('The version specified is not supported', name)

def add_arguments(parser: ArgumentParser) -> ArgumentParser:
    # Adds new arguments to the specified ArgumentParser object.

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    parser.add_argument(
        'versions',
        nargs='+',
        choices=VERSIONS,
        help='Specifies the .NET version(s) for the benchmarks run')

    parser.add_argument(
        '--device-name',
        dest='device_name',
        help='The name of this device, to be included in the .tar.gz file name')

    parser.add_argument(
        '--filter',
        dest='filter',
        default='*',
        help='Specifies the benchmark filter to pass to BenchmarkDotNet')

    parser.add_argument(
        '--architecture',
        dest='architecture',
        choices=['x64', 'x86', 'arm64', 'arm'],
        default=get_machine_architecture(),
        help='Specifies the SDK processor architecture')

    parser.add_argument(
        '--bdn-arguments',
        dest='bdn_arguments',
        help='Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes',
    )

    parser.add_argument(
        '--no-clean',
        dest='no_clean',
        action='store_true',
        help='Do not clean the SDK installations before execution')

    parser.add_argument(
        '--resume',
        dest='resume',
        action='store_true',
        help='Resume a previous run from existing benchmark results')

    parser.add_argument(
        '--dry-run',
        dest='dry_run',
        action='store_true',
        help='Perform a dry run, showing what would be executed')

    parser.add_argument(
        '--run-once',
        dest='run_once',
        action='store_true',
        help='Runs each benchmark only once, useful for testing')

    return parser

def __process_arguments(args: list):
    parser = ArgumentParser(
        description='Tool to execute the monthly manual micro benchmark performance runs',
        allow_abbrev=False
    )

    add_arguments(parser)
    return parser.parse_args(args)

def __main(args: list) -> int:
    setup_loggers(verbose=True)

    args = __process_arguments(args)
    rootPath = os.path.normpath(os.path.join(os.path.dirname(__file__), '..'))
    sdkPath = os.path.join(rootPath, 'tools', 'dotnet')

    logPrefix = ''
    logger = getLogger()
    logLevel = logger.getEffectiveLevel()

    def log(text: str):
        logger.log(logLevel, logPrefix + text)

    if args.dry_run:
        logPrefix = '[DRY RUN] '

    if args.run_once:
        if args.bdn_arguments:
            args.bdn_arguments += '--iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart'
        else:
            args.bdn_arguments = '--iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart'

    versionTarFiles = []

    for versionName in args.versions:
        version = get_version_from_name(versionName)
        moniker = version['tfm'].replace('nativeaot', 'net') # results of nativeaotX.0 are stored in netX.0 folder
        resultsPath = os.path.join(rootPath, 'artifacts', 'bin', 'MicroBenchmarks', 'Release', moniker, 'BenchmarkDotNet.Artifacts', 'results')

        if not args.no_clean:
            # Delete any preexisting SDK installations, which allows
            # multiple versions to be run from a single command
            if os.path.isdir(sdkPath):
                log('rmdir -r ' + sdkPath)

                if not args.dry_run:
                    shutil.rmtree(sdkPath)

        benchmarkArgs = ['--skip-logger-setup', '--filter', args.filter, '--architecture', args.architecture, '-f', version['tfm']]

        if 'build' in version:
            benchmarkArgs += ['--dotnet-versions', version['build']]

        if args.resume:
            benchmarkArgs += ['--resume']
        else:
            if os.path.isdir(resultsPath):
                log('rmdir -r ' + resultsPath)

                if not args.dry_run:
                    shutil.rmtree(resultsPath)

        if args.bdn_arguments:
            if version['tfm'].startswith('nativeaot'):
                benchmarkArgs += ['--bdn-arguments', args.bdn_arguments + ' --ilCompilerVersion ' + version['ilc']]
            else:
                benchmarkArgs += ['--bdn-arguments', args.bdn_arguments]
        elif version['tfm'].startswith('nativeaot'):
            benchmarkArgs += ['--bdn-arguments', '--ilCompilerVersion ' + version['ilc']]

        log('Executing: benchmarks_ci.py ' + str.join(' ', benchmarkArgs))

        if not args.dry_run:
            try:
                benchmarks_ci.__main(benchmarkArgs)
            except CalledProcessError:
                log('benchmarks_ci exited with non zero exit code, please check the log and report benchmark failure')
                # don't rethrow if some results were produced, as we want to create the tar file with results anyway
                if not os.path.isdir(resultsPath):
                    raise

        log('Results were created in the following folder:')
        log('  ' + resultsPath)

        timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M')

        if args.device_name:
            resultsName = timestamp + '-' + args.device_name + '-' + versionName
        else:
            resultsName = timestamp + '-' + versionName

        resultsName = args.architecture + '-' + resultsName
        resultsTarPath = os.path.join(rootPath, 'artifacts', resultsName + '.tar.gz')
        versionTarFiles += [resultsTarPath]

        if not args.dry_run:
            resultsTar = tarfile.open(resultsTarPath, 'w:gz')
            resultsTar.add(resultsPath, arcname=resultsName)
            resultsTar.close()

    log('Results were collected into the following tar archive(s):')

    for versionTarFile in versionTarFiles:
        log('  ' + versionTarFile)

if __name__ == '__main__':
    __main(sys.argv[1:])
