'''
pre-command
'''
import sys, os, subprocess
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

precommands = PreCommands()
precommands.uninstall_workload("wasm-tools")

setup_loggers(True)
precommands.new(template='blazorwasm',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
# ensure that we don't use the workload, which would trigger native
# relinking
precommands.execute(['-p:WasmNativeWorkload=false'])
