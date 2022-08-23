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
# precommands = PreCommands()
# precommands.new(template='console',
#                 output_dir=const.APPDIR,
#                 bin_dir=const.BINDIR,
#                 exename=EXENAME,
#                 working_directory=sys.path[0],
#                 language='F#')
# precommands.execute()

precommands = PreCommands()
precommands.existing(os.path.join(sys.path[0], const.SRCDIR), 'FSharp.Compiler.Service.fsproj')
precommands.execute()