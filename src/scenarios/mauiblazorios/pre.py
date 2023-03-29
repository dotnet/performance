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

# Add the index.razor.cs file
with open(f"{const.APPDIR}/Pages/Index.razor.cs", "w") as indexCSFile:
    indexCSFile.write('''
    using Microsoft.AspNetCore.Components;
    #if ANDROID
        using Android.App;
    #endif\n\n''' 
    + f"    namespace {EXENAME}.Pages" + 
'''
    {
        public partial class Index
        {
            protected override void OnAfterRender(bool firstRender)
            {
                if (firstRender)
                {
                    #if ANDROID
                        var activity = MainActivity.Context as Activity;
                        activity.ReportFullyDrawn();
                    #else
                        System.Console.WriteLine(\"__MAUI_Blazor_WebView_OnAfterRender__\");
                    #endif
                }
            }
        }
    }
''')

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
# NuGet.config file cannot be in the build directory currently due to https://github.com/dotnet/aspnetcore/issues/41397
# shutil.copy('./MauiNuGet.config', './app/Nuget.config')
precommands.execute(['/p:_RequireCodeSigning=false', '/p:ApplicationId=net.dot.mauiblazortesting'])

output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Copy the MauiVersion to a file so we have it on the machine
maui_version = get_version_from_dll_powershell_ios(rf"./{const.APPDIR}/obj/Release/{precommands.framework}/ios-arm64/ipa/Payload/{EXENAME}.app/Microsoft.Maui.dll")
version_dict = { "mauiVersion": maui_version }
versions_write_json(version_dict, rf"{output_dir}/versions.json")
print(f"Versions: {version_dict} from location " + rf"./{const.APPDIR}/obj/Release/{precommands.framework}/ios-arm64/ipa/Payload/{EXENAME}.app/Microsoft.Maui.dll")

