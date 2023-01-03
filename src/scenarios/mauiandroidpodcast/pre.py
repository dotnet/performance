'''
pre-command
'''
import requests
import subprocess
from mauishared.mauisharedpython import RemoveAABFiles
from performance.logger import setup_loggers, getLogger
from shared.precommands import PreCommands
from shared.versionmanager import versionswritejson, GetVersionFromDllPowershell
from shared import const

setup_loggers(True)
precommands = PreCommands()
target_framework_wo_platform = precommands.framework.split('-')[0]

# Download what we need
with open ("MauiNuGet.config", "wb") as f:
    f.write(requests.get(f'https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/NuGet.config', allow_redirects=True).content)

branch = f'{precommands.framework[:6]}'
subprocess.run(['git', 'clone', 'https://github.com/microsoft/dotnet-podcasts.git', '-b', branch, '--single-branch', '--depth', '1'])
subprocess.run(['powershell', '-Command', r'Remove-Item -Path .\\dotnet-podcasts\\.git -Recurse -Force']) # Git files have permission issues, do their deletion separately

workload_install_args = ['--configfile', 'MauiNuGet.config']
if int(target_framework_wo_platform.split('.')[0][3:]) > 7: # Use the rollback file for versions greater than 7
    workload_install_args += ['--from-rollback-file', f'https://aka.ms/dotnet/maui/{target_framework_wo_platform}.json']

precommands.install_workload('maui', workload_install_args) 
precommands.existing(projectdir='./dotnet-podcasts',projectfile='./src/Mobile/Microsoft.NetConf2021.Maui.csproj')

# Build the APK
precommands._restore()
precommands.execute(['--no-restore'])

# Remove the aab files as we don't need them, this saves space
output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
RemoveAABFiles(output_dir)

# Copy the MauiVersion to a file so we have it on the machine
maui_version = GetVersionFromDllPowershell(rf".\{const.APPDIR}\obj\Release\{precommands.framework}\{precommands.runtime_identifier}\linked\Microsoft.Maui.dll")
version_dict = { "mauiVersion": maui_version }
versionswritejson(version_dict, rf"{output_dir}\versions.json")
print(f"Versions: {version_dict}")
