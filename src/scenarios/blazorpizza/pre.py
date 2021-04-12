'''
pre-command
'''
import sys, os
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

# For blazor3.2 the linker argument '--dump-dependencies' should be added statically to enable linker dump
# For blazor5.0 the linker argument can be added to the command line as an msbuild property
setup_loggers(True)
precommands = PreCommands()
precommands.existing("src", "BlazingPizza.sln")
precommands.execute()
