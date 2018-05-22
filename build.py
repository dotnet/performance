#!/usr/bin/env python3

'''
Builds the CoreClr Benchmarks
'''

from contextlib import contextmanager
from traceback import format_exc

import argparse
import datetime
import logging
import os
import subprocess
import sys
import time


LAUNCH_TIME = time.time()
LOGGING_FORMATTER = logging.Formatter(
    fmt='[%(asctime)s][%(levelname)s] %(message)s',
    datefmt="%Y-%m-%d %H:%M:%S")


class FatalError(Exception):
    '''
    Raised for various script errors regarding environment and build
    requirements.
    '''


def is_supported_version() -> bool:
    '''Checks if the script is running on the supported version (>=3.5).'''
    return sys.version_info.major > 2 and sys.version_info.minor > 4


def get_script_path() -> str:
    '''Gets this script directory.'''
    return sys.path[0]
    # return os.path.dirname(os.path.realpath(__file__))


def get_repo_root_path() -> str:
    '''Gets repository root directory.'''
    return get_script_path()


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


def get_logging_console_handler(
        fmt: logging.Formatter,
        verbose: bool) -> logging.StreamHandler:
    '''
    Gets a logging console handler (logging.StreamHandler) based on the
    specified formatter (logging.Formatter) and verbosity.
    '''
    console_handler = logging.StreamHandler()
    console_handler.setLevel(logging.INFO if verbose else logging.WARNING)
    console_handler.setFormatter(fmt)
    return console_handler


def get_logging_file_handler(
        file: str,
        fmt: logging.Formatter,
        set_formatter: bool = True) -> logging.FileHandler:
    '''
    Gets a logging file handler (logging.FileHandler) based on the specified
    formatter (logging.Formatter).
    '''
    file_handler = logging.FileHandler(file)
    file_handler.setLevel(logging.INFO)
    if set_formatter:
        file_handler.setFormatter(fmt)
    return file_handler


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


def log_start_message(name):
    '''Used to log a start event message header.'''
    start_msg = "Script started at {}".format(
        str(datetime.datetime.fromtimestamp(LAUNCH_TIME)))
    logging.getLogger(name).info('-' * len(start_msg))
    logging.getLogger(name).info(start_msg)
    logging.getLogger(name).info('-' * len(start_msg))


@contextmanager
def push_dir(path: str = None):
    '''
    Adds the specified location to the top of a location stack, then changes to
    the specified directory.
    '''
    if path:
        prev = os.getcwd()
        try:
            logging.getLogger('shell').info('pushd "%s"', path)
            os.chdir(path)
            yield
        finally:
            logging.getLogger('shell').info('popd')
            os.chdir(prev)
    else:
        yield


class RunCommand(object):
    '''
    This is a class wrapper around `subprocess.Popen` with an additional set
    of logging features.
    '''

    def __init__(
            self,
            log_file,
            cmdline: list,
            success_exit_codes: list = None,
            verbose: bool = False):
        if not log_file:
            raise TypeError('Unspecified log file.')
        if cmdline is None:
            raise TypeError('Unspecified command line to be executed.')
        if not cmdline:
            raise ValueError('Specified command line is empty.')

        self.__log_file = log_file
        self.__cmdline = cmdline
        self.__verbose = verbose

        if success_exit_codes is None:
            self.__success_exit_codes = [0]
        else:
            self.__success_exit_codes = success_exit_codes

    @property
    def log_file(self):
        '''Log file name to write to.'''
        return self.__log_file

    @property
    def cmdline(self):
        '''Command-line to use when starting the application.'''
        return self.__cmdline

    @property
    def success_exit_codes(self):
        '''
        The successful exit codes that the associated process specifies when it
        terminated.
        '''
        return self.__success_exit_codes

    @property
    def verbose(self):
        '''Enables/Disables verbosity.'''
        return self.__verbose

    def run(self, suffix: str = None, working_directory: str = None):
        '''
        This is a function wrapper around `subprocess.Popen` with an additional
        set of logging features.
        '''
        should_pipe = self.verbose
        with push_dir(working_directory):
            quoted_cmdline = subprocess.list2cmdline(self.cmdline)
            quoted_cmdline += ' > {}'.format(
                os.devnull) if not should_pipe else ''

            logging.getLogger('shell').info(quoted_cmdline)
            exe_name = os.path.basename(self.cmdline[0]).replace('.', '_')

            exe_log_file = self.log_file
            if suffix is not None:
                exe_log_file = exe_log_file.replace(
                    '.log', '.{}.log'.format(suffix))

            exe_logger = logging.getLogger(exe_name)
            exe_logger.handlers = []

            file_handler = get_logging_file_handler(
                exe_log_file,
                LOGGING_FORMATTER,
                set_formatter=(suffix is None))
            exe_logger.addHandler(file_handler)

            if suffix is not None:
                log_start_message(exe_name)

            console_handler = get_logging_console_handler(
                LOGGING_FORMATTER, self.verbose)
            exe_logger.addHandler(console_handler)

            with open(os.devnull) as devnull:
                proc = subprocess.Popen(
                    self.cmdline,
                    stdout=subprocess.PIPE if should_pipe else devnull,
                    stderr=subprocess.STDOUT,
                    universal_newlines=True,
                )

                if proc.stdout is not None:
                    for line in iter(proc.stdout.readline, ''):
                        line = line.rstrip()
                        exe_logger.info(line)
                    proc.stdout.close()

                proc.wait()
                if proc.returncode not in self.success_exit_codes:
                    exe_logger.error(
                        "Process exited with status %s", proc.returncode)
                    raise subprocess.CalledProcessError(
                        proc.returncode, quoted_cmdline)


def check_requirements(log_file: str, verbose: bool):
    '''
    Checks that the requirements needs to build the CoreClr benchmarks are met.
    '''
    logging.getLogger('script').info("Making sure dotnet exists...")
    try:
        cmdline = ['dotnet', '--info']
        RunCommand(log_file, cmdline, verbose=verbose).run('dotnet-info')
    except Exception:
        raise FatalError("Cannot find dotnet.")


class TargetFrameworkAction(argparse.Action):
    '''
    Used by the ArgumentParser to represent the information needed to parse the
    supported .NET Core target frameworks argument from the command line.
    '''

    def __call__(self, parser, namespace, values, option_string=None):
        if values:
            wrong_choices = []
            for value in values:
                if value not in self.supported_target_frameworks():
                    wrong_choices.append(value)
            if wrong_choices:
                message = ', '.join(wrong_choices)
                message = 'Invalid choice(s): {}'.format(message)
                raise argparse.ArgumentError(self, message)
            setattr(namespace, self.dest, values)

    @staticmethod
    def supported_target_frameworks() -> list:
        '''List of supported .NET Core target frameworks.'''
        return ['netcoreapp1.1', 'netcoreapp2.0', 'netcoreapp2.1', 'net461']


def process_arguments():
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


class DotNet(object):
    '''
    This is a class wrapper around the `dotnet` command line interface.
    '''

    def __init__(
            self,
            log_file: str,
            working_directory: str,
            csproj_file: str,
            verbose: bool):
        if not log_file:
            raise TypeError('Unspecified log file.')
        if not working_directory:
            raise TypeError('Unspecified working directory.')
        if not os.path.isdir(working_directory):
            raise ValueError(
                'Specified working directory: {}, does not exist.'.format(
                    working_directory))

        if os.path.isabs(csproj_file) and not os.path.exists(csproj_file):
            raise ValueError(
                'Specified project file: {}, does not exist.'.format(
                    csproj_file))
        elif not os.path.exists(os.path.join(working_directory, csproj_file)):
            raise ValueError(
                'Specified project file: {}, does not exist.'.format(
                    csproj_file))

        self.__log_file = log_file
        self.__working_directory = working_directory
        self.__csproj_file = csproj_file
        self.__verbose = verbose

    @property
    def log_file(self):
        '''Gets the log file name to write to.'''
        return self.__log_file

    @property
    def working_directory(self):
        '''Gets the working directory for the dotnet process to be started.'''
        return self.__working_directory

    @property
    def csproj_file(self):
        '''Gets the project file to run the dotnet cli against.'''
        return self.__csproj_file

    @property
    def verbose(self):
        '''Gets a flag to whether verbosity if turned on or off.'''
        return self.__verbose

    @property
    def packages_path(self):
        '''Gets the folder to restore packages to.'''
        return os.path.join(get_repo_root_path(), 'packages')

    @property
    def bin_path(self):
        '''Gets the directory in which the built binaries will be placed.'''
        return os.path.join(get_repo_root_path(), 'bin{}'.format(os.path.sep))

    def restore(self):
        '''
        Calls dotnet to restore the dependencies and tools of the specified
        project.
        '''
        cmdline = ['dotnet', 'restore',
                   '--packages', self.packages_path,
                   self.csproj_file]
        RunCommand(self.log_file, cmdline, verbose=self.verbose).run(
            'dotnet-restore', self.working_directory)

    def publish(self, configuration: str, framework: str,):
        '''
        Calls dotnet to pack the specified application and its dependencies
        into the repo bin folder for deployment to a hosting system.
        '''
        cmdline = ['dotnet', 'publish',
                   '--no-restore',
                   '--configuration', configuration,
                   '--framework', framework,
                   self.csproj_file,
                   '/p:BaseOutputPath={}'.format(self.bin_path)]
        RunCommand(self.log_file, cmdline, verbose=self.verbose).run(
            'dotnet-publish', self.working_directory)


def build_coreclr(
        log_file: str,
        configuration: str,
        frameworks: list,
        verbose: bool):
    '''Builds the CoreClr set of benchmarks (Code Quality).'''
    working_directory = os.path.join(
        get_repo_root_path(), 'src', 'coreclr', 'PerformanceHarness')
    csproj_file = 'PerformanceHarness.csproj'

    dotnet = DotNet(log_file, working_directory, csproj_file, verbose)
    dotnet.restore()
    for framework in frameworks:
        dotnet.publish(configuration, framework)


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
        build_coreclr(log_file, configuration, frameworks, verbose)

        return 0
    except FatalError as ex:
        logging.getLogger('script').error(str(ex))
    except subprocess.CalledProcessError as ex:
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
