'''
pre-command
'''
import sys
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.new(template='console',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                language='vb')
precommands.add_onmain_logging("Program.vb", "    Sub Main(args As String())", language_file_extension="vb")
precommands.execute()
