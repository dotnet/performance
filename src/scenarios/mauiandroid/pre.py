'''
pre-command
'''
import os
import shutil
import sys
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import remove_aab_files, install_latest_maui, MauiNuGetConfigContext
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_sdk_versions
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)
logger.info("Starting pre-command for MAUI Android template app (dotnet new maui)")

precommands = PreCommands()

# Use context manager to temporarily merge MAUI's NuGet feeds into repo config
# This ensures dotnet package search, dotnet new, and dotnet build/publish have access to MAUI packages
with MauiNuGetConfigContext(precommands.framework):
    install_latest_maui(precommands)
    precommands.print_dotnet_info()
    # Setup the Maui folder - will use merged NuGet.config with MAUI feeds
    precommands.new(template='maui',
                    output_dir=const.APPDIR,
                    bin_dir=const.BINDIR,
                    exename=EXENAME,
                    working_directory=sys.path[0],
                    no_restore=False)
    
    # Build the APK - will also use merged NuGet.config
    precommands.execute([])
    # NuGet.config is automatically restored after this block

# Remove the aab files as we don't need them, this saves space
output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Extract the versions of used SDKs from the linked folder DLLs
dll_folder = os.path.join(".", const.APPDIR, "obj", precommands.configuration, precommands.framework, "android-arm64", "linked")
version_dict = get_sdk_versions(dll_folder)
versions_write_json(version_dict, os.path.join(output_dir, "versions.json"))
print(f"Versions: {version_dict} from location {dll_folder}")