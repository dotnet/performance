'''
pre-command: Example call 'python .\pre.py publish -f net7.0-windows10.0.19041.0 -c Release'
'''
import shutil
import subprocess
import sys
import os
from performance.logger import setup_loggers, getLogger
from shared.codefixes import insert_after
from shared.precommands import PreCommands
from shared import const
from shared.mauisharedpython import install_latest_maui, MauiNuGetConfigContext
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)

precommands = PreCommands()

install_latest_maui(precommands)
precommands.print_dotnet_info()

# Use context manager to temporarily merge MAUI's NuGet feeds into repo config
# This ensures both dotnet new and dotnet build/publish have access to MAUI packages

with MauiNuGetConfigContext(precommands.framework):
    precommands.new(template='maui-blazor',
                    output_dir=const.APPDIR,
                    bin_dir=const.BINDIR,
                    exename=EXENAME,
                    working_directory=sys.path[0],
                    no_restore=False)
    
    shutil.copy2(os.path.join(const.SRCDIR, 'Replacement.Index.razor.cs'), os.path.join(const.APPDIR, 'Pages', 'Index.razor.cs'))
    precommands.add_startup_logging(os.path.join('Pages', 'Index.razor.cs'), "if (firstRender) {")
    precommands.execute(['/p:Platform=x64','/p:WindowsAppSDKSelfContained=True','/p:WindowsPackageType=None','/p:WinUISDKReferences=False','/p:PublishReadyToRun=true'])
# NuGet.config is automatically restored after this block
