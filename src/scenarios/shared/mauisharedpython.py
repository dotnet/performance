import json
import subprocess
import os
import requests
import urllib.request
import xml.etree.ElementTree as ET
from shared.precommands import PreCommands
from collections import namedtuple

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
        # Generate and use rollback based on Version.Details.xml
        # Generate the list of versions starts to get and the names to save them as in the rollback.
        rollback_name_to_xml_name_mappings: dict[str, str] = {}
        rollback_dict: dict[str, str] = {}
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.android"] = "Microsoft.Android.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.ios"] = "Microsoft.iOS.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.maccatalyst"] = "Microsoft.MacCatalyst.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.macos"] = "Microsoft.macOS.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.maui"] = "Microsoft.Android.Sdk" # TODO
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.tvos"] = "Microsoft.tvOS.Sdk"
        rollback_name_to_xml_name_mappings[f"microsoft.net.sdk.mono.toolchain.{target_framework_wo_platform}"] = "Microsoft.NETCore.App.Ref"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.mono.toolchain.current"] = "Microsoft.NETCore.App.Ref"
        rollback_name_to_xml_name_mappings[f"microsoft.net.sdk.mono.emscripten.{target_framework_wo_platform}"] = "Microsoft.NET.Workload.Emscripten.Current"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.mono.emscripten.current"] = "Microsoft.NET.Workload.Emscripten.Current"

        root = ET.fromstring(urllib.request.urlopen(f"https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/eng/Version.Details.xml").read())
        dependencies = root.findall(".//Dependency[@Name]")
        for rollback_name, xml_name in rollback_name_to_xml_name_mappings.items():
            for dependency in dependencies:
                if dependency.get("Name").startswith(xml_name): # type: ignore, we know Name is present
                    rollback_dict[rollback_name] = dependency.get("Version", "ERROR: Failed to get version")
                    break
            if rollback_dict[rollback_name] == "ERROR: Failed to get version":
                raise ValueError(f"Unable to find {rollback_name} with proper version in the provided xml file")

        json_output = json.dumps(rollback_dict, indent=4)
        with open(f"rollback_{target_framework_wo_platform}.json", "w", encoding="utf-8") as f:
            f.write(json_output)
        workload_install_args += ['--from-rollback-file', f'rollback_{target_framework_wo_platform}.json']

    precommands.install_workload('maui', workload_install_args) 
