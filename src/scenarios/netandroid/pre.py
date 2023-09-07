'''
pre-command
'''
import shutil
import sys
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import remove_aab_files, install_versioned_maui
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_version_from_dll_powershell
from test import EXENAME

setup_loggers(True)

precommands = PreCommands()
install_versioned_maui(precommands)

# Setup the app folder
precommands.new(template='android',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

# Build the APK
shutil.copy('./MauiNuGet.config', './app/Nuget.config')
precommands.execute([])

# Remove the aab files as we don't need them, this saves space
output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Copy the netAndroidVersion to a file so we have it on the machine
net_android_version = get_version_from_dll_powershell(rf".\{const.APPDIR}\obj\Release\{precommands.framework}\android-arm64\linked\Mono.Android.dll")
version_dict = { "netAndroidVersion": net_android_version }
versions_write_json(version_dict, rf"{output_dir}\versions.json")
print(f"Versions: {version_dict} from location " + rf".\{const.APPDIR}\obj\Release\{precommands.framework}\android-arm64\linked\Mono.Android.dll")
