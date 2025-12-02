import json
import os
import shutil
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

def sync_maui_version_details(target_framework: str = "net10.0"):
    '''
    Downloads MAUI's Version.Details.xml and merges MAUI workload dependencies into the repo's version.
    This keeps the repo's Version.Details.xml automatically up-to-date with MAUI's latest versions.
    
    Args:
        target_framework: Target framework to determine which MAUI branch to use (e.g., "net10.0")
    '''
    # Extract base framework version
    if '-' in target_framework:
        target_framework_wo_platform = target_framework.split('-')[0]
    else:
        target_framework_wo_platform = target_framework
    
    repo_version_details_path = os.path.join(get_repo_root_path(), "eng", "Version.Details.xml")
    
    # Download MAUI's Version.Details.xml
    maui_version_url = f'https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/eng/Version.Details.xml'
    getLogger().info(f"Downloading MAUI Version.Details.xml from {maui_version_url}")
    
    try:
        with urllib.request.urlopen(maui_version_url) as response:
            maui_version_xml_content = response.read().decode('utf-8')
    except Exception as e:
        getLogger().error(f"Failed to download MAUI Version.Details.xml: {e}")
        raise
    
    # Parse both XML files
    maui_root = ET.fromstring(maui_version_xml_content)
    repo_tree = ET.parse(repo_version_details_path)
    repo_root = repo_tree.getroot()
    
    # MAUI workload dependency names to sync
    maui_dependency_patterns = [
        "Microsoft.Android.Sdk",
        "Microsoft.iOS.Sdk",
        "Microsoft.MacCatalyst.Sdk",
        "Microsoft.macOS.Sdk",
        "Microsoft.tvOS.Sdk",
        "Microsoft.Maui.Controls",
        "Microsoft.NETCore.App.Ref",
        "Microsoft.NET.Sdk",
        "Microsoft.NET.Workload.Emscripten"
    ]
    
    # Get ToolsetDependencies section from repo
    repo_toolset = repo_root.find(".//ToolsetDependencies")
    if repo_toolset is None:
        getLogger().error("No ToolsetDependencies section found in repo Version.Details.xml")
        raise ValueError("Invalid Version.Details.xml structure")
    
    # Track what we updated
    updated_count = 0
    added_count = 0
    
    # Get all dependencies from MAUI's Version.Details.xml
    maui_dependencies = maui_root.findall(".//Dependency[@Name]")
    
    for maui_dep in maui_dependencies:
        dep_name = maui_dep.get("Name")
        
        # Check if this is a MAUI workload dependency we care about
        if not any(pattern in dep_name for pattern in maui_dependency_patterns):
            continue
        
        # Skip previous version dependencies (net9.0, etc.) - only sync current target framework versions
        if f"net{int(target_framework_wo_platform[3:]) - 1}" in dep_name:
            getLogger().debug(f"Skipping previous version dependency: {dep_name}")
            continue
        
        # Find if this dependency already exists in repo
        existing_dep = repo_toolset.find(f".//Dependency[@Name='{dep_name}']")
        
        if existing_dep is not None:
            # Update existing dependency
            old_version = existing_dep.get("Version")
            new_version = maui_dep.get("Version")
            
            if old_version != new_version:
                existing_dep.set("Version", new_version)
                
                # Update URI and Sha if present
                uri_elem = existing_dep.find("Uri")
                maui_uri_elem = maui_dep.find("Uri")
                if uri_elem is not None and maui_uri_elem is not None:
                    uri_elem.text = maui_uri_elem.text
                    
                sha_elem = existing_dep.find("Sha")
                maui_sha_elem = maui_dep.find("Sha")
                if sha_elem is not None and maui_sha_elem is not None:
                    sha_elem.text = maui_sha_elem.text
                
                updated_count += 1
                getLogger().info(f"Updated {dep_name}: {old_version} -> {new_version}")
        else:
            # Add new dependency
            # Insert before the "Previous .NET" comment or at the end
            prev_version_comment_index = None
            for i, child in enumerate(repo_toolset):
                if child.tag is ET.Comment and "Previous .NET" in child.text:
                    prev_version_comment_index = i
                    break
            
            if prev_version_comment_index is not None:
                repo_toolset.insert(prev_version_comment_index, maui_dep)
            else:
                repo_toolset.append(maui_dep)
            
            added_count += 1
            getLogger().info(f"Added new dependency: {dep_name} (Version: {maui_dep.get('Version')})")
    
    # Write back to file with proper formatting
    ET.indent(repo_tree, space="  ")
    repo_tree.write(repo_version_details_path, encoding="utf-8", xml_declaration=True)
    
    getLogger().info(f"MAUI Version.Details.xml sync complete: {updated_count} updated, {added_count} added")
    return updated_count + added_count > 0

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

    # Automatically sync MAUI dependencies from upstream Version.Details.xml
    getLogger().info("Syncing MAUI Version.Details.xml dependencies...")
    try:
        sync_maui_version_details(target_framework_wo_platform)
    except Exception as e:
        getLogger().warning(f"Failed to sync MAUI Version.Details.xml: {e}. Continuing with existing versions.")

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

def download_maui_nuget_config(target_framework: str = "net10.0", output_filename: str = "MauiNuGet.config") -> str:
    '''
        Download MAUI's NuGet.config from the appropriate branch.
        Returns the path to the downloaded config file.
        
        Args:
            target_framework: Target framework to determine which branch to use (e.g., "net10.0")
            output_filename: Name of the file to save the downloaded config
    '''
    # Extract base framework version (e.g., "net10.0" from "net10.0-android")
    if '-' in target_framework:
        target_framework_wo_platform = target_framework.split('-')[0]
    else:
        target_framework_wo_platform = target_framework
    
    url = f'https://raw.githubusercontent.com/dotnet/maui/{target_framework_wo_platform}/NuGet.config'
    getLogger().info(f"Downloading MAUI NuGet.config from {url}")
    
    try:
        with open(output_filename, "wb") as f:
            with urllib.request.urlopen(url) as response:
                f.write(response.read())
        getLogger().info(f"Successfully downloaded MAUI NuGet.config to {output_filename}")
        return os.path.abspath(output_filename)
    except Exception as e:
        getLogger().error(f"Failed to download MAUI NuGet.config: {e}")
        raise

class MauiNuGetConfigContext:
    '''
    Context manager that temporarily merges MAUI's package sources into the repo's NuGet.config.
    This is necessary because dotnet new doesn't support --configfile parameter.
    '''
    def __init__(self, target_framework: str):
        self.target_framework = target_framework
        self.repo_nuget_config = os.path.join(get_repo_root_path(), "NuGet.config")
        self.backup_path = self.repo_nuget_config + ".maui_backup"
        self.maui_config_path = None
        
    def __enter__(self):
        getLogger().info("Setting up MAUI NuGet.config merge...")
        
        # Download MAUI's NuGet.config
        self.maui_config_path = download_maui_nuget_config(self.target_framework, "MauiNuGet.config")
        
        # Backup the repo's NuGet.config
        shutil.copy2(self.repo_nuget_config, self.backup_path)
        getLogger().info(f"Backed up repo NuGet.config to {self.backup_path}")
        
        # Parse both configs
        repo_tree = ET.parse(self.repo_nuget_config)
        repo_root = repo_tree.getroot()
        maui_tree = ET.parse(self.maui_config_path)
        maui_root = maui_tree.getroot()
        
        # Get package sources from both
        repo_sources = repo_root.find(".//packageSources")
        maui_sources = maui_root.find(".//packageSources")
        
        if repo_sources is None or maui_sources is None:
            getLogger().error("Could not find packageSources in NuGet.config files")
            raise ValueError("Invalid NuGet.config structure")
        
        # Get existing source keys to avoid duplicates
        existing_keys = {add_elem.get("key") for add_elem in repo_sources.findall("add") if add_elem.get("key")}
        
        # Add MAUI sources that don't exist in repo config
        # Filter out placeholder sources that MAUI uses for their build system
        placeholder_patterns = ["PLACEHOLDER", "local", "nuget-only"]
        added_count = 0
        for add_elem in maui_sources.findall("add"):
            key = add_elem.get("key")
            value = add_elem.get("value")
            
            # Skip if key already exists in repo config
            if not key or key in existing_keys:
                continue
            
            # Skip placeholder sources (local, nuget-only, or any with PLACEHOLDER in value)
            if any(pattern.lower() in key.lower() for pattern in placeholder_patterns):
                getLogger().debug(f"Skipping placeholder source: {key}")
                continue
            if value and "PLACEHOLDER" in value:
                getLogger().debug(f"Skipping placeholder source with placeholder value: {key}")
                continue
            
            # Add valid source
            repo_sources.append(add_elem)
            added_count += 1
            getLogger().debug(f"Added package source: {key}")
        
        getLogger().info(f"Added {added_count} package sources from MAUI NuGet.config")
        
        # Write the merged config back
        repo_tree.write(self.repo_nuget_config, encoding="utf-8", xml_declaration=True)
        getLogger().info("Merged MAUI package sources into repo NuGet.config")
        
        return self
        
    def __exit__(self, exc_type, exc_val, exc_tb):
        getLogger().info("Restoring original NuGet.config...")
        
        # Restore the original NuGet.config
        if os.path.exists(self.backup_path):
            shutil.move(self.backup_path, self.repo_nuget_config)
            getLogger().info("Restored original NuGet.config")
        
        # Clean up the downloaded MAUI config
        if self.maui_config_path and os.path.exists(self.maui_config_path):
            os.remove(self.maui_config_path)
            getLogger().debug("Cleaned up temporary MAUI NuGet.config")
        
        return False  # Don't suppress exceptions

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
    
    # Automatically sync MAUI dependencies from upstream Version.Details.xml
    # This ensures our Version.Details.xml stays current even when using feed-based installation
    getLogger().info("Syncing MAUI Version.Details.xml dependencies...")
    try:
        sync_maui_version_details(precommands.framework)
    except Exception as e:
        getLogger().warning(f"Failed to sync MAUI Version.Details.xml: {e}. Continuing with feed-based installation.")

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
