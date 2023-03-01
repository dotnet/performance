'''
pre-command
'''
import subprocess
import os
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.codefixes import insert_after, replace_line
from shared.mauisharedpython import remove_aab_files, install_versioned_maui
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_version_from_dll_powershell

setup_loggers(True)
precommands = PreCommands()

# branch = f'{precommands.framework[:6]}'
if not os.path.exists('./maui'):
    subprocess.run(['git', 'clone', 'https://github.com/dotnet/maui.git', '-b', 'net8.0', '--single-branch', '--depth', '1'])
    subprocess.run(['powershell', '-Command', r'Remove-Item -Path .\\maui\\.git -Recurse -Force']) # Git files have permission issues, for their deletion seperately

install_versioned_maui(precommands)

# This part needs to be worked out as the precommands build is failing, but external build succeeds (dotnet build src/Core/tests/Benchmarks.Droid/Benchmarks.Droid.csproj -t:Benchmark -c Release) TODO Fix that/figure out the failure reason
precommands.existing(projectdir='./maui',projectfile='./src/Core/tests/Benchmarks.Droid/Benchmarks.Droid.csproj')
replace_line(f"./{const.APPDIR}/src/Core/tests/Benchmarks.Droid/Benchmarks.Droid.csproj", "<EmbedAssembliesIntoApk>false</EmbedAssembliesIntoApk>", "<!-- <EmbedAssembliesIntoApk>false</EmbedAssembliesIntoApk> -->")
replace_line(f"./{const.APPDIR}/src/Core/tests/Benchmarks.Droid/Benchmarks.Droid.csproj", "<PackageReference Include=\"BenchmarkDotNet\" Version=\"0.13.3\" />", "<PackageReference Include=\"BenchmarkDotNet\" Version=\"0.13.5.2112\" />")
insert_after(f"./{const.APPDIR}/NuGet.config", "<add key=\"dotnet-eng\" value=\"https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json\" protocolVersion=\"3\" />", "<add key=\"bdn-nightly\" value=\"https://ci.appveyor.com/nuget/benchmarkdotnet\" />")

# Build the APK
# workload_install_args = ['--configfile', './maui/Nuget.config']
# precommands.install_workload('wasm-tools', workload_install_args)
# precommands._restore()
precommands.execute()

# Remove the aab files as we don't need them, this saves space
output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)

# Copy the MauiVersion to a file so we have it on the machine
maui_version = get_version_from_dll_powershell(rf".\{const.APPDIR}\obj\Release\{precommands.framework}\android-arm64\linked\Microsoft.Maui.dll")
version_dict = { "mauiVersion": maui_version }
versions_write_json(version_dict, rf"{output_dir}\versions.json")
print(f"Versions: {version_dict}")
