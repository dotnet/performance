'''
Version File Manager
'''
import json
import os
import subprocess

from typing import Dict

def versions_write_json(versiondict: Dict[str, str], outputfile: str = 'versions.json'):
    with open(outputfile, 'w', encoding='utf-8') as file:
        json.dump(versiondict, file)

def versions_read_json(inputfile: str = 'versions.json'):
    with open(inputfile, 'r', encoding='utf-8') as file:
        return json.load(file)

def versions_write_env(versiondict: Dict[str, str]):
    for key, value in versiondict.items():
        os.environ[key.upper()] = value # Windows automatically converts environment variables to uppercase, match this behavior everywhere

def versions_read_json_file_save_env(inputfile: str = 'versions.json'):
    versions = versions_read_json(inputfile)
    print(f"Versions: {versions}")
    versions_write_env(versions)

    # Remove the versions.json file if we are in the lab to ensure SOD doesn't pick it up
    if "PERFLAB_INLAB" in os.environ and os.environ["PERFLAB_INLAB"] == "1":
        os.remove(inputfile)

def get_version_from_dll_powershell(dll_path: str):
    result = subprocess.run(['powershell', '-Command', rf'Get-ChildItem {dll_path} | Select-Object -ExpandProperty VersionInfo | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
    return result.stdout.decode('utf-8').strip()

def get_version_from_dll_powershell_ios(dll_path: str):
    result = subprocess.run(['pwsh', '-Command', rf'Get-ChildItem {dll_path} | Select-Object -ExpandProperty VersionInfo | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=False)
    return result.stdout.decode('utf-8').strip()