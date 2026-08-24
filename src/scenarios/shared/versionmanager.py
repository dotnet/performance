'''
Version File Manager
'''
import json
import os
import platform
import subprocess
from performance.logger import getLogger
from datetime import datetime
from urllib.error import URLError
from urllib.request import urlopen

DOTNET_SOURCE_MANIFEST_URL = "https://raw.githubusercontent.com/dotnet/dotnet/{commit}/src/source-manifest.json"

def versions_write_json(versiondict: dict[str, str], outputfile: str = 'versions.json'):
    with open(outputfile, 'w', encoding='utf-8') as file:
        json.dump(versiondict, file)

def versions_read_json(inputfile: str = 'versions.json'):
    with open(inputfile, 'r', encoding='utf-8') as file:
        return json.load(file)

def versions_write_env(versiondict: dict[str, str]):
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

def get_version_from_dll(dll_path: str):
    '''Cross-platform DLL version extraction. Uses powershell on Windows, pwsh elsewhere.'''
    if platform.system() == 'Windows':
        return get_version_from_dll_powershell(dll_path)
    else:
        return get_version_from_dll_powershell_ios(dll_path)

def get_runtime_commit_hash(dotnet_commit_hash: str) -> str:
    source_manifest_url = DOTNET_SOURCE_MANIFEST_URL.format(commit=dotnet_commit_hash)
    with urlopen(source_manifest_url, timeout=60) as response:
        source_manifest = json.load(response)

    for repository in source_manifest.get("repositories", []):
        if repository.get("path") == "runtime":
            runtime_commit_hash = repository.get("commitSha")
            if runtime_commit_hash:
                return runtime_commit_hash

    raise ValueError(f"Runtime commit hash not found in {source_manifest_url}")

def get_sdk_versions(dll_folder_path: str, windows_powershell: bool = True) -> dict[str, str]:
    '''
    Get the SDK versions from the used dlls
    :param dll_folder_path: The folder path where the dlls are located
    :return: A dictionary with the SDK version identifiers and commit hashes
    '''

    def parse_version_output(output: str) -> tuple[str, str]:
        """
        Parse the output of the PowerShell command to extract version and commit hash.
        :param output: The output string from the PowerShell command.
        :return: A tuple containing the version and commit hash.
        """
        version = None
        commit = None

        if '+' in output: # Handle "versi<version>+<commit>" format
            parts = output.split('+')
            version = parts[0].strip()
            commit = parts[1].strip()
        else: # Handle "<version>; git-rev-head:<commit>; git-branch:<branch>" format
            parts = output.split(';')
            version = parts[0].strip()

            for part in parts:
                if 'git-rev-head:' in part:
                    commit = part.split(':', 1)[1].strip()
                    break
        
        assert version is not None, "Version parsing failed"
        assert commit is not None, "Commit parsing failed"

        return version, commit

    powershell_cmd = get_version_from_dll

    mobile_sdks = {
        "net_android": "Mono.Android.dll",
        "net_ios": "Microsoft.iOS.dll",
        "net_maui": "Microsoft.Maui.dll",
        "runtime": "System.Runtime.dll"
    }
    results = dict[str, str]()

    for sdk, dll_name in mobile_sdks.items():
        dll_path = os.path.join(dll_folder_path, dll_name)
        print(f"Getting version from {dll_path}")
        result = powershell_cmd(dll_path)
        
        if "Cannot find path" in result:
            getLogger().warning(f"Cannot find {dll_name} in {dll_folder_path}. Skipping version extraction.")
            continue

        version, commit = parse_version_output(result)
        results[f"{sdk}_version"] = version
        if sdk == "runtime":
            results["PERFLAB_DATA_dotnet_commit_hash"] = commit
            try:
                results["PERFLAB_DATA_runtime_commit_hash"] = get_runtime_commit_hash(commit)
            except (URLError, TimeoutError, json.JSONDecodeError, UnicodeDecodeError, ValueError) as error:
                getLogger().warning(f"Unable to resolve runtime commit from dotnet/dotnet commit {commit}: {error}")
        else:
            results[f"PERFLAB_DATA_{sdk}_commit_hash"] = commit

    # Add datetime of the SDK installation to the results
    now = datetime.utcnow()
    results["PERFLAB_DATA_sdk_install_datetime"] = now.strftime("%Y-%m-%dT%H:%M:%SZ")

    return results
