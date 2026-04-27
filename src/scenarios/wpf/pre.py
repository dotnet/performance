'''
pre-command
'''
import sys, subprocess
from performance.logger import setup_loggers
from shared.precommands import PreCommands
from shared import const
from test import EXENAME
from logging import getLogger

setup_loggers(True)

getLogger().info("Checking status of Microsoft-Windows-WPF Event manifest")
exitcode = subprocess.call(['wevtutil','gp','Microsoft-Windows-WPF'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
if(exitcode != 0):
    getLogger().info("Microsoft-Windows-WPF Event manifest not installed, installing...")
    subprocess.call(['wevtutil', 'im', 'C:\\Windows\\Microsoft.NET\\Framework\\v4.0.30319\\WPF\\wpf-etw.man'])
getLogger().info("Microsoft-Windows-WPF installed, continuing")

precommands = PreCommands()
precommands.new(template='wpf',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0])
precommands.execute()