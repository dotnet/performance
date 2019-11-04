'''
pre-command
'''
import sys
from performance.logger import setup_loggers
from shared.precommands import PreCommands

setup_loggers(True)
precommands = PreCommands()
precommands.execute()

