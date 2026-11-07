'''
pre-command
'''
import sys, os, subprocess, shutil
from performance.logger import setup_loggers
from shared import const
from shared.precommands import PreCommands
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
precommands.existing("src", "BlazingPizza.sln")

precommands.install_workload('wasm-tools')
f = open(os.path.join(os.getcwd(), "app", "BlazingPizza.Client", "BlazingPizza.Client.csproj"), 'r')
outFileText = ""
for line in f.readlines():
    if "<PropertyGroup>" in line:
        outFileText += line
        outFileText += "    <RunAOTCompilation>true</RunAOTCompilation>" + os.linesep
    else:
        outFileText += line
f.close()
os.remove(os.path.join(os.getcwd(), "app", "BlazingPizza.Client", "BlazingPizza.Client.csproj"))
f = open(os.path.join(os.getcwd(), "app", "BlazingPizza.Client", "BlazingPizza.Client.csproj"), 'w')
f.write(outFileText)
f.close()
precommands.execute()
