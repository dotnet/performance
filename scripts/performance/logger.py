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

class LoggerStateManager:
    def __init__(self):
        self.logger_initialized = False
        self.logger_opentelemetry_imported = False

    def set_initialized(self, value: bool): self.logger_initialized = value
    def set_opentelemetry_imported(self, value: bool): self.logger_opentelemetry_imported = value
    def get_initialized(self) -> bool: return self.logger_initialized
    def get_opentelemetry_imported(self) -> bool: return self.logger_opentelemetry_imported

logger_state_manager = LoggerStateManager()

try:
    from opentelemetry._logs import set_logger_provider
    from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
    from opentelemetry.sdk._logs.export import BatchLogRecordProcessor, ConsoleLogExporter
    logger_state_manager.set_opentelemetry_imported(True)
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

        # Log console handler
        getLogger().addHandler(__get_console_handler(verbose))

        if enable_open_telemetry_logger:
            if logger_state_manager.get_opentelemetry_imported():
                logger_provider = LoggerProvider()
                set_logger_provider(logger_provider)
                logger_provider.add_log_record_processor(BatchLogRecordProcessor(ConsoleLogExporter()))
                handler = LoggingHandler(level=INFO, logger_provider=logger_provider)

                # Attach OTel handler to logger
                getLogger().addHandler(handler)
            else:
                getLogger().warning('OpenTelemetry not imported. Skipping OpenTelemetry logger initialization.')

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
        makedirs(log_dir, exist_ok=True)

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

    if not logger_state_manager.get_initialized():
        __initialize(verbose)
        logger_state_manager.set_initialized(True)
