#!/usr/bin/env python3

'''
Builds the Benchmarks
'''

from subprocess import CalledProcessError
from traceback import format_exc
from typing import Tuple

import argparse
import datetime
import logging
import os
import sys

from build.common import get_logging_console_handler
from build.common import get_logging_file_handler
from build.common import get_repo_root_path
from build.common import is_supported_version
from build.common import log_start_message
from build.common import LAUNCH_TIME
from build.common import LOGGING_FORMATTER
from build.exception.FatalError import FatalError
from build.parser.TargetFrameworkAction import TargetFrameworkAction
from build.process.DotNet import DotNet
from build.runner.RunCommand import RunCommand


def generate_log_file_name() -> str:
    '''Generates a unique log file name for the current script'''
    log_dir = os.path.join(get_repo_root_path(), 'logs')
    if not os.path.exists(log_dir):
        os.makedirs(log_dir)

    script_name = os.path.splitext(os.path.basename(sys.argv[0]))[0]
    timestamp = datetime.datetime.fromtimestamp(LAUNCH_TIME).strftime(
        "%Y%m%d%H%M%S")
    log_file_name = '{}-{}-pid{}.log'.format(
        timestamp, script_name, os.getpid())
    return os.path.join(log_dir, log_file_name)


def init_logging(verbose: bool) -> str:
    '''Initializes the loggers used by the script.'''
    logging.getLogger().setLevel(logging.INFO)

    log_file_name = generate_log_file_name()

    for logger in ['shell', 'script']:
        logging.getLogger(logger).addHandler(get_logging_console_handler(
            LOGGING_FORMATTER, verbose))
        logging.getLogger(logger).addHandler(get_logging_file_handler(
            log_file_name, LOGGING_FORMATTER))
        logging.getLogger(logger).setLevel(logging.INFO)

    return log_file_name


def check_requirements(log_file: str, verbose: bool) -> None:
    '''
    Checks that the requirements needs to build the benchmarks are met.
    '''
    logging.getLogger('script').info("Making sure dotnet exists...")
    try:
        cmdline = ['dotnet', '--info']
        RunCommand(log_file, cmdline, verbose=verbose).run('dotnet-info')
    except Exception:
        raise FatalError("Cannot find dotnet.")


def process_arguments() -> Tuple[str, list, bool]:
    '''
    Function used to parse the command line arguments passed to this script
    through the cli.
    '''
    parser = argparse.ArgumentParser(
        description="Builds the CoreClr benchmarks.",
    )
    parser.add_argument(
        '-c', '--configuration',
        metavar='CONFIGURATION',
        required=False,
        default='release',
        choices=['debug', 'release'],
        type=str.casefold,
        help='Configuration use for building the project (default "release").',
    )
    parser.add_argument(
        '-f', '--frameworks',
        metavar='FRAMEWORK',
        required=False,
        nargs='*',
        action=TargetFrameworkAction,
        default=TargetFrameworkAction.supported_target_frameworks(),
        help='Target frameworks to publish for (default all).',
    )
    parser.add_argument(
        '-v', '--verbose',
        required=False,
        default=False,
        action='store_true',
        help='Turns on verbosity (default "False")',
    )

    # --verbosity <LEVEL>
    # ['quiet', 'minimal', 'normal', 'detailed', 'diagnostic']

    args = parser.parse_args()
    return (
        args.configuration,
        args.frameworks,
        args.verbose
    )


def build_benchmarks(
        log_file: str,
        configuration: str,
        frameworks: list,
        verbose: bool) -> None:
    '''Builds the benchmarks'''
    workspace = get_repo_root_path()
    working_directory = os.path.join(workspace, 'src', 'benchmarks')
    csproj_file = 'Benchmarks.csproj'

    dotnet = DotNet(log_file, working_directory, csproj_file, verbose)
    dotnet.restore()
    for framework in frameworks:
        dotnet.publish(configuration, framework, 'Benchmarks')


def main() -> int:
    '''Script main entry point.'''
    try:
        if not is_supported_version():
            raise FatalError("Unsupported python version.")

        args = process_arguments()
        configuration, frameworks, verbose = args
        log_file = init_logging(verbose)

        log_start_message('script')
        check_requirements(log_file, verbose)
        build_benchmarks(log_file, configuration, frameworks, verbose)

        return 0
    except FatalError as ex:
        logging.getLogger('script').error(str(ex))
    except CalledProcessError as ex:
        logging.getLogger('script').error(
            'Command: "%s", exited with status: %s', ex.cmd, ex.returncode)
    except IOError as ex:
        logging.getLogger('script').error(
            "I/O error (%s): %s", ex.errno, ex.strerror)
    except SystemExit:  # Argparse throws this exception when it exits.
        pass
    except Exception:
        logging.getLogger('script')(
            'Unexpected error: {}'.format(sys.exc_info()[0]))
        logging.getLogger('script')(format_exc())
        raise
    return 1


if __name__ == "__main__":
    sys.exit(main())
