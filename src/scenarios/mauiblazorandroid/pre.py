'''
pre-command
'''
import sys
import requests
from mauishared.mauisharedpython import RemoveAABFiles, GetVersionFromDll
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.precommands import PreCommands
from shared.versionmanager import versionswritejson
from test import EXENAME

setup_loggers(True)
precommands = PreCommands()

# Download what we need
with open ("MauiNuGet.config", "wb") as f:
    f.write(requests.get(f'https://raw.githubusercontent.com/dotnet/maui/{precommands.framework[:6]}/NuGet.config', allow_redirects=True).content)

precommands.install_workload('maui', ['--configfile', 'MauiNuGet.config'])

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
precommands.execute(['--no-restore', '--source', 'MauiNuGet.config'])

RemoveAABFiles(precommands.output)

# Copy the MauiVersion to a file so we have it on the machine
maui_version = GetVersionFromDll(rf".\{const.APPDIR}\obj\Release\{precommands.framework}\{precommands.runtime_identifier}\linked\Microsoft.Maui.dll")
version_dict = { "mauiVersion": maui_version }
versionswritejson(version_dict, rf"{precommands.output}\versions.json")
print(f"Versions: {version_dict}")
