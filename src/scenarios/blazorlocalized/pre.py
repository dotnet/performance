'''
pre-command
'''
import sys, os, subprocess
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.uninstall_workload("wasm-tools")
precommands.existing("src", "BlazorLocalized.sln")
precommands.execute()
