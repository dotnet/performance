'''
pre-command: Set up a MAUI Android app for deploy measurement.
Creates the template (without restore) and prepares the modified file for incremental deploy.
NuGet packages are restored on the Helix machine, not shipped in the payload.
'''
import json
import os
import re
import shutil
import sys
from performance.common import get_repo_root_path
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import extract_latest_dotnet_feed_from_nuget_config, MauiNuGetConfigContext
from shared.precommands import PreCommands
from test import EXENAME

def install_maui_android_workload(precommands: PreCommands):
    '''
    Install the maui-android workload (not the full 'maui' workload).
    The full 'maui' workload includes iOS/macOS/Windows components that aren't
    available on Linux. Since this scenario only needs Android, 'maui-android'
    is sufficient and works on both Windows and Linux.
    '''
    # Why this is complex: we can't simply run `dotnet workload install maui-android`
    # because that would install the latest public version, which may not match the
    # SDK version being tested. Instead, we resolve the exact workload manifest version
    # from the NuGet feed that matches our SDK, create a rollback file pinning that
    # version, and install using --from-rollback-file.
    logger.info("########## Installing maui-android workload ##########")

    if precommands.has_workload:
        logger.info("Skipping maui-android installation due to --has-workload=true")
        return

    feed = extract_latest_dotnet_feed_from_nuget_config(
        path=os.path.join(get_repo_root_path(), "NuGet.config")
    )
    logger.info(f"Installing the latest maui-android workload from feed {feed}")

    workload = "microsoft.net.sdk.android"
    try:
        packages = precommands.get_packages_for_sdk_from_feed(workload, feed)
    except Exception as e:
        logger.warning(f"Failed to get packages for {workload} from latest feed: {e}")
        logger.info("Trying second latest feed as fallback")
        fallback_feed = extract_latest_dotnet_feed_from_nuget_config(
            path=os.path.join(get_repo_root_path(), "NuGet.config"),
            offset=1
        )
        logger.info(f"Using fallback feed: {fallback_feed}")
        packages = precommands.get_packages_for_sdk_from_feed(workload, fallback_feed)

    # Filter to manifest packages only
    pattern = r'Microsoft\.NET\.Sdk\..*\.Manifest\-\d+\.\d+\.\d+(\-(preview|rc|alpha)\.\d+)?$'
    packages = [pkg for pkg in packages if re.match(pattern, pkg['id'])]
    logger.info(f"After manifest pattern filtering, found {len(packages)} packages for {workload}")

    # Extract SDK and .NET versions from package IDs
    for package in packages:
        match = re.search(r'Manifest-(.+)$', package["id"])
        if not match:
            raise Exception(f"Unable to find .NET SDK version in package ID: {package['id']}")
        sdk_version = match.group(1)
        package['sdk_version'] = sdk_version

        ver_match = re.search(r'^\d+\.\d+', sdk_version)
        if not ver_match:
            raise Exception(f"Unable to find .NET version in SDK version '{sdk_version}'")
        package['dotnet_version'] = ver_match.group(0)

    # Keep only packages targeting the highest .NET version
    dotnet_versions = [float(pkg['dotnet_version']) for pkg in packages]
    highest = max(dotnet_versions)
    packages = [pkg for pkg in packages if float(pkg['dotnet_version']) == highest]
    logger.info(f"After .NET version filtering for {workload}: {len(packages)} packages (highest={highest})")

    # Prefer non-preview packages
    preview_pattern = r'\-(preview|rc|alpha)\.\d+$'
    non_preview = [pkg for pkg in packages if not re.search(preview_pattern, pkg['id'])]
    if non_preview:
        packages = non_preview

    # Sort by SDK version descending and take the latest
    packages.sort(key=lambda x: x['sdk_version'], reverse=True)
    if not packages:
        raise Exception(f"No packages available for {workload} after filtering")

    latest = packages[0]
    logger.info(f"Latest package: ID={latest['id']}, Version={latest['latestVersion']}, SDK={latest['sdk_version']}")

    # Create rollback file with only the android workload
    rollback_value = f"{latest['latestVersion']}/{latest['sdk_version']}"
    rollback_dict = {workload: rollback_value}
    logger.info(f"Rollback dictionary: {rollback_dict}")
    with open("rollback_maui.json", "w", encoding="utf-8") as f:
        f.write(json.dumps(rollback_dict, indent=4))
    logger.info("Created rollback_maui.json file")

    # Install maui-android (not 'maui') — works on both Windows and Linux
    precommands.install_workload('maui-android', ['--from-rollback-file', 'rollback_maui.json'])
    logger.info("########## Finished installing maui-android workload ##########")

setup_loggers(True)
logger = getLogger(__name__)
logger.info("Starting pre-command for MAUI Android deploy measurement")

precommands = PreCommands()

with MauiNuGetConfigContext(precommands.framework):
    install_maui_android_workload(precommands)
    precommands.print_dotnet_info()

    # Create template without restoring packages — packages will be restored
    # on the Helix machine to avoid shipping ~1-2GB in the workitem payload.
    precommands.new(template='maui',
                    output_dir=const.APPDIR,
                    bin_dir=const.BINDIR,
                    exename=EXENAME,
                    working_directory=sys.path[0],
                    no_restore=True,
                    extra_args=['-sc'])

    # Copy the merged NuGet.config into the app directory. This file contains
    # MAUI NuGet feed URLs added by MauiNuGetConfigContext. The Helix machine
    # needs these feeds during restore, and we must copy before the context
    # manager restores the original NuGet.config.
    repo_root = os.path.normpath(os.path.join(sys.path[0], '..', '..', '..'))
    repo_nuget_config = os.path.join(repo_root, 'NuGet.config')
    app_nuget_config = os.path.join(const.APPDIR, 'NuGet.config')
    shutil.copy2(repo_nuget_config, app_nuget_config)
    logger.info(f"Copied merged NuGet.config from {repo_nuget_config} to {app_nuget_config}")

    # Inject properties into the csproj so they apply to every command that
    # targets this project (restore, build, install).
    csproj_path = os.path.join(const.APPDIR, f'{EXENAME}.csproj')
    with open(csproj_path, 'r') as f:
        csproj_content = f.read()

    logger.info(f"Original .csproj content:\n{csproj_content}")

    injected_props = {
        # Preview SDKs may lack prune-package-data files, causing NETSDK1226.
        'AllowMissingPrunePackageData': 'true',
        # The perf repo globally disables the Roslyn compiler server to avoid
        # BenchmarkDotNet file-locking issues. Re-enable it here to match real
        # MAUI developer inner loop experience.
        'UseSharedCompilation': 'true',
    }
    csproj_modified = csproj_content
    if '</PropertyGroup>' not in csproj_modified:
        raise Exception(
            f"Cannot inject properties into {csproj_path}: "
            f"no <PropertyGroup> found in the generated template."
        )
    for prop_name, prop_value in injected_props.items():
        if prop_name not in csproj_modified:
            csproj_modified = csproj_modified.replace(
                '</PropertyGroup>',
                f'    <{prop_name}>{prop_value}</{prop_name}>\n  </PropertyGroup>',
                1  # only the first PropertyGroup
            )

    with open(csproj_path, 'w') as f:
        f.write(csproj_modified)

    logger.info(f"Updated {csproj_path} with injected properties")
    logger.info(f"Modified .csproj content:\n{csproj_modified}")

    # Create modified source files in src/ for the incremental deploy simulation.
    # The runner toggles between original and modified versions each iteration,
    # exercising both the C# compiler (Csc) and XAML compiler (XamlC) paths.
    src_dir = os.path.join(sys.path[0], const.SRCDIR)
    os.makedirs(src_dir, exist_ok=True)

    # --- Modified MainPage.xaml.cs: add a debug line in the constructor ---
    cs_original = os.path.join(const.APPDIR, 'Pages', 'MainPage.xaml.cs')
    cs_modified = os.path.join(src_dir, 'MainPage.xaml.cs')

    with open(cs_original, 'r') as f:
        cs_content = f.read()

    cs_modified_content = cs_content.replace(
        'InitializeComponent();',
        'InitializeComponent();\n\t\tSystem.Diagnostics.Debug.WriteLine("incremental-touch");'
    )
    if cs_modified_content == cs_content:
        raise Exception("Could not find 'InitializeComponent();' in %s — template may have changed" % cs_original)

    with open(cs_modified, 'w') as f:
        f.write(cs_modified_content)
    logger.info(f"Modified MainPage.xaml.cs written to {cs_modified}")

    # --- Modified MainPage.xaml: change a label's text ---
    xaml_original = os.path.join(const.APPDIR, 'Pages', 'MainPage.xaml')
    xaml_modified = os.path.join(src_dir, 'MainPage.xaml')

    with open(xaml_original, 'r') as f:
        xaml_content = f.read()

    xaml_modified_content = xaml_content.replace(
        'Text="Task Categories"',
        'Text="Task Categories (updated)"'
    )
    if xaml_modified_content == xaml_content:
        raise Exception("Could not find 'Text=\"Task Categories\"' in %s — template may have changed" % xaml_original)

    with open(xaml_modified, 'w') as f:
        f.write(xaml_modified_content)
    logger.info(f"Modified MainPage.xaml written to {xaml_modified}")
