'''
pre-command
'''
import subprocess
import sys
import os
import requests
from zipfile import ZipFile
from performance.logger import setup_loggers, getLogger
from shutil import copyfile
from shared import const
from shared.precommands import PreCommands
from argparse import ArgumentParser
from test import EXENAME, MAUIVERSIONFILE

setup_loggers(True)

parser = ArgumentParser(add_help=False)
parser.add_argument(
        '-o', '--output',
        dest='output_dir',
        required=False,
        type=str,
        help='capture of the output directory')
args, unknown_args = parser.parse_known_args()

# Download what we need
with open ("MauiNuGet.config", "wb") as f:
    f.write(requests.get(f'https://raw.githubusercontent.com/dotnet/maui/net7.0/NuGet.config', allow_redirects=True).content)

precommands = PreCommands()
precommands.install_workload('maui', ['--from-rollback-file', f'https://aka.ms/dotnet/maui/net7.0.json', '--configfile', 'MauiNuGet.config'])

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

# Remove the aab files as we don't need them, this saves space
output_file_partial_path = f"com.companyname.{str.lower(EXENAME)}"
if args.output_dir:
    output_file_partial_path = os.path.join(args.output_dir, output_file_partial_path)

os.remove(f"{output_file_partial_path}-Signed.aab")
os.remove(f"{output_file_partial_path}.aab")

# Copy the MauiVersion to a file so we have it on the machine
result = subprocess.run(['powershell', '-Command', rf'Get-ChildItem .\{const.APPDIR}\obj\Release\net7.0-android\android-arm64\linked\Microsoft.Maui.dll | Select-Object -ExpandProperty VersionInfo | Select-Object ProductVersion | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
maui_version = result.stdout.decode('utf-8').strip()
print(f'MAUI_VERSION: {maui_version}')
if("sha" not in maui_version or "azdo" not in maui_version):
    raise ValueError(f"MAUI_VERSION does not contain sha and azdo indicating failure to retrieve or set the value. MAUI_VERSION: {maui_version}")
with open(f'{args.output_dir}/{MAUIVERSIONFILE}', 'w') as f:
    f.write(maui_version)
