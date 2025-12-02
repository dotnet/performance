import json
import os
import xml.etree.ElementTree as ET
import re
import urllib.request
from performance.common import get_repo_root_path
from shared.precommands import PreCommands
from logging import getLogger

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
        match = re.search(r'^\d+\.\d+\.\d+(\-(preview|rc|alpha).\d+)?', full_band_version_holder)
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
            if dependency.attrib['Name'].startswith(xml_name):
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

    # Use the repo's NuGet.config which has the darc feeds containing the pinned package versions
    repo_nuget_config = os.path.join(get_repo_root_path(), "NuGet.config")

    workload_install_args = ['--configfile', repo_nuget_config, '--skip-sign-check']
    if int(target_framework_wo_platform.split('.')[0][3:]) > 8: # Use the rollback file for versions greater than 8 (should be set to only run for versions where we also use a specific dotnet version from the yml)
        rollback_dict = generate_maui_rollback_dict()
        dump_dict_to_json_file(rollback_dict, f"rollback_{target_framework_wo_platform}.json")
        workload_install_args += ['--from-rollback-file', f'rollback_{target_framework_wo_platform}.json']

    precommands.install_workload('maui', workload_install_args)

def extract_latest_dotnet_feed_from_nuget_config(path: str, offset: int = 0) -> str:
    '''
        Extract the latest dotnet feed from the NuGet.config file.
        The latest feed is the one that has the highest version number.
        Supports only single number versioning (dotnet9, dotnet10, etc.)
        
        Args:
            path: Path to the NuGet.config file
            offset: Offset from the latest version (0 = latest, 1 = second latest, etc.)
    '''
    tree = ET.parse(path)
    root = tree.getroot()

    # Find the <packageSources> element
    package_sources = root.find(".//packageSources")
    if package_sources is None:
        raise ValueError("No <packageSources> element found in NuGet.config")

    # Extract all <add> elements with keys starting with "dotnet" followed by a number
    dotnet_feeds = dict[int, str]()
    for add_element in package_sources.findall("add"):
        key = add_element.get("key")
        value = add_element.get("value")
        if key and value:
            match = re.match(r"dotnet(\d+)$", key)
            if match:
                version = int(match.group(1))
                dotnet_feeds[version] = value

    # Find the requested version based on offset
    if not dotnet_feeds:
        raise ValueError("No dotnet feeds found in NuGet.config")

    sorted_versions = sorted(dotnet_feeds.keys(), reverse=True)
    
    if offset >= len(sorted_versions):
        raise ValueError(f"Offset {offset} is too large. Only {len(sorted_versions)} dotnet feeds available")
    
    target_version = sorted_versions[offset]
    target_feed = dotnet_feeds[target_version]

    return target_feed

def install_latest_maui(
        precommands: PreCommands, 
        feed=extract_latest_dotnet_feed_from_nuget_config(path=os.path.join(get_repo_root_path(), "NuGet.config"))
        ):
    '''
        Install the latest maui workload using the provided feed. 
        This function will create a rollback file and install the maui workload using that file.
    '''

    getLogger().info("########## Installing latest MAUI workload ##########")

    if precommands.has_workload:
        getLogger().info("Skipping maui installation due to --has-workload=true")
        return

    maui_rollback_dict: dict[str, str] = {
        "microsoft.net.sdk.android" : "",
        "microsoft.net.sdk.ios" : "",
        "microsoft.net.sdk.maccatalyst" : "",
        "microsoft.net.sdk.macos" : "",
        "microsoft.net.sdk.maui" : "",
        "microsoft.net.sdk.tvos" : ""
    }

    getLogger().info(f"Installing the latest maui workload from feed {feed}")

    # Get the latest published version of the maui workloads
    for workload in maui_rollback_dict.keys():
        getLogger().info(f"Processing workload: {workload}")
        try:
            packages = precommands.get_packages_for_sdk_from_feed(workload, feed)
        except Exception as e:
            getLogger().warning(f"Failed to get packages for {workload} from latest feed: {e}")
            getLogger().info("Trying second latest feed as fallback")
            fallback_feed = extract_latest_dotnet_feed_from_nuget_config(
                path=os.path.join(get_repo_root_path(), "NuGet.config"), 
                offset=1
            )
            getLogger().info(f"Using fallback feed: {fallback_feed}")
            packages = precommands.get_packages_for_sdk_from_feed(workload, fallback_feed)

        # Log all package IDs before filtering
        getLogger().debug(f"All package IDs for {workload}: {[pkg['id'] for pkg in packages]}")

        # Filter out packages that have ID that matches the pattern 'Microsoft.NET.Sdk.<workload>.Manifest-<version>'
        pattern = r'Microsoft\.NET\.Sdk\..*\.Manifest\-\d+\.\d+\.\d+(\-(preview|rc|alpha)\.\d+)?$'
        packages = [pkg for pkg in packages if re.match(pattern, pkg['id'])]
        getLogger().info(f"After manifest pattern filtering, found {len(packages)} packages for {workload}")
        getLogger().debug(f"Filtered package IDs for {workload}: {[pkg['id'] for pkg in packages]}")

        # Extract the .NET version from the package ID (Manifest-<version>($|-<preview|rc|alpha>.*))
        for package in packages:
            getLogger().debug(f"Processing package ID: {package['id']}")
            match = re.search(r'Manifest-(.+)$', package["id"])
            if match:
                sdk_version = match.group(1)
                package['sdk_version'] = sdk_version
                getLogger().debug(f"Extracted SDK version '{sdk_version}' from package {package['id']}")

                # Extract the .NET version from sdk_version (first integer)
                match = re.search(r'^\d+\.\d+', sdk_version)
                if match:
                    dotnet_version = match.group(0)
                    package['dotnet_version'] = dotnet_version
                    getLogger().debug(f"Extracted .NET version '{dotnet_version}' from SDK version '{sdk_version}'")
                else:
                    getLogger().error(f"Unable to find .NET version in SDK version '{sdk_version}' for package {package['id']}")
                    raise Exception("Unable to find .NET version in SDK version")
            else:
                getLogger().error(f"Unable to find .NET SDK version in package ID: {package['id']}")
                raise Exception("Unable to find .NET SDK version in package ID")
            
        # Filter out packages that have lower 'dotnet_version' than the rest of the packages
        # Sometimes feed can contain packages from previous release versions, so we need to filter them out
        dotnet_versions = [float(pkg['dotnet_version']) for pkg in packages]
        getLogger().debug(f"Found .NET versions for {workload}: {dotnet_versions}")
        highest_dotnet_version = max(dotnet_versions)
        getLogger().info(f"Highest .NET version for {workload}: {highest_dotnet_version}")
        packages_before_version_filter = len(packages)
        packages = [pkg for pkg in packages if float(pkg['dotnet_version']) == highest_dotnet_version]
        getLogger().info(f"After .NET version filtering for {workload}: {len(packages)} packages (was {packages_before_version_filter})")
        
        pkg_details = [f"{pkg['id']} (v{pkg['dotnet_version']})" for pkg in packages]
        getLogger().debug(f"Packages after .NET version filter: {pkg_details}")

        # Check if we have non-preview packages available and use them
        preview_pattern = r'\-(preview|rc|alpha)\.\d+$'
        non_preview_packages = [pkg for pkg in packages if not re.search(preview_pattern, pkg['id'])]
        getLogger().info(f"Found {len(non_preview_packages)} non-preview packages for {workload} out of {len(packages)} total")
        
        preview_packages = [pkg['id'] for pkg in packages if re.search(preview_pattern, pkg['id'])]
        getLogger().debug(f"Preview packages: {preview_packages}")
        getLogger().debug(f"Non-preview packages: {[pkg['id'] for pkg in non_preview_packages]}")
        
        if non_preview_packages:
            getLogger().info(f"Using non-preview packages for {workload}")
            packages = non_preview_packages
        else:
            getLogger().info(f"No non-preview packages available for {workload}, using all packages")

        # Sort the packages by 'sdk_version'
        before_sort = [f"{pkg['id']} (sdk_v{pkg['sdk_version']})" for pkg in packages]
        newline = '\n'
        getLogger().debug(f"Packages before sorting for {workload}: {newline.join(before_sort)}")
        packages.sort(key=lambda x: x['sdk_version'], reverse=True)
        after_sort = [f"{pkg['id']} (sdk_v{pkg['sdk_version']})" for pkg in packages]
        getLogger().debug(f"Packages after sorting for {workload}: {newline.join(after_sort)}")

        # Get the latest package
        if not packages:
            getLogger().error(f"No packages available for {workload} after filtering")
            raise Exception(f"No packages available for {workload} after filtering")
            
        latest_package = packages[0]

        getLogger().info(f"Latest package details for {workload}: ID={latest_package['id']}, Version={latest_package['latestVersion']}, SDK_Version={latest_package['sdk_version']}, .NET_Version={latest_package['dotnet_version']}")
        
        rollback_value = f"{latest_package['latestVersion']}/{latest_package['sdk_version']}"
        maui_rollback_dict[workload] = rollback_value
        getLogger().info(f"Set rollback for {workload}: {rollback_value}")

    # Create the rollback file
    getLogger().info(f"Final rollback dictionary: {maui_rollback_dict}")
    with open("rollback_maui.json", "w", encoding="utf-8") as f:
        f.write(json.dumps(maui_rollback_dict, indent=4))
    getLogger().info("Created rollback_maui.json file")

    # Install the workload using the rollback file
    getLogger().info("Installing maui workload with rollback file")
    precommands.install_workload('maui', ['--from-rollback-file', 'rollback_maui.json'])
    getLogger().info("########## Finished installing latest MAUI workload ##########")
