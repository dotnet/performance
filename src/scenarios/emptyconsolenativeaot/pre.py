import sys
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME
from shutil import copyfile
import os

setup_loggers(True)
precommands = PreCommands()
precommands.new(template='console',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
precommands.execute(['/p:PublishAot=true', '/p:StripSymbols=true', '/p:IlcGenerateMstatFile=true'])

src = os.path.join(const.APPDIR, 'obj', precommands.configuration, precommands.framework, precommands.runtime_identifier, 'native', EXENAME)
dst = os.path.join(precommands.output, f'{EXENAME}.mstat')
copyfile(src, dst)
