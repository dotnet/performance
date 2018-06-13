'''
Contains the definition of RunCommand runner object.
'''

import logging
import os
import subprocess

from ..common import push_dir
from ..common import get_logging_console_handler
from ..common import get_logging_file_handler
from ..common import log_start_message
from ..common import LOGGING_FORMATTER


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
    def log_file(self) -> str:
        '''Log file name to write to.'''
        return self.__log_file

    @property
    def cmdline(self) -> str:
        '''Command-line to use when starting the application.'''
        return self.__cmdline

    @property
    def success_exit_codes(self) -> list:
        '''
        The successful exit codes that the associated process specifies when it
        terminated.
        '''
        return self.__success_exit_codes

    @property
    def verbose(self) -> bool:
        '''Enables/Disables verbosity.'''
        return self.__verbose

    def run(self, suffix: str = None, working_directory: str = None) -> None:
        '''
        Executes specified shell command.
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
                with subprocess.Popen(
                    self.cmdline,
                    stdout=subprocess.PIPE if should_pipe else devnull,
                    stderr=subprocess.STDOUT,
                    universal_newlines=True,
                ) as proc:

                    if proc.stdout is not None:
                        with proc.stdout:
                            for line in iter(proc.stdout.readline, ''):
                                line = line.rstrip()
                                exe_logger.info(line)

                    proc.wait()
                    # FIXME: dotnet child processes are still running.

                    if proc.returncode not in self.success_exit_codes:
                        exe_logger.error(
                            "Process exited with status %s", proc.returncode)
                        raise subprocess.CalledProcessError(
                            proc.returncode, quoted_cmdline)
