import subprocess
import os
import requests
from shared.precommands import PreCommands

# Remove the aab files as we don't need them, this saves space in the correlation payload
def remove_aab_files(output_dir="."):
    file_list = os.listdir(output_dir)
    for file in file_list:
        if file.endswith(".aab"):
            os.remove(os.path.join(output_dir, file))

def install_versioned_maui(precommands: PreCommands):
    target_framework_wo_platform = precommands.framework.split('-')[0]

    # Download what we need
    with open ("MauiNuGet.config", "wb") as f:
        f.write(requests.get(f'https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/NuGet.config', allow_redirects=True).content)

    workload_install_args = ['--configfile', 'MauiNuGet.config', '--skip-sign-check']
    if int(target_framework_wo_platform.split('.')[0][3:]) > 7: # Use the rollback file for versions greater than 7
        workload_install_args += ['--from-rollback-file', f'https://maui.blob.core.windows.net/metadata/rollbacks/{target_framework_wo_platform}.json']

    precommands.install_workload('maui', workload_install_args) 
