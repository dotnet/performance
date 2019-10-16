'''
pre-command
'''
import os
import sys
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.existing(os.path.join(sys.path[0], '50consoletemplate', '50consoletemplate.csproj'))
precommands.execute()
