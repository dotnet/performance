#!/usr/bin/env python3

'''
This script provides simple execution of the .NET micro benchmarks
for a .NET preview release, making it easier to contribute to our
monthly manual performance runs.
'''

from performance.logger import setup_loggers
from argparse import ArgumentParser, ArgumentTypeError
from datetime import datetime
from logging import getLogger

import benchmarks_ci
import tarfile
import shutil
import sys
import os

VERSIONS = {
    'net7.0-preview2': { 'tfm': 'net7.0', 'build': '7.0.100-preview.2.22124.4' },
    'net7.0-preview1': { 'tfm': 'net7.0', 'build': '7.0.100-preview.1.22077.12' },
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
        default='x64',
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
        help='Do not clean the SDK and results directories before execution')

    parser.add_argument(
        '--dry-run',
        dest='dry_run',
        action='store_true',
        help='Perform a dry run, showing what would be executed')

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
    sdkPath = os.path.join(rootPath, 'tools', 'dotnet', args.architecture, 'sdk')

    logPrefix = ''

    if args.dry_run:
        logPrefix = '[DRY RUN] '

    for versionName in args.versions:
        version = get_version_from_name(versionName)
        resultsPath = os.path.join(rootPath, 'artifacts', 'bin', 'MicroBenchmarks', 'Release', version['tfm'], 'BenchmarkDotNet.Artifacts', 'results')

        if not args.no_clean:
            # Delete any preexisting SDK and results, which allows
            # multiple versions to be run from a single command
            if os.path.isdir(sdkPath):
                getLogger().log(getLogger().getEffectiveLevel(), logPrefix + 'rmdir -r ' + sdkPath)

                if not args.dry_run:
                    shutil.rmtree(sdkPath)

            if os.path.isdir(resultsPath):
                getLogger().log(getLogger().getEffectiveLevel(), logPrefix + 'rmdir -r ' + resultsPath)

                if not args.dry_run:
                    shutil.rmtree(resultsPath)

        benchmarkArgs = ['--skip-logger-setup', '--filter', args.filter, '--architecture', args.architecture, '-f', version['tfm']]

        if 'build' in version:
            benchmarkArgs += ['--dotnet-versions', version['build']]

        if args.bdn_arguments:
            benchmarkArgs += ['--bdn-arguments', args.bdn_arguments]

        getLogger().log(getLogger().getEffectiveLevel(), logPrefix + 'Executing: benchmarks_ci.py ' + str.join(' ', benchmarkArgs))

        if not args.dry_run:
            benchmarks_ci.__main(benchmarkArgs)

        getLogger().log(getLogger().getEffectiveLevel(), logPrefix + 'Results were created in the following folder:')
        getLogger().log(getLogger().getEffectiveLevel(), logPrefix + '  ' + resultsPath)

        timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M')

        if args.device_name:
            resultsName = timestamp + '-' + args.device_name + '-' + versionName
        else:
            resultsName = timestamp + '-' + versionName

        resultsTarPath = os.path.join(rootPath, 'artifacts', resultsName + '.tar.gz')

        if not args.dry_run:
            resultsTar = tarfile.open(resultsTarPath, 'w:gz')
            resultsTar.add(resultsPath, arcname=resultsName)
            resultsTar.close()

        getLogger().log(getLogger().getEffectiveLevel(), logPrefix + 'Results were collected into the following tar archive:')
        getLogger().log(getLogger().getEffectiveLevel(), logPrefix + '  ' + resultsTarPath)

if __name__ == '__main__':
    __main(sys.argv[1:])
