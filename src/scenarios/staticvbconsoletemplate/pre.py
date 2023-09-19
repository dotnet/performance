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
precommands.existing(os.path.join(sys.path[0], const.SRCDIR), ('%s.vbproj' % EXENAME))
precommands.add_onmain_logging("Program.vb", "    Sub Main(args As String())", language_file_extension="vb")
precommands.execute()
