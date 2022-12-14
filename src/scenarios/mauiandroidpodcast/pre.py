'''
pre-command
'''
import requests
import subprocess
from mauishared.mauisharedpython import RemoveAABFiles, GetVersionFromDll
from performance.logger import setup_loggers, getLogger
from shared.precommands import PreCommands
from shared.versionmanager import versionswritejson
from shared import const

setup_loggers(True)
precommands = PreCommands()

# Download what we need
with open ("MauiNuGet.config", "wb") as f:
    f.write(requests.get(f'https://raw.githubusercontent.com/dotnet/maui/{precommands.framework[:6]}/NuGet.config', allow_redirects=True).content)

subprocess.run(['git', 'clone', 'https://github.com/microsoft/dotnet-podcasts.git', '-b', f'{precommands.framework[:6]}', '--single-branch', '--depth', '1'])
subprocess.run(['powershell', '-Command', r'Remove-Item -Path .\\dotnet-podcasts\\.git -Recurse -Force']) # Git files have permission issues, for their deletion seperately

precommands.install_workload('maui', ['--configfile', 'MauiNuGet.config'])
precommands.existing(projectdir='./dotnet-podcasts',projectfile='./src/Mobile/Microsoft.NetConf2021.Maui.csproj')

# Build the APK
precommands._restore()
precommands.execute(['--no-restore'])

# Remove the aab files as we don't need them, this saves space
RemoveAABFiles(precommands.output)

maui_version = GetVersionFromDll(f".\{const.APPDIR}\src\Mobile\obj\Release\{precommands.framework}\{precommands.runtime_identifier}\linked\Microsoft.Maui.dll")
version_dict = { "maui_version": maui_version }
versionswritejson(version_dict, rf"{precommands.output}\versions.json")
print(f"Versions: {version_dict}")
