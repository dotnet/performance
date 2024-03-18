'''
Support module around logging functionality for the performance scripts.
'''

from datetime import datetime
from logging import FileHandler, Formatter, StreamHandler
from logging import getLogger
from logging import INFO, WARNING
from os import getpid, makedirs, path
from time import time

import sys
import __main__

from .common import get_repo_root_path

__initialized = False
try:
    from opentelemetry._logs import set_logger_provider
    from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
    from opentelemetry.sdk._logs.export import BatchLogRecordProcessor, ConsoleLogExporter
except ImportError:
    pass

def setup_loggers(verbose: bool, enable_open_telemetry_logger: bool = False):
    '''Setup the root logger for the performance scripts.'''
    def __formatter() -> Formatter:
        fmt = '[%(asctime)s][%(levelname)s] %(message)s'
        datefmt = "%Y/%m/%d %H:%M:%S"
        return Formatter(fmt=fmt, datefmt=datefmt)

    def __initialize(verbose: bool):
        '''Initializes the loggers used by the script.'''
        launch_datetime = datetime.fromtimestamp(time())

        getLogger().setLevel(INFO)

        if enable_open_telemetry_logger:
            logger_provider = LoggerProvider()
            set_logger_provider(logger_provider)
            logger_provider.add_log_record_processor(BatchLogRecordProcessor(ConsoleLogExporter()))
            handler = LoggingHandler(level=INFO, logger_provider=logger_provider)

            # Attach OTel handler to logger
            getLogger().addHandler(handler)

        # Log console handler
        getLogger().addHandler(__get_console_handler(verbose))

        # Log file handler
        log_file_name = __generate_log_file_name(launch_datetime)
        getLogger().addHandler(__get_file_handler(log_file_name))

        # Log level
        getLogger().setLevel(INFO)

        start_msg = "Initializing logger {}".format(str(launch_datetime))
        getLogger().info('-' * len(start_msg))
        getLogger().info(start_msg)
        getLogger().info('-' * len(start_msg))

    def __generate_log_file_name(launch_datetime: datetime) -> str:
        '''Generates a unique log file name for the current script.'''
        log_dir = path.join(get_repo_root_path(), 'logs')
        if not path.exists(log_dir):
            makedirs(log_dir)

        if not hasattr(__main__, '__file__'):
            script_name = 'python_interactive_mode'
        else:
            script_name = path.splitext(path.basename(sys.argv[0]))[0]

        timestamp = launch_datetime.strftime("%Y%m%d%H%M%S")
        log_file_name = '{}-{}-pid{}.log'.format(
            timestamp, script_name, getpid())
        return path.join(log_dir, log_file_name)

    def __get_console_handler(verbose: bool):
        console_handler = StreamHandler()
        level = INFO if verbose else WARNING
        console_handler.setLevel(level)
        console_handler.setFormatter(__formatter())
        return console_handler

    def __get_file_handler(file: str) -> FileHandler:
        file_handler = FileHandler(file, encoding='utf-8')
        file_handler.setLevel(INFO)
        file_handler.setFormatter(__formatter())
        return file_handler

    global __initialized
    if not __initialized:
        __initialize(verbose)
        __initialized = True
