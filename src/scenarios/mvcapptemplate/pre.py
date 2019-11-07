'''
pre-command
'''
import sys
from performance.logger import setup_loggers
from shared.precommands import PreCommands
from shared import const
from test import EXE_NAME

setup_loggers(True)
precommands = PreCommands()
precommands.new(template='mvc',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXE_NAME,
                working_directory=sys.path[0])
precommands.execute()
