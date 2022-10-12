'''
pre-command: Example call 'python .\pre.py publish -f net7.0-windows10.0.19041.0 -c Release'
'''
import shutil
import subprocess
import sys
import os
from performance.logger import setup_loggers
from shared.precommands import PreCommands
from shared import const
from test import EXENAME
import requests

setup_loggers(True)
NugetURL = 'https://raw.githubusercontent.com/dotnet/maui/main/NuGet.config'
NugetFile = requests.get(NugetURL)
open('./Nuget.config', 'wb').write(NugetFile.content)

precommands = PreCommands()
precommands.install_workload('maui', ['--from-rollback-file', 'https://aka.ms/dotnet/maui/net7.0.json', '--configfile', './Nuget.config'])
precommands.new(template='maui',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

subprocess.run(["dotnet", "add", "./app", "package", "Microsoft.WindowsAppSDK"]) # Add the package reference for the Microsoft.WindowsAppSDK for self-contained running
precommands.execute(['/p:Platform=x64','/p:WindowsAppSDKSelfContained=True','/p:WindowsPackageType=None','/p:WinUISDKReferences=False','/p:PublishReadyToRun=true'])
