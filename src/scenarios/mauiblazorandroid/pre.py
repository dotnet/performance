'''
pre-command
'''
import shutil
import sys
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import remove_aab_files, install_latest_maui
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_version_from_dll_powershell
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)
logger.info("Starting pre-command for Maui Blazor Android Default App")

precommands = PreCommands()

install_latest_maui(precommands)
precommands.print_dotnet_info()

# Setup the Maui folder
precommands.new(template='maui-blazor',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

# Update the home.razor file with the code
with open(f"{const.APPDIR}/Components/Pages/Home.razor", "a") as homeRazorFile:
    homeRazorFile.write(
'''
@code {
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            var activity = MainActivity.Context as Activity;
            activity.ReportFullyDrawn();
        }
    }
}
''')
    
# Open the _Imports.razor file for appending
with open(f"{const.APPDIR}/_Imports.razor", "a") as importsFile:
    importsFile.write("@using Android.App;")

# Replace line in the Android MainActivity.cs file
with open(f"{const.APPDIR}/Platforms/Android/MainActivity.cs", "r") as mainActivityFile:
    mainActivityFileLines = mainActivityFile.readlines()

with open(f"{const.APPDIR}/Platforms/Android/MainActivity.cs", "w") as mainActivityFile:
    for line in mainActivityFileLines:
        if line.startswith("{"):
            mainActivityFile.write("{\npublic static Android.Content.Context Context { get; private set; }\npublic MainActivity() { Context = this; }")
        else:
            mainActivityFile.write(line)

# Build the APK
precommands.execute([])

output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Copy the MauiVersion to a file so we have it on the machine
maui_version = get_version_from_dll_powershell(rf".\{const.APPDIR}\obj\Release\{precommands.framework}\android-arm64\linked\Microsoft.Maui.dll")
version_dict = { "mauiVersion": maui_version }
versions_write_json(version_dict, rf"{output_dir}\versions.json")
print(f"Versions: {version_dict}")

