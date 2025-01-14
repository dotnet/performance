'''
Common functionality used by the repository scripts.
'''

from contextlib import contextmanager
from logging import getLogger
from os import environ
from shutil import rmtree
from stat import S_IWRITE
from subprocess import CalledProcessError
from subprocess import list2cmdline
from subprocess import PIPE, DEVNULL
from subprocess import Popen
from subprocess import STDOUT
from io import StringIO
from platform import machine

import os
import sys
import time
import base64
from typing import Callable, List, Optional, Tuple, Type, TypeVar


def get_machine_architecture():
    machineArch = machine().lower()
    # values taken from https://stackoverflow.com/a/45125525/5852046
    if machineArch == 'amd64' or machineArch == 'x86_64' or machineArch == 'x64':
        return 'x64'
    elif machineArch == 'arm64' or machineArch == 'aarch64' or machineArch == 'aarch64_be' or machineArch == 'armv8b' or machineArch == 'armv8l':
        return 'arm64'
    elif machineArch == 'arm32' or machineArch == 'aarch32' or machineArch == 'arm':
        return 'arm'
    elif machineArch == 'i386' or machineArch == 'i486' or machineArch == 'i686':
        return 'x86'
    else:
        return 'x64' # Default architecture

def iswin():
    return sys.platform == 'win32'

def extension():
    'gets platform specific extension'
    return '.exe' if iswin() else ''

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
    os.makedirs(path, exist_ok=True)


def remove_directory(path: str) -> None:
    '''Recursively deletes a directory tree.'''
    if not path:
        raise TypeError('Undefined path.')
    if not isinstance(path, str):
        raise TypeError('Invalid type.')

    if os.path.isdir(path):
        def handle_rmtree_errors(func: Callable[[str], None], path: str, excinfo: Exception):
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


def helixpayload():
    '''
    Returns the helix payload. Will be None outside of helix.
    '''
    return environ.get('HELIX_CORRELATION_PAYLOAD')

def helixuploadroot():
    '''
    Returns the helix upload root. Will be None outside of helix.
    '''
    return environ.get('HELIX_WORKITEM_UPLOAD_ROOT')

def helixworkitemroot():
    '''
    Returns the helix workitem root. Will be None outside of helix.
    '''
    return environ.get('HELIX_WORKITEM_ROOT')

def runninginlab():
    return environ.get('PERFLAB_INLAB') == '1'

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

def base64_to_bytes(base64_string: str) -> bytes:
    byte_data = base64.b64decode(base64_string)
    return byte_data

@contextmanager
def push_dir(path: Optional[str] = None):
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

TRet = TypeVar('TRet')
def retry_on_exception(
        function: Callable[[], TRet],
        retry_count = 3,
        retry_delay = 5,
        retry_delay_multiplier = 1,
        retry_exceptions: List[Type[Exception]]=[Exception], 
        raise_exceptions: List[Type[Exception]]=[]) -> Optional[TRet]:
    '''
    Retries the specified function if it throws an exception.

    :param function: The function to execute.
    :param retry_count: The number of times to retry the function.
    :param retry_delay: The delay between retries (seconds).
    :param retry_delay_multiplier: The multiplier to apply to the retry delay after failure.
    :param retry_exceptions: The exception to retry on (Defaults to Exception). (Cannot be used with raise_exceptions)
    :param raise_exceptions: The exceptions to ignore (Defaults to no exceptions ignored). (Cannot be used with retry_exceptions)
    '''
    if retry_count < 0:
        raise ValueError('retry_count must be >= 0')
    if retry_delay < 0:
        raise ValueError('retry_delay must be >= 0')
    if retry_delay_multiplier < 1:
        raise ValueError('retry_delay_multiplier must be >= 1')
    if len(raise_exceptions) > 0 and retry_exceptions != [Exception]:
        raise ValueError('retry_exceptions and raise_exceptions are mutually exclusive')

    for i in range(retry_count):
        try:
            return function()
        except Exception as e:
            # If using the raise_exceptions list, raise those exceptions no matter what
            if len(raise_exceptions) > 0 and type(e) in raise_exceptions:
                raise
            # If use retry_exceptions list, only retry on those exceptions (or all if Exception is in the list)
            elif Exception not in retry_exceptions and type(e) not in retry_exceptions:
                raise
            if i == retry_count - 1:
                raise
            getLogger().info(f'Exception caught {type(e)}: {str(e)}')
            getLogger().info('Retrying in %d seconds...', retry_delay)
            time.sleep(retry_delay)
            retry_delay *= retry_delay_multiplier

def __write_pipeline_variable(name: str, value: str):
    # Create a variable in the build pipeline
    # See https://github.com/microsoft/azure-pipelines-agent/blob/master/docs/design/percentEncoding.md for escape rules
    # Reference code: https://github.com/microsoft/azure-pipelines-task-lib/blob/master/node/taskcommand.ts
    def escape(s: str, is_key: bool):
        s = s.replace("%", "%AZP25").replace("\r", "%0D").replace("\n", "%0A")
        if is_key:
            s = s.replace("]", "%5D").replace(";", "%3B")
        return s
    getLogger().info("Writing pipeline variable %s with value %s" % (name, value))
    print('##vso[task.setvariable variable=%s]%s' % (escape(name, True), escape(value, False)))

def set_environment_variable(name: str, value: str, save_to_pipeline: bool = True):
    """
    Sets an environment variable, both in the python process and to the CI pipeline.
    Saving it to the CI pipeline can be disabled using the save_to_pipeline parameter.    
    """
    if save_to_pipeline:
        __write_pipeline_variable(name, value)
    os.environ[name] = value

class RunCommand:
    '''
    This is a class wrapper around `subprocess.Popen` with an additional set
    of logging features.
    '''

    def __init__(
            self,
            cmdline: List[str],
            success_exit_codes: Optional[List[int]] = None,
            verbose: bool = False,
            echo: bool = True,
            retry: int = 0):
        if cmdline is None:
            raise TypeError('Unspecified command line to be executed.')
        if not cmdline:
            raise ValueError('Specified command line is empty.')

        self.__cmdline = cmdline
        self.__verbose = verbose
        self.__retry = retry
        self.__echo = echo

        if success_exit_codes is None:
            self.__success_exit_codes = [0]
        else:
            self.__success_exit_codes = success_exit_codes

    @property
    def cmdline(self) -> List[str]:
        '''Command-line to use when starting the application.'''
        return self.__cmdline

    @property
    def success_exit_codes(self) -> List[int]:
        '''
        The successful exit codes that the associated process specifies when it
        terminated.
        '''
        return self.__success_exit_codes

    @property
    def echo(self) -> bool:
        '''Enables/Disables echoing of STDOUT'''
        return self.__echo

    @property
    def verbose(self) -> bool:
        '''Enables/Disables verbosity.'''
        return self.__verbose

    @property
    def stdout(self) -> str:
        return self.__stdout.getvalue()

    def __runinternal(self, working_directory: Optional[str] = None) -> Tuple[int, str]:
        should_pipe = self.verbose
        with push_dir(working_directory):
            quoted_cmdline = '$ '
            quoted_cmdline += list2cmdline(self.cmdline)

            if '-AzureFeed' in self.cmdline or '-FeedCredential' in self.cmdline:
                quoted_cmdline = "<dotnet-install command contains secrets, skipping log>"
            
            getLogger().info(quoted_cmdline)

            with Popen(
                    self.cmdline,
                    stdout=PIPE if should_pipe else DEVNULL,
                    stderr=STDOUT,
                    universal_newlines=False,
                    encoding=None,
                    bufsize=0
            ) as proc:
                if proc.stdout is not None:
                    with proc.stdout:
                        self.__stdout = StringIO()
                        for raw_line in iter(proc.stdout.readline, b''):
                            line = raw_line.decode('utf-8', errors='backslashreplace')
                            self.__stdout.write(line)
                            line = line.rstrip()
                            if self.echo:
                                getLogger().info(line)
                proc.wait()
                return (proc.returncode, quoted_cmdline)


    def run(self, working_directory: Optional[str] = None) -> int:
        '''Executes specified shell command.'''

        retrycount = 0
        (returncode, quoted_cmdline) = self.__runinternal(working_directory)
        while returncode not in self.success_exit_codes and self.__retry != 0 and retrycount < self.__retry:
            (returncode, _) = self.__runinternal(working_directory)
            retrycount += 1

        if returncode not in self.success_exit_codes:
            getLogger().error(
                "Process exited with status %s", returncode)
            raise CalledProcessError(
                returncode, quoted_cmdline)
        
        return returncode
