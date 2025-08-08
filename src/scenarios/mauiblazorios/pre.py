'''
pre-command
'''
import shutil
import sys
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import remove_aab_files, install_latest_maui
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_sdk_versions
from test import EXENAME

setup_loggers(True)
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
            System.Console.WriteLine("__MAUI_Blazor_WebView_OnAfterRender__");
        }
    }
}
''')

# Build the IPA
# NuGet.config file cannot be in the build directory due same cause as to https://github.com/dotnet/aspnetcore/issues/41397
precommands.execute(['/p:_RequireCodeSigning=false', '/p:EnableCodeSigning=false', '/p:ApplicationId=net.dot.mauiblazortesting'])

output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Extract the versions of used SDKs from the linked folder DLLs
version_dict = get_sdk_versions(rf"./{const.APPDIR}/obj/Release/{precommands.framework}/ios-arm64/linked", False)
versions_write_json(version_dict, rf"{output_dir}/versions.json")
print(f"Versions: {version_dict} from location " + rf"./{const.APPDIR}/obj/Release/{precommands.framework}/ios-arm64/linked")

