#!/usr/bin/env python3

'''
This script provides simple execution of the .NET micro benchmarks
for a .NET preview release, making it easier to contribute to our
monthly manual performance runs.
'''

from argparse import ArgumentParser, ArgumentTypeError
from logging import getLogger

import benchmarks_ci
import datetime
import tarfile
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
    '''Adds new arguments to the specified ArgumentParser object.'''

    if not isinstance(parser, ArgumentParser):
        raise TypeError('Invalid parser.')

    parser.add_argument(
        'version',
        choices=VERSIONS,
        help='Specifies the .NET release for the benchmarks run')

    parser.add_argument(
        '--filter',
        dest='filter',
        default='*',
        help='Specifies the benchmark filter to pass to BenchmarkDotNet')

    parser.add_argument(
        '--architecture',
        dest='architecture',
        required=False,
        choices=['x64', 'x86', 'arm64', 'arm'],
        default='x64',
        help='Specifies the processor architecture to pass to BenchmarkDotNet')

    parser.add_argument(
        '--bdn-arguments',
        dest='bdn_arguments',
        required=False,
        help='Command line arguments to be passed to BenchmarkDotNet, wrapped in quotes',
    )

    return parser

def __process_arguments(args: list):
    parser = ArgumentParser(
        description='Tool to execute the monthly manual micro benchmark performance runs',
        allow_abbrev=False
    )

    add_arguments(parser)
    return parser.parse_args(args)

def __main(args: list) -> int:
    args = __process_arguments(args)

    if not args.version:
        raise Exception('Version must be specified.')

    version = get_version_from_name(args.version)
    benchmarkArgs = ['--filter', args.filter, '-f', version['tfm']]

    if 'build' in version:
        benchmarkArgs += ['--dotnet-versions', version['build']]

    if args.architecture:
        benchmarkArgs += ['--architecture', args.architecture]

    if args.bdn_arguments:
        benchmarkArgs += ['--bdn-arguments', args.bdn_arguments]

    getLogger().log(getLogger().getEffectiveLevel(), '\nExecuting: benchmarks_ci.py ' + str.join(' ', benchmarkArgs) + '\n')
    benchmarks_ci.__main(benchmarkArgs)

    rootPath = os.path.normpath(os.path.join(os.path.dirname(__file__), '..'))
    resultsPath = os.path.join(rootPath, 'artifacts', 'bin', 'MicroBenchmarks', 'Release', version['tfm'], 'BenchmarkDotNet.Artifacts', 'results')

    getLogger().log(getLogger().getEffectiveLevel(), 'Results were created in the following folder:')
    getLogger().log(getLogger().getEffectiveLevel(), '  ' + resultsPath)

    today = str(datetime.date.today())
    resultsName = today + '-' + args.version
    resultsTarPath = os.path.join(rootPath, 'artifacts', resultsName + '.tar.gz')
    resultsTar = tarfile.open(resultsTarPath, 'w:gz')
    resultsTar.add(resultsPath, arcname=resultsName)
    resultsTar.close()

    getLogger().log(getLogger().getEffectiveLevel(), 'Results were collected into the following tar archive:')
    getLogger().log(getLogger().getEffectiveLevel(), '  ' + resultsTarPath)

if __name__ == '__main__':
    __main(sys.argv[1:])
