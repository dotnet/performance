'''
pre-command
'''
import sys
import requests
from mauishared.mauisharedpython import RemoveAABFiles
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.precommands import PreCommands
from shared.versionmanager import versionswritejson, GetVersionFromDllPowershell
from test import EXENAME

setup_loggers(True)

precommands = PreCommands()

# Download what we need
with open ("MauiNuGet.config", "wb") as f:
    f.write(requests.get(f'https://raw.githubusercontent.com/dotnet/maui/{precommands.framework[:6]}/NuGet.config', allow_redirects=True).content)
    
precommands.install_workload('maui', ['--configfile', 'MauiNuGet.config'])

# Setup the Maui folder
precommands.new(template='maui',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

# Build the APK
precommands.execute(['--no-restore', '--source', 'MauiNuGet.config'])

# Remove the aab files as we don't need them, this saves space
RemoveAABFiles(precommands.output)

# Copy the MauiVersion to a file so we have it on the machine
maui_version = GetVersionFromDllPowershell(rf".\{const.APPDIR}\obj\Release\{precommands.framework}\{precommands.runtime_identifier}\linked\Microsoft.Maui.dll")
version_dict = { "mauiVersion": maui_version }
versionswritejson(version_dict, rf"{precommands.output}\versions.json")
print(f"Versions: {version_dict}")
