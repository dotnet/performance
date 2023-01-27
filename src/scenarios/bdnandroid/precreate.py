'''
pre-command
'''
import subprocess
import os
from performance.logger import setup_loggers, getLogger
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_version_from_dll_powershell
from shared import const

setup_loggers(True)
precommands = PreCommands()

# branch = f'{precommands.framework[:6]}'
if not os.path.exists('./maui'):
    subprocess.run(['git', 'clone', 'https://github.com/dotnet/maui.git', '-b', 'net8.0', '--single-branch', '--depth', '1'])
    subprocess.run(['powershell', '-Command', r'Remove-Item -Path .\\maui\\.git -Recurse -Force']) # Git files have permission issues, for their deletion seperately

# This part needs to be worked out as the precommands build is failing, but external build succeeds (dotnet build src/Core/tests/Benchmarks.Droid/Benchmarks.Droid.csproj -t:Benchmark -c Release) TODO Fix that/figure out the failure reason
precommands.existing(projectdir='./maui',projectfile='./src/Core/tests/Benchmarks.Droid/Benchmarks.Droid.csproj')

# Build the APK
# workload_install_args = ['--configfile', './maui/Nuget.config']
# precommands.install_workload('wasm-tools', workload_install_args)
# precommands._restore()
precommands.execute()

# Remove the aab files as we don't need them, this saves space
output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
