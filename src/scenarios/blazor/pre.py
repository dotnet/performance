'''
pre-command
'''
import sys, os, subprocess
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

# For blazor3.2 the linker argument '--dump-dependencies' should be added statically to enable linker dump
# For blazor5.0 the linker argument can be added to the command line as an msbuild property

precommands = PreCommands()

if precommands.has_workload:
    if precommands.framework == 'net6.0' or precommands.framework == 'net7.0':
        subprocess.run(["dotnet", "workload", "uninstall", "wasm-tools"])
    else:
        subprocess.run(["dotnet", "workload", "uninstall", "microsoft-net-sdk-blazorwebassembly-aot"])

setup_loggers(True)
precommands.new(template='blazorwasm',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
precommands.execute()
