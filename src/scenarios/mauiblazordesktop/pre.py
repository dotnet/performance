'''
pre-command: Example call 'python .\pre.py publish -f net6.0-windows10.0.19041.0 -c Release'
'''
import shutil
import subprocess
import sys
import os
from performance.logger import setup_loggers
from shared.codefixes import insert_after
from shared.precommands import PreCommands
from shared import const
from test import EXENAME
import requests

setup_loggers(True)
NugetURL = 'https://raw.githubusercontent.com/dotnet/maui/main/NuGet.config'
NugetFile = requests.get(NugetURL)
open('./Nuget.config', 'wb').write(NugetFile.content)

precommands = PreCommands()
precommands.install_workload('maui', ['--from-rollback-file', 'https://aka.ms/dotnet/maui/net6.0.json', '--configfile', './Nuget.config'])
precommands.new(template='maui-blazor',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

subprocess.run(["dotnet", "add", "./app", "package", "Microsoft.WindowsAppSDK"]) # Add the package reference for the Microsoft.WindowsAppSDK for self-contained running
shutil.copy2(os.path.join(const.SRCDIR, 'Replacement.Index.razor.cs'), os.path.join(const.APPDIR, 'Pages', 'Index.razor.cs'))
precommands.add_startup_logging(os.path.join('Pages', 'Index.razor.cs'), "if (firstRender) {")
insert_after(os.path.join(const.APPDIR, 'MauiBlazorDesktopTesting.csproj'), "<TargetFrameworks>net6.0-android;net6.0-ios;net6.0-maccatalyst</TargetFrameworks>", "        <TargetFrameworks>$(TargetFrameworks);net6.0-windows10.0.19041.0</TargetFrameworks>")

print("File: ")
with open(os.path.join(const.APPDIR, 'Pages', 'Index.razor.cs'), 'r') as f:
    print(f.read())

precommands.execute(['/p:Platform=x64','/p:WindowsAppSDKSelfContained=True','/p:WindowsPackageType=None','/p:WinUISDKReferences=False','/p:PublishReadyToRun=true'])
shutil.copyfile('DesktopTestBinlog.binlog', os.path.join(os.environ.get('HELIX_WORKITEM_UPLOAD_ROOT'), 'Binlog.binlog'))
