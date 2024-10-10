import json
import os
import xml.etree.ElementTree as ET
import re
import urllib.request
from performance.common import get_repo_root_path
from shared.precommands import PreCommands

# Remove the aab files as we don't need them, this saves space in the correlation payload
def remove_aab_files(output_dir="."):
    file_list = os.listdir(output_dir)
    for file in file_list:
        if file.endswith(".aab"):
            os.remove(os.path.join(output_dir, file))  

def generate_maui_rollback_dict():
    # Generate and use rollback based on Version.Details.xml
    # Generate the list of versions starts to get and the names to save them as in the rollback.
    # These mapping values were taken from the previously generated rollback files for the maui workload. There should be at least one entry for each
    # of the Maui Workload dependencies in the /eng/Version.Details.xml file, aside from Microsoft.NET.Sdk.
    # If there are errors in the future, reach out to the maui team.
    rollback_name_to_xml_name_mappings: dict[str, str] = {
        "microsoft.net.sdk.android" : "Microsoft.Android.Sdk",
        "microsoft.net.sdk.ios" : "Microsoft.iOS.Sdk",
        "microsoft.net.sdk.maccatalyst" : "Microsoft.MacCatalyst.Sdk",
        "microsoft.net.sdk.macos" : "Microsoft.macOS.Sdk",
        "microsoft.net.sdk.maui" : "Microsoft.Maui.Controls",
        "microsoft.net.sdk.tvos" : "Microsoft.tvOS.Sdk",
        "microsoft.net.sdk.mono.toolchain.current" : "Microsoft.NETCore.App.Ref",
        "microsoft.net.sdk.mono.emscripten.current" : "Microsoft.NET.Workload.Emscripten.Current"
    }
    rollback_dict: dict[str, str] = {}

    # Load in the Version.Details.xml file
    with open(os.path.join(get_repo_root_path(), "eng", "Version.Details.xml"), encoding="utf-8") as f:
        version_details_xml = f.read()
        root = ET.fromstring(version_details_xml)

    # Get the General Band version from the Version.Details.xml file sdk version
    general_version_obj = root.find(".//Dependency[@Name='Microsoft.NET.Sdk']")
    if general_version_obj is not None:
        full_band_version_holder = general_version_obj.get("Version")
        if full_band_version_holder is None:
            raise ValueError("Unable to find Microsoft.NET.Sdk with proper version in Version.Details.xml")
        match = re.search(r'^\d+\.\d+\.\d+\-(preview|rc|alpha).\d+', full_band_version_holder)
        if match:
            default_band_version = match.group(0)
        else:
            raise ValueError("Unable to find general version in Version.Details.xml")
    else:
        raise ValueError("Unable to find general version in Version.Details.xml")

    # Get the available versions from the Version.Details.xml file
    dependencies = root.findall(".//Dependency[@Name]")
    for rollback_name, xml_name in rollback_name_to_xml_name_mappings.items():
        for dependency in dependencies:
            if dependency.get("Name").startswith(xml_name): # type: ignore we know Name is present
                workload_version = dependency.get("Version")
                if workload_version is None:
                    raise ValueError(f"Unable to find {xml_name} with proper version in the provided xml file")

                # Use the band version based on what the maui upstream currently has. This is necessary if they hardcode the version.
                band_name_match_string = rf"^\s*Mapping_{xml_name}:(\S*)"
                band_version_mapping = re.search(band_name_match_string, version_details_xml, flags=re.MULTILINE)
                if band_version_mapping is None:
                    raise ValueError(f"Unable to find band version mapping for match {band_name_match_string} in Version.Details.xml")
                if band_version_mapping.group(1) == "default":
                    band_version = default_band_version
                else:
                    band_version = band_version_mapping.group(1)
                rollback_dict[rollback_name] = f"{workload_version}/{band_version}"
                break
        if rollback_name not in rollback_dict:
            raise ValueError(f"Unable to find {rollback_name} with proper version in Version.Details.xml")
    return rollback_dict

def dump_dict_to_json_file(dump_dict: dict[str, str], file_name: str):
    json_output = json.dumps(dump_dict, indent=4)
    with open(file_name, "w", encoding="utf-8") as f:
        f.write(json_output)

def install_versioned_maui(precommands: PreCommands):
    target_framework_wo_platform = precommands.framework.split('-')[0]

    # Download what we need
    with open("MauiNuGet.config", "wb") as f:
        with urllib.request.urlopen(f'https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/NuGet.config') as response:
            f.write(response.read())

    workload_install_args = ['--configfile', 'MauiNuGet.config', '--skip-sign-check']
    if int(target_framework_wo_platform.split('.')[0][3:]) > 8: # Use the rollback file for versions greater than 8 (should be set to only run for versions where we also use a specific dotnet version from the yml)
        rollback_dict = generate_maui_rollback_dict()
        dump_dict_to_json_file(rollback_dict, f"rollback_{target_framework_wo_platform}.json")
        workload_install_args += ['--from-rollback-file', f'rollback_{target_framework_wo_platform}.json']

    precommands.install_workload('maui', workload_install_args) 
