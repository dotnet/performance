'''
pre-command
'''
import subprocess
from performance.logger import setup_loggers, getLogger
from shared.precommands import PreCommands
from shared.mauisharedpython import remove_aab_files, install_versioned_maui
from shared.versionmanager import versions_write_json, get_version_from_dll_powershell
from shared import const

setup_loggers(True)
precommands = PreCommands()
install_versioned_maui(precommands)

branch = f'{precommands.framework[:6]}'
subprocess.run(['git', 'clone', 'https://github.com/microsoft/dotnet-podcasts.git', '-b', branch, '--single-branch', '--depth', '1'])
subprocess.run(['powershell', '-Command', r'Remove-Item -Path .\\dotnet-podcasts\\.git -Recurse -Force']) # Git files have permission issues, do their deletion separately

precommands.existing(projectdir='./dotnet-podcasts', projectfile='./src/Mobile/Microsoft.NetConf2021.Maui.csproj')

# Build the APK
precommands.execute(['/p:_RequireCodeSigning=false', '/p:ApplicationId=net.dot.netconf2021.maui'])

# Remove the aab files as we don't need them, this saves space
output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Copy the MauiVersion to a file so we have it on the machine
# maui_version = get_version_from_dll_powershell(rf".\{const.APPDIR}\obj\Release\{precommands.framework}\android-arm64\linked\Microsoft.Maui.dll")
# version_dict = { "mauiVersion": maui_version }
# versions_write_json(version_dict, rf"{output_dir}\versions.json")
# print(f"Versions: {version_dict}")
