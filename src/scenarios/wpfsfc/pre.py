'''
pre-command
'''
import sys
import os, subprocess
from performance.logger import setup_loggers
from shared.precommands import PreCommands
from shared import const
from logging import getLogger

setup_loggers(True)

getLogger().info("Checking status of Microsoft-Windows-WPF Event manifest")
exitcode = subprocess.call(['wevtutil','gp','Microsoft-Windows-WPF'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
if(exitcode != 0):
    getLogger().info("Microsoft-Windows-WPF Event manifest not installed, installing...")
    subprocess.call(['wevtutil', 'im', 'C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\WPF\\wpf-etw.man'])
getLogger().info("Microsoft-Windows-WPF installed, continuing")

precommands = PreCommands()
precommands.existing(os.path.join(sys.path[0], const.SRCDIR), 'wpfsfc.csproj')
precommands.execute()