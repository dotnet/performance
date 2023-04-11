import sys
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME
from shutil import copyfile
import os
from performance.common import iswin

setup_loggers(True)
precommands = PreCommands()
precommands.new(template='console',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
args = ['/p:PublishAot=true', '/p:StripSymbols=true', '/p:IlcGenerateMstatFile=true']
if not iswin():
    args.extend(['/p:ObjCopyName=objcopy'])
precommands.execute(args)

src = os.path.join(const.APPDIR, 'obj', precommands.configuration, precommands.framework, precommands.runtime_identifier, 'native', f'{EXENAME}.mstat')
dst = os.path.join(precommands.output, f'{EXENAME}.mstat')
copyfile(src, dst)

delete_files = [f'{EXENAME}.pdb', f'{EXENAME}.dbg']
for file in delete_files:
    f = os.path.join(precommands.output, file)
    if os.path.exists(f):
        os.remove(f)