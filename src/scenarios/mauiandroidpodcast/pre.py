'''
pre-command
'''
import os
import requests
import subprocess
from performance.logger import setup_loggers, getLogger
from shared.precommands import PreCommands
from shared import const
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

subprocess.run(['git', 'clone', 'https://github.com/microsoft/dotnet-podcasts.git', '-b', 'net7.0', '--single-branch', '--depth', '1'])
subprocess.run(['powershell', '-Command', r'Remove-Item -Path .\\dotnet-podcasts\\.git -Recurse -Force']) # Git files have permission issues, for their deletion seperately

precommands = PreCommands()
precommands.install_workload('maui', ['--from-rollback-file', f'https://aka.ms/dotnet/maui/net7.0.json', '--configfile', 'MauiNuGet.config'])
precommands.existing(projectdir='./dotnet-podcasts',projectfile='./src/Mobile/Microsoft.NetConf2021.Maui.csproj')

# Build the APK
precommands._restore()
precommands.execute(['--no-restore'])

# Remove the aab files as we don't need them, this saves space
output_file_partial_path = f"com.Microsoft.NetConf2021.Maui"
if args.output_dir:
    output_file_partial_path = os.path.join(args.output_dir, output_file_partial_path)

os.remove(f"{output_file_partial_path}-Signed.aab")
os.remove(f"{output_file_partial_path}.aab")

# Copy the MauiVersion to a file so we have it on the machine
result = subprocess.run(['powershell', '-Command', rf'Get-ChildItem .\{const.APPDIR}\src\Mobile\obj\Release\net7.0-android\android-arm64\linked\Microsoft.Maui.dll | Select-Object -ExpandProperty VersionInfo | Select-Object ProductVersion | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
maui_version = result.stdout.decode('utf-8').strip()
print(f'MAUI_VERSION: {maui_version}')
if("sha" not in maui_version or "azdo" not in maui_version):
    raise ValueError(f"MAUI_VERSION does not contain sha and azdo indicating failure to retrieve or set the value. MAUI_VERSION: {maui_version}")
with open(f'{args.output_dir}/{MAUIVERSIONFILE}', 'w') as f:
    f.write(maui_version)
