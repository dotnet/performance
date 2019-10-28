'''
Common functionality used by the repository scripts.
'''

from contextlib import contextmanager
from logging import getLogger
from shutil import rmtree
from stat import S_IWRITE
from subprocess import CalledProcessError
from subprocess import list2cmdline
from subprocess import PIPE
from subprocess import Popen
from subprocess import STDOUT

import os
import sys


def __is_supported_version() -> bool:
    '''Checks if the script is running on the supported version (>=3.5).'''
    return sys.version_info >= (3, 5)


def validate_supported_runtime():
    '''Raises a RuntimeError exception when the runtime is not supported.'''
    if not __is_supported_version():
        raise RuntimeError('Python 3.5 or newer is required.')


def get_python_executable() -> str:
    '''
    Gets the absolute path of the executable binary for the Python interpreter.
    '''
    if not sys.executable:
        raise RuntimeError('Unable to get the path to the Python executable.')
    return sys.executable


def make_directory(path: str):
    '''Creates a directory.'''
    if not path:
        raise TypeError('Undefined path.')
    if not os.path.isdir(path):
        os.makedirs(path)


def remove_directory(path: str) -> None:
    '''Recursively deletes a directory tree.'''
    if not path:
        raise TypeError('Undefined path.')
    if not isinstance(path, str):
        raise TypeError('Invalid type.')

    if os.path.isdir(path):
        def handle_rmtree_errors(func, path, excinfo):
            """
            Helper function to handle long path errors on Windows.
            """
            long_path = path
            if os.sep == '\\' and not long_path.startswith('\\\\?\\'):
                long_path = '\\\\?\\' + long_path
                long_path = long_path.encode().decode('utf-8')
            os.chmod(long_path, S_IWRITE)
            func(long_path)

        rmtree(path, onerror=handle_rmtree_errors)


def get_script_path() -> str:
    '''Gets this script directory.'''
    return os.path.dirname(os.path.realpath(__file__))


def get_repo_root_path() -> str:
    '''Gets repository root directory.'''
    return os.path.abspath(os.path.join(get_script_path(), '..', '..'))


def get_tools_directory() -> str:
    '''Gets the default root directory where tools should be installed.'''
    return os.path.join(get_repo_root_path(), 'tools')


def get_artifacts_directory() -> str:
    '''
    Gets the default artifacts directory where arcade builds the benchmarks.
    '''
    return os.path.join(get_repo_root_path(), 'artifacts')

def get_packages_directory() -> str:
    '''
    The path to directory where packages should get restored
    '''
    return os.path.join(get_artifacts_directory(), 'packages')

@contextmanager
def push_dir(path: str = None) -> None:
    '''
    Adds the specified location to the top of a location stack, then changes to
    the specified directory.
    '''
    if path:
        prev = os.getcwd()
        try:
            abspath = path if os.path.isabs(path) else os.path.abspath(path)
            getLogger().info('$ pushd "%s"', abspath)
            os.chdir(abspath)
            yield
        finally:
            getLogger().info('$ popd')
            os.chdir(prev)
    else:
        yield


class RunCommand:
    '''
    This is a class wrapper around `subprocess.Popen` with an additional set
    of logging features.
    '''

    def __init__(
            self,
            cmdline: list,
            success_exit_codes: list = None,
            verbose: bool = False):
        if cmdline is None:
            raise TypeError('Unspecified command line to be executed.')
        if not cmdline:
            raise ValueError('Specified command line is empty.')

        self.__cmdline = cmdline
        self.__verbose = verbose

        if success_exit_codes is None:
            self.__success_exit_codes = [0]
        else:
            self.__success_exit_codes = success_exit_codes

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

    def run(self, working_directory: str = None) -> None:
        '''Executes specified shell command.'''
        should_pipe = self.verbose
        with push_dir(working_directory):
            quoted_cmdline = '$ '
            quoted_cmdline += list2cmdline(self.cmdline)
            quoted_cmdline += ' > {}'.format(
                os.devnull) if not should_pipe else ''

            getLogger().info(quoted_cmdline)

            with open(os.devnull) as null_device:
                with Popen(
                        self.cmdline,
                        stdout=PIPE if should_pipe else null_device,
                        stderr=STDOUT,
                        universal_newlines=True,
                ) as proc:
                    if proc.stdout is not None:
                        with proc.stdout:
                            for line in iter(proc.stdout.readline, ''):
                                line = line.rstrip()
                                getLogger().info(line)

                    proc.wait()

                    if proc.returncode not in self.success_exit_codes:
                        getLogger().error(
                            "Process exited with status %s", proc.returncode)
                        raise CalledProcessError(
                            proc.returncode, quoted_cmdline)
