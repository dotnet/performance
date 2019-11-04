'''
pre-command
'''
import sys
import os
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.existing(os.path.join(sys.path[0], const.SRCDIR), 'staticwinformstemplate.csproj')
precommands.add_startup_logging("Form1.cs", "InitializeComponent();")
precommands.execute()
