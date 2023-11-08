'''
pre-command
'''
import shutil
import sys
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import remove_aab_files, install_versioned_maui
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_version_from_dll_powershell_ios
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()
install_versioned_maui(precommands)

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
            System.Console.WriteLine("__MAUI_Blazor_WebView_OnAfterRender__");
        }
    }
}
''')

# Build the IPA
# NuGet.config file cannot be in the build directory currently due to https://github.com/dotnet/aspnetcore/issues/41397
# shutil.copy('./MauiNuGet.config', './app/Nuget.config')
shutil.copy('./MauiNuGet.config', './app/Nuget.config')
precommands.execute(['/p:_RequireCodeSigning=false', '/p:ApplicationId=net.dot.mauiblazortesting'])

output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Copy the MauiVersion to a file so we have it on the machine
maui_version = get_version_from_dll_powershell_ios(rf"./{const.APPDIR}/obj/Release/{precommands.framework}/ios-arm64/linked/Microsoft.Maui.dll")
version_dict = { "mauiVersion": maui_version }
versions_write_json(version_dict, rf"{output_dir}/versions.json")
print(f"Versions: {version_dict} from location " + rf"./{const.APPDIR}/obj/Release/{precommands.framework}/ios-arm64/linked/Microsoft.Maui.dll")

