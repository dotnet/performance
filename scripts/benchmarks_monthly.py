#!/usr/bin/env python3

'''
This script provides simple execution of the .NET micro benchmarks
for a .NET preview release, making it easier to contribute to our
monthly manual performance runs.
'''

from argparse import ArgumentParser, ArgumentTypeError
from logging import getLogger

import benchmarks_ci
import sys

VERSIONS = {
    '7.0-preview2': { 'tfm': 'net7.0', 'build': '7.0.100-preview.2.22124.4' },
    '7.0-preview1': { 'tfm': 'net7.0', 'build': '7.0.100-preview.1.22077.12' },
    '6.0': { 'tfm': 'net6.0' }
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
    benchmarkArgs = ['--filter', '*', '-f', version['tfm']]

    if 'build' in version:
        benchmarkArgs += ['--dotnet-versions', version['build']]

    getLogger().critical('\nExecuting: benchmarks_ci.py ' + str.join(' ', benchmarkArgs) + '\n')
    benchmarks_ci.__main(benchmarkArgs)

if __name__ == '__main__':
    __main(sys.argv[1:])
