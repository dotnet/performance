'''
pre-command
'''
import sys
from performance.logger import setup_loggers
from shared.precommands import PreCommands
from shared import const
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.new(template='wpf',
                output_dir=const.SRCDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
