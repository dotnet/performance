'''
pre-command
WARNING: blazorserver is no longer a template in dotnet 8.0 (https://github.com/dotnet/performance/issues/3108)
         runs have been removed but keeping the test folder for now
'''
import sys
import shutil
import os.path
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.new(template='blazorserver',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
precommands.execute()
shutil.copy(os.path.join('src', 'Program.cs'), os.path.join(const.APPDIR, 'Program.cs'))
