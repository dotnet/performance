import subprocess
import os

# Remove the aab files as we don't need them, this saves space in the correlation payload
def RemoveAABFiles(output_dir="."):
    file_list = os.listdir(output_dir)
    for file in file_list:
        if file.endswith(".aab"):
            os.remove(os.path.join(output_dir, file))

def GetVersionFromDll(dll_path: str):
    result = subprocess.run(['powershell', '-Command', rf'Get-ChildItem {dll_path} | Select-Object -ExpandProperty VersionInfo | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
    return result.stdout.decode('utf-8').strip()
