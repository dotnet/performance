'''
pre-command
'''
import sys
import os
from performance.logger import setup_loggers
from shared.precommands import PreCommands
from shared import const

def findallprojectfiles():
    projectfiles = [] 
    for (path, dirnames, filenames) in os.walk(const.SRCDIR):
        for filename in filenames:
            if filename.endswith('.csproj'):
                projectfiles.append(os.path.relpath(os.path.join(path,filename), const.SRCDIR))
    return projectfiles # main project file \mvc\mvc.csproj is the last element  

setup_loggers(True)
precommands = PreCommands()
precommands.existing(os.path.join(sys.path[0], const.SRCDIR), findallprojectfiles())
precommands.execute()
