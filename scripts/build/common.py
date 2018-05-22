'''
Common functionality used by the .NET Performance Repository build scripts.
'''

from contextlib import contextmanager

import datetime
import logging
import os
import sys
import time


LAUNCH_TIME = time.time()
LOGGING_FORMATTER = logging.Formatter(
    fmt='[%(asctime)s][%(levelname)s] %(message)s',
    datefmt="%Y-%m-%d %H:%M:%S")


def is_supported_version() -> bool:
    '''Checks if the script is running on the supported version (>=3.5).'''
    return sys.version_info.major > 2 and sys.version_info.minor > 4


def log_start_message(name) -> None:
    '''Used to log a start event message header.'''
    start_msg = "Script started at {}".format(
        str(datetime.datetime.fromtimestamp(LAUNCH_TIME)))
    logging.getLogger(name).info('-' * len(start_msg))
    logging.getLogger(name).info(start_msg)
    logging.getLogger(name).info('-' * len(start_msg))


def get_script_path() -> str:
    '''Gets this script directory.'''
    return sys.path[0]


def get_repo_root_path() -> str:
    '''Gets repository root directory.'''
    return os.path.abspath(os.path.join(get_script_path(), '..'))


@contextmanager
def push_dir(path: str = None) -> None:
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
