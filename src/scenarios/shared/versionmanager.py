'''
Version File Manager
'''
import json
import os
import subprocess

def versionswritejson(versiondict: dict, outputfile = 'versions.json'):
    with open(outputfile, 'w') as file:
        json.dump(versiondict, file)

def versionsreadjson(inputfile = 'versions.json'):
    with open(inputfile, 'r') as file:
        return json.load(file)

def versionswriteenv(versiondict: dict):
    for key, value in versiondict.items():
        os.environ[key] = value

def versionsreadjsonfilesaveenv(inputfile = 'versions.json'):
    versions = versionsreadjson(inputfile)
    print(f"Versions: {versions}")
    versionswriteenv(versions)

    # Remove the versions.json file if we are in the lab to ensure SOD doesn't pick it up
    if "PERFLAB_INLAB" in os.environ and os.environ["PERFLAB_INLAB"] == "1":
        os.remove(inputfile)

def GetVersionFromDllPowershell(dll_path: str):
    result = subprocess.run(['powershell', '-Command', rf'Get-ChildItem {dll_path} | Select-Object -ExpandProperty VersionInfo | Select-Object -ExpandProperty ProductVersion'], stdout=subprocess.PIPE, stderr=subprocess.STDOUT, shell=True)
    return result.stdout.decode('utf-8').strip()
