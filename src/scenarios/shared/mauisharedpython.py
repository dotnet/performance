import json
import os
import xml.etree.ElementTree as ET
import re
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
        # Generate and use rollback based on Version.Details.xml
        # Generate the list of versions starts to get and the names to save them as in the rollback.
        rollback_name_to_xml_name_mappings: dict[str, str] = {}
        rollback_name_to_verion_property_mappings: dict[str, str] = {}
        rollback_dict: dict[str, str] = {}

        rollback_name_to_xml_name_mappings["microsoft.net.sdk.android"] = "Microsoft.Android.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.ios"] = "Microsoft.iOS.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.maccatalyst"] = "Microsoft.MacCatalyst.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.macos"] = "Microsoft.macOS.Sdk"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.tvos"] = "Microsoft.tvOS.Sdk"
        rollback_name_to_xml_name_mappings[f"microsoft.net.sdk.mono.toolchain.{target_framework_wo_platform}"] = "Microsoft.NETCore.App.Ref"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.mono.toolchain.current"] = "Microsoft.NETCore.App.Ref"
        rollback_name_to_xml_name_mappings[f"microsoft.net.sdk.mono.emscripten.{target_framework_wo_platform}"] = "Microsoft.NET.Workload.Emscripten.Current"
        rollback_name_to_xml_name_mappings["microsoft.net.sdk.mono.emscripten.current"] = "Microsoft.NET.Workload.Emscripten.Current"
        
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.android"] = "DotNetAndroidManifestVersionBand"
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.ios"] = "DotNetMaciOSManifestVersionBand"
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.maccatalyst"] = "DotNetMaciOSManifestVersionBand"
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.maui"] = "DotNetMauiManifestVersionBand"
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.macos"] = "DotNetMaciOSManifestVersionBand"
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.tvos"] = "DotNetMaciOSManifestVersionBand"
        rollback_name_to_verion_property_mappings[f"microsoft.net.sdk.mono.toolchain.{target_framework_wo_platform}"] = "DotNetMonoManifestVersionBand"
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.mono.toolchain.current"] = "DotNetMonoManifestVersionBand"
        rollback_name_to_verion_property_mappings[f"microsoft.net.sdk.mono.emscripten.{target_framework_wo_platform}"] = "DotNetEmscriptenManifestVersionBand"
        rollback_name_to_verion_property_mappings["microsoft.net.sdk.mono.emscripten.current"] = "DotNetEmscriptenManifestVersionBand"

        # Get the available versions from the Version.Details.xml file (Everything in rollback_name_to_xml_name_mappings)
        root = ET.fromstring(requests.get(f"https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/eng/Version.Details.xml", timeout=10).content)
        dependencies = root.findall(".//Dependency[@Name]")
        for rollback_name, xml_name in rollback_name_to_xml_name_mappings.items():
            for dependency in dependencies:
                if dependency.get("Name").startswith(xml_name): # type: ignore we know Name is present
                    rollback_dict[rollback_name] = dependency.get("Version", "ERROR: Failed to get version")
                    break
            if rollback_dict[rollback_name] == "ERROR: Failed to get version":
                raise ValueError(f"Unable to find {rollback_name} with proper version in the provided xml file")

        # Separately handle the maui workload version as it is not on the Version.Details.xml page
        status_json = requests.get(f"https://api.github.com/repos/dotnet/maui/commits/{target_framework_wo_platform}/status", timeout=10).json()
        maui_version = "ERROR: Failed to get version"
        for status in status_json.get("statuses", []):
            if status.get("context") == "MAUI-Release":
                description = status.get("description", "")
                # Find the version substring before "+azdo"
                maui_version = description.split("+azdo")[0].strip("# ")

        if maui_version == "ERROR: Failed to get version":
            raise ValueError("Unable to find MAUI version in the provided status json")
        rollback_dict["microsoft.net.sdk.maui"] = maui_version

        # Get the band version from the Version.props xml file (Everything in rollback_name_to_verion_property_mappings)
        root = ET.fromstring(requests.get(f"https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/eng/Versions.props", timeout=10).content)
        general_version_obj = root.find(".//PropertyGroup/VSToolsNetCoreSDKResolverPackageVersion")
        if general_version_obj is not None and general_version_obj.text is not None:
            general_version = general_version_obj.text
            match = re.match(r'^\d+\.\d+\.\d+\-(preview|rc|alpha).\d+', general_version)
            if match:
                general_version = match.group(0)
            else:
                raise ValueError("Unable to find general version in the provided xml file")
        else:
            raise ValueError("Unable to find general version in the provided xml file")
        
        # Get the band version for each of the workloads and append it to the rollback_dict entries
        for rollback_name, version_property in rollback_name_to_verion_property_mappings.items():
            for props_version_property in root.findall(".//PropertyGroup/*"):
                if props_version_property.tag == version_property:
                    if props_version_property.text is None or props_version_property.text == "":
                        rollback_dict[rollback_name] = rollback_dict[rollback_name] + "/ERROR: Failed to get band version"
                    elif props_version_property.text != "$(DotNetVersionBand)":
                        rollback_dict[rollback_name] = rollback_dict[rollback_name] + "/" + props_version_property.text
                    else:
                        rollback_dict[rollback_name] = rollback_dict[rollback_name] + "/" + general_version
                    break
            print(rollback_dict)
            if rollback_dict[rollback_name].split("/")[1] == "ERROR: Failed to get band version":
                raise ValueError(f"Unable to find {rollback_name} with proper version in the provided xml file")
            
        json_output = json.dumps(rollback_dict, indent=4)
        with open(f"rollback_{target_framework_wo_platform}.json", "w", encoding="utf-8") as f:
            f.write(json_output)
        workload_install_args += ['--from-rollback-file', f'rollback_{target_framework_wo_platform}.json']

    precommands.install_workload('maui', workload_install_args) 
