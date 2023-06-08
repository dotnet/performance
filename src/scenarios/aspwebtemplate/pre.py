'''
pre-command
'''
import sys
import shutil
import os.path
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from aspwebtemplate.test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.new(template='web',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
precommands.execute()