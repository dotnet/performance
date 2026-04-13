'''
pre-command: Set up a MAUI iOS app for deploy measurement.
Creates the template (without restore) and prepares the modified file for incremental deploy.
NuGet packages are restored on the Helix machine, not shipped in the payload.
'''
import glob
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import urllib.request
import zipfile
from performance.common import get_repo_root_path
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import extract_latest_dotnet_feed_from_nuget_config, MauiNuGetConfigContext
from shared.precommands import PreCommands
from test import EXENAME

def install_maui_ios_workload(precommands: PreCommands):
    '''
    Install the maui-ios workload (not the full 'maui' workload).
    The full 'maui' workload includes Android/Windows components that aren't
    needed for this scenario. Since this scenario only needs iOS, 'maui-ios'
    is sufficient and much smaller.
    '''
    logger.info("########## Installing maui-ios workload ##########")

    if precommands.has_workload:
        logger.info("Skipping maui-ios installation due to --has-workload=true")
        return

    workload = "microsoft.net.sdk.ios"

    # Resolve the latest iOS workload version dynamically from NuGet feeds.
    # This follows the same pattern as install_latest_maui() in mauisharedpython.py.
    feed = extract_latest_dotnet_feed_from_nuget_config(
        path=os.path.join(get_repo_root_path(), "NuGet.config")
    )
    logger.info(f"Installing the latest iOS workload from feed {feed}")

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
        feed = fallback_feed
        packages = precommands.get_packages_for_sdk_from_feed(workload, feed)

    # Filter to Manifest packages only
    pattern = r'Microsoft\.NET\.Sdk\..*\.Manifest\-\d+\.\d+\.\d+(\-(preview|rc|alpha)\.\d+)?$'
    packages = [pkg for pkg in packages if re.match(pattern, pkg['id'])]
    logger.info(f"After manifest pattern filtering, found {len(packages)} packages for {workload}")

    # Extract SDK version and .NET version from each package ID
    for package in packages:
        match = re.search(r'Manifest-(.+)$', package["id"])
        if match:
            sdk_version = match.group(1)
            package['sdk_version'] = sdk_version

            match = re.search(r'^\d+\.\d+', sdk_version)
            if match:
                package['dotnet_version'] = match.group(0)
            else:
                raise Exception(f"Unable to find .NET version in SDK version '{sdk_version}'")
        else:
            raise Exception(f"Unable to find .NET SDK version in package ID: {package['id']}")

    # Keep only packages from the highest .NET version (feed may contain older releases)
    dotnet_versions = [float(pkg['dotnet_version']) for pkg in packages]
    highest_dotnet_version = max(dotnet_versions)
    logger.info(f"Highest .NET version for {workload}: {highest_dotnet_version}")
    packages = [pkg for pkg in packages if float(pkg['dotnet_version']) == highest_dotnet_version]

    # Prefer non-preview packages; fall back to all if none available
    preview_pattern = r'\-(preview|rc|alpha)\.\d+$'
    non_preview_packages = [pkg for pkg in packages if not re.search(preview_pattern, pkg['id'])]
    logger.info(f"Found {len(non_preview_packages)} non-preview packages for {workload} out of {len(packages)} total")
    if non_preview_packages:
        packages = non_preview_packages
    else:
        logger.info(f"No non-preview packages available for {workload}, using all packages")

    # Sort by sdk_version descending and pick the latest
    packages.sort(key=lambda x: x['sdk_version'], reverse=True)
    if not packages:
        raise Exception(f"No packages available for {workload} after filtering")
    latest = packages[0]
    logger.info(
        f"Latest package for {workload}: ID={latest['id']}, "
        f"Version={latest['latestVersion']}, SDK_Version={latest['sdk_version']}, "
        f".NET_Version={latest['dotnet_version']}"
    )

    # Create rollback file with only the iOS workload
    rollback_value = f"{latest['latestVersion']}/{latest['sdk_version']}"
    rollback_dict = {workload: rollback_value}
    logger.info(f"Rollback dictionary: {rollback_dict}")
    with open("rollback_maui.json", "w", encoding="utf-8") as f:
        f.write(json.dumps(rollback_dict, indent=4))
    logger.info("Created rollback_maui.json file")

    # Try the standard --from-rollback-file install first. This works when all
    # upstream packs (including cross-targeting packs) are published on NuGet feeds.
    logger.info(
        "Installing maui-ios workload from rollback file (nightly). "
        "Failure here triggers the manifest-patching fallback."
    )
    try:
        precommands.install_workload('maui-ios', ['--from-rollback-file', 'rollback_maui.json'])
        logger.info("########## Finished installing maui-ios workload ##########")
        return
    except subprocess.CalledProcessError as e:
        logger.warning(
            f"Rollback-file install failed: {e}. "
            "This typically happens when the iOS workload manifest (e.g. v26.4) "
            "declares net10.0 cross-targeting packs that don't exist on any NuGet feed. "
            "Falling back to manifest-patching workaround."
        )

    # ── Manifest-patching fallback ──
    # The iOS workload manifest can reference net10.0 cross-targeting packs that
    # haven't been published yet (upstream coherency issue). We work around this
    # by downloading the manifest nupkg, patching out net10.0 entries, placing the
    # patched manifest on disk, and installing with --skip-manifest-update.
    logger.info("########## Starting manifest-patching fallback ##########")

    package_id = latest['id']
    package_version = latest['latestVersion']
    sdk_band = latest['sdk_version']

    # Step 1: Resolve PackageBaseAddress from the NuGet v3 service index
    logger.info(f"Fetching NuGet v3 service index from {feed}")
    with urllib.request.urlopen(feed, timeout=60) as resp:
        service_index = json.loads(resp.read().decode('utf-8'))

    package_base_url = None
    for resource in service_index.get('resources', []):
        if 'PackageBaseAddress' in resource.get('@type', ''):
            package_base_url = resource['@id'].rstrip('/')
            break
    if not package_base_url:
        raise Exception(
            f"Could not find PackageBaseAddress resource in NuGet v3 service index at {feed}"
        )
    logger.info(f"Resolved PackageBaseAddress: {package_base_url}")

    # Step 2: Download the manifest nupkg (NuGet flat container uses lowercase IDs/versions)
    nupkg_url = (
        f"{package_base_url}/{package_id.lower()}/{package_version.lower()}"
        f"/{package_id.lower()}.{package_version.lower()}.nupkg"
    )
    logger.info(f"Downloading manifest nupkg from {nupkg_url}")

    with tempfile.TemporaryDirectory() as tmpdir:
        nupkg_path = os.path.join(tmpdir, 'manifest.nupkg')
        # urlretrieve doesn't support timeout — use urlopen + manual write instead.
        # Explicit timeout prevents Helix jobs from hanging indefinitely on network issues.
        with urllib.request.urlopen(nupkg_url, timeout=120) as resp:
            with open(nupkg_path, 'wb') as dl_file:
                dl_file.write(resp.read())
        logger.info(f"Downloaded manifest nupkg to {nupkg_path}")

        # Step 3: Extract all files from the data/ directory inside the nupkg
        extracted_files = {}
        with zipfile.ZipFile(nupkg_path, 'r') as zf:
            for entry in zf.namelist():
                if entry.startswith('data/') and not entry.endswith('/'):
                    filename = os.path.basename(entry)
                    extracted_files[filename] = zf.read(entry)
                    logger.info(f"Extracted from nupkg: {entry} ({len(extracted_files[filename])} bytes)")

        if 'WorkloadManifest.json' not in extracted_files:
            raise Exception(
                f"WorkloadManifest.json not found in data/ directory of {nupkg_url}. "
                f"Found files: {list(extracted_files.keys())}"
            )

        # Step 4: Patch WorkloadManifest.json — remove entries containing "net10"
        raw_text = extracted_files['WorkloadManifest.json'].decode('utf-8')
        # The manifest uses trailing commas (invalid JSON). Strip them before parsing.
        # Python's json.loads() (especially 3.14+) rejects trailing commas.
        cleaned_text = re.sub(r',\s*([}\]])', r'\1', raw_text)
        manifest = json.loads(cleaned_text)

        removed_packs = []
        removed_extends = []
        removed_top_packs = []

        # Patch workloads.ios.packs — remove entries containing "net10" (case-insensitive).
        # The packs field can be a list (of pack name strings) or a dict (pack names as keys),
        # depending on the manifest version.
        ios_workload = manifest.get('workloads', {}).get('ios', {})
        if 'packs' in ios_workload:
            if isinstance(ios_workload['packs'], list):
                original_items = list(ios_workload['packs'])
                ios_workload['packs'] = [
                    item for item in ios_workload['packs']
                    if 'net10' not in item.lower()
                ]
                removed_packs = [item for item in original_items if 'net10' in item.lower()]
            else:
                original_keys = list(ios_workload['packs'].keys())
                for key in original_keys:
                    if 'net10' in key.lower():
                        del ios_workload['packs'][key]
                        removed_packs.append(key)

        # Patch workloads.ios.extends — remove list items containing "net10"
        if 'extends' in ios_workload:
            original_extends = list(ios_workload['extends'])
            ios_workload['extends'] = [
                item for item in ios_workload['extends']
                if 'net10' not in item.lower()
            ]
            removed_extends = [
                item for item in original_extends
                if 'net10' in item.lower()
            ]

        # Patch top-level packs — remove keys containing "net10".
        # This should always be a dict, but guard against unexpected list format.
        if 'packs' in manifest:
            if isinstance(manifest['packs'], dict):
                original_keys = list(manifest['packs'].keys())
                for key in original_keys:
                    if 'net10' in key.lower():
                        del manifest['packs'][key]
                        removed_top_packs.append(key)
            else:
                logger.warning(
                    f"Top-level 'packs' is {type(manifest['packs']).__name__}, expected dict — "
                    "skipping top-level packs patching"
                )

        logger.info(
            f"Patched WorkloadManifest.json — removed: "
            f"workloads.ios.packs={removed_packs}, "
            f"workloads.ios.extends={removed_extends}, "
            f"top-level packs={removed_top_packs}"
        )

        if not removed_packs and not removed_extends and not removed_top_packs:
            logger.warning(
                "Manifest patching removed NO entries — the install failure may not be "
                "caused by missing net10 packs. The subsequent install will likely fail "
                "with the same error."
            )

        # json.dumps produces valid JSON (no trailing commas) — the SDK accepts both.
        patched_json = json.dumps(manifest, indent=2)
        extracted_files['WorkloadManifest.json'] = patched_json.encode('utf-8')

        # Step 5: Determine DOTNET_ROOT
        dotnet_root = os.environ.get('DOTNET_ROOT')
        if not dotnet_root:
            dotnet_path = shutil.which('dotnet')
            if not dotnet_path:
                raise Exception("Cannot determine DOTNET_ROOT: not set and dotnet not found in PATH")
            dotnet_root = os.path.dirname(os.path.realpath(dotnet_path))
        logger.info(f"DOTNET_ROOT: {dotnet_root}")

        # Step 6: Place patched manifest files on disk
        # sdk_band comes from the manifest package ID (e.g., "11.0.100-preview.4")
        # package_version is the NuGet version (e.g., "26.4.11427-net11-p4")
        target_dir = os.path.join(
            dotnet_root, 'sdk-manifests', sdk_band,
            'microsoft.net.sdk.ios', package_version
        )
        os.makedirs(target_dir, exist_ok=True)
        logger.info(f"Writing patched manifest files to {target_dir}")

        for filename, content in extracted_files.items():
            target_path = os.path.join(target_dir, filename)
            with open(target_path, 'wb') as f:
                f.write(content)
            logger.info(f"Wrote {target_path} ({len(content)} bytes)")

    # Step 7: Install with --skip-manifest-update (manifest is already on disk)
    logger.info("Installing maui-ios workload with --skip-manifest-update (patched manifest)")
    precommands.install_workload('maui-ios', ['--skip-manifest-update'])
    logger.info("########## Finished installing maui-ios workload (manifest-patched) ##########")

def check_xcode_compatibility(framework: str):
    '''
    Best-effort check that the active Xcode version matches the iOS SDK's
    _RecommendedXcodeVersion. Logs a warning on mismatch — does not fail.
    The caller (pipeline or run-local.sh) handles the actual Xcode selection.
    '''
    try:
        result = subprocess.run(
            ['xcodebuild', '-version'],
            capture_output=True, text=True, timeout=10
        )
        if result.returncode != 0:
            logger.warning("Could not detect Xcode version (xcodebuild -version failed)")
            return

        # Parse "Xcode 26.4" → "26.4"
        xcode_line = result.stdout.strip().split('\n')[0]
        xcode_match = re.search(r'Xcode\s+(\d+\.\d+)', xcode_line)
        if not xcode_match:
            logger.warning(f"Could not parse Xcode version from: {xcode_line}")
            return
        active_xcode = xcode_match.group(1)

        # Extract TFM prefix: "net11.0-ios" → "net11.0"
        tfm_prefix = framework.split('-')[0] if '-' in framework else framework

        # Find the iOS SDK Versions.props file
        dotnet_path = shutil.which('dotnet')
        if not dotnet_path:
            logger.warning("Could not find dotnet in PATH — skipping Xcode version check")
            return
        dotnet_dir = os.path.dirname(os.path.realpath(dotnet_path))
        packs_dir = os.path.join(dotnet_dir, 'packs')

        # Search for Microsoft.iOS.Sdk.<TFM>_*/*/targets/Microsoft.iOS.Sdk.Versions.props
        search_pattern = os.path.join(
            packs_dir,
            f'Microsoft.iOS.Sdk.{tfm_prefix}_*',
            '*', 'targets', 'Microsoft.iOS.Sdk.Versions.props'
        )
        props_files = sorted(glob.glob(search_pattern))
        if not props_files:
            logger.warning(
                f"Could not find Microsoft.iOS.Sdk.Versions.props for {tfm_prefix} "
                f"in {packs_dir} — skipping Xcode version check"
            )
            return

        # Use the last (highest version) match
        versions_props = props_files[-1]
        logger.info(f"Found iOS SDK Versions.props: {versions_props}")

        # Parse _RecommendedXcodeVersion
        with open(versions_props, 'r') as f:
            content = f.read()
        rec_match = re.search(r'<_RecommendedXcodeVersion>([^<]+)</_RecommendedXcodeVersion>', content)
        if not rec_match:
            logger.warning(f"Could not find _RecommendedXcodeVersion in {versions_props}")
            return
        required_xcode = rec_match.group(1)

        active_major_minor = '.'.join(active_xcode.split('.')[:2])
        required_major_minor = '.'.join(required_xcode.split('.')[:2])

        if active_major_minor != required_major_minor:
            logger.warning(
                f"Xcode version MISMATCH: "
                f"active Xcode is {active_xcode} but iOS SDK requires {required_xcode}. "
                f"The build may fail with _ValidateXcodeVersion error. "
                f"Set XCODE_PATH or DEVELOPER_DIR to a compatible Xcode installation."
            )
        else:
            logger.info(
                f"Xcode version OK: active={active_xcode}, "
                f"required={required_xcode} (major.minor match: {active_major_minor})"
            )
    except Exception as e:
        logger.warning(f"Xcode compatibility check failed (non-fatal): {e}")

def strip_non_ios_tfms(csproj_path: str, framework: str):
    '''
    Strip non-iOS TargetFrameworks from the generated .csproj.
    The MAUI template (since .NET 10+) generates multiple conditional
    <TargetFrameworks> elements for android, ios, maccatalyst, and windows.
    We replace all of them with a single unconditional <TargetFrameworks>
    containing only the iOS TFM we want to build.
    Uses <TargetFrameworks\\b[^>]*> to match both unconditional and
    Condition="..." variants of the element.
    '''
    with open(csproj_path, 'r') as f:
        content = f.read()

    logger.info(f"Stripping non-iOS TFMs from {csproj_path}, keeping: {framework}")

    # Remove all existing <TargetFrameworks ...>...</TargetFrameworks> lines
    # (both unconditional and conditional variants).
    stripped = re.sub(
        r'\s*<TargetFrameworks\b[^>]*>[^<]*</TargetFrameworks>',
        '',
        content
    )

    # Also handle singular <TargetFramework> if present
    stripped = re.sub(
        r'\s*<TargetFramework\b[^>]*>[^<]*</TargetFramework>',
        '',
        stripped
    )

    # Insert a single unconditional <TargetFrameworks> with the iOS TFM
    # into the first <PropertyGroup>
    stripped = stripped.replace(
        '<PropertyGroup>',
        f'<PropertyGroup>\n    <TargetFrameworks>{framework}</TargetFrameworks>',
        1  # only the first PropertyGroup
    )

    with open(csproj_path, 'w') as f:
        f.write(stripped)

    logger.info(f"Stripped non-iOS TFMs. csproj now targets: {framework}")

def inject_csproj_properties(csproj_path: str, properties: dict):
    '''Inject MSBuild properties into the first PropertyGroup of a csproj.'''
    with open(csproj_path, 'r') as f:
        content = f.read()

    if '</PropertyGroup>' not in content:
        raise Exception(f"No <PropertyGroup> found in {csproj_path}")

    for name, value in properties.items():
        if name not in content:
            content = content.replace(
                '</PropertyGroup>',
                f'    <{name}>{value}</{name}>\n  </PropertyGroup>',
                1
            )

    with open(csproj_path, 'w') as f:
        f.write(content)
    logger.info(f"Injected properties into {csproj_path}: {list(properties.keys())}")

setup_loggers(True)
logger = getLogger(__name__)
logger.info("Starting pre-command for MAUI iOS deploy measurement")

precommands = PreCommands()

with MauiNuGetConfigContext(precommands.framework):
    install_maui_ios_workload(precommands)
    check_xcode_compatibility(precommands.framework)
    precommands.print_dotnet_info()

    # Create template without restoring packages — packages will be restored
    # on the Helix machine to avoid shipping ~1-2GB in the workitem payload.
    precommands.new(template='maui',
                    output_dir=const.APPDIR,
                    bin_dir=const.BINDIR,
                    exename=EXENAME,
                    working_directory=sys.path[0],
                    no_restore=True)

    # Copy the merged NuGet.config into the app directory. This file contains
    # MAUI NuGet feed URLs added by MauiNuGetConfigContext. The Helix machine
    # needs these feeds during restore, and we must copy before the context
    # manager restores the original NuGet.config.
    repo_root = os.path.normpath(os.path.join(sys.path[0], '..', '..', '..'))
    repo_nuget_config = os.path.join(repo_root, 'NuGet.config')
    app_nuget_config = os.path.join(const.APPDIR, 'NuGet.config')
    shutil.copy2(repo_nuget_config, app_nuget_config)
    logger.info(f"Copied merged NuGet.config from {repo_nuget_config} to {app_nuget_config}")

    # Prepare the csproj: strip non-iOS TFMs and inject required properties
    csproj_path = os.path.join(const.APPDIR, f'{EXENAME}.csproj')
    strip_non_ios_tfms(csproj_path, precommands.framework)
    inject_csproj_properties(csproj_path, {
        # Preview SDKs may lack prune-package-data files, causing NETSDK1226.
        'AllowMissingPrunePackageData': 'true',
        # Re-enable Roslyn compiler server (perf repo disables it globally
        # for BenchmarkDotNet) to match real developer inner loop.
        'UseSharedCompilation': 'true',
    })

    # Create modified source files in src/ for the incremental deploy simulation.
    # The runner toggles between original and modified versions each iteration,
    # exercising both the C# compiler (Csc) and XAML compiler (XamlC) paths.
    src_dir = os.path.join(sys.path[0], const.SRCDIR)
    os.makedirs(src_dir, exist_ok=True)

    # --- Modified MainPage.xaml.cs: add a debug line in the constructor ---
    # The template may place MainPage in either the root or Pages/ subdirectory.
    # Normalize to ALWAYS use Pages/ so that the hardcoded --edit-dest paths in
    # maui_scenarios_ios_innerloop.proj ("app/Pages/MainPage.xaml.cs") are valid.
    pages_dir = os.path.join(const.APPDIR, 'Pages')
    cs_candidates = [
        os.path.join(const.APPDIR, 'Pages', 'MainPage.xaml.cs'),
        os.path.join(const.APPDIR, 'MainPage.xaml.cs'),
    ]
    cs_original = None
    for candidate in cs_candidates:
        if os.path.exists(candidate):
            cs_original = candidate
            break
    if cs_original is None:
        raise Exception(
            "Could not find MainPage.xaml.cs in template — "
            f"searched: {cs_candidates}"
        )

    # If MainPage files are at the root, move them into Pages/ so that the
    # .proj's hardcoded edit-dest paths are always correct.
    if os.path.dirname(os.path.abspath(cs_original)) != os.path.abspath(pages_dir):
        os.makedirs(pages_dir, exist_ok=True)
        for fname in ['MainPage.xaml.cs', 'MainPage.xaml']:
            src_file = os.path.join(const.APPDIR, fname)
            if os.path.exists(src_file):
                shutil.move(src_file, os.path.join(pages_dir, fname))
                logger.info(f"Moved {src_file} → {os.path.join(pages_dir, fname)}")
        cs_original = os.path.join(pages_dir, 'MainPage.xaml.cs')

    cs_modified = os.path.join(src_dir, 'MainPage.xaml.cs')
    with open(cs_original, 'r') as f:
        cs_content = f.read()

    cs_modified_content = cs_content.replace(
        'InitializeComponent();',
        'InitializeComponent();\n\t\tSystem.Diagnostics.Debug.WriteLine("incremental-touch");'
    )
    if cs_modified_content == cs_content:
        raise Exception(
            "Could not find 'InitializeComponent();' in %s — template may have changed" % cs_original
        )

    with open(cs_modified, 'w') as f:
        f.write(cs_modified_content)
    logger.info(f"Modified MainPage.xaml.cs written to {cs_modified}")

    # --- Modified MainPage.xaml: change a label's text ---
    # Look in the same directory where we found the .cs file
    xaml_original = os.path.join(os.path.dirname(cs_original), 'MainPage.xaml')
    if not os.path.exists(xaml_original):
        raise Exception(f"Could not find MainPage.xaml at {xaml_original}")

    xaml_modified = os.path.join(src_dir, 'MainPage.xaml')
    with open(xaml_original, 'r') as f:
        xaml_content = f.read()

    # Use a flexible match — look for any Text="..." attribute on a Label
    # to handle template variations. Prefer a known string first.
    xaml_modified_content = xaml_content.replace(
        'Text="Hello, World!"',
        'Text="Hello, World! (updated)"'
    )
    if xaml_modified_content == xaml_content:
        # Fallback: try the .NET 10+ template's "Task Categories" text
        xaml_modified_content = xaml_content.replace(
            'Text="Task Categories"',
            'Text="Task Categories (updated)"'
        )
    if xaml_modified_content == xaml_content:
        # Last resort: replace the first Text="..." attribute we find
        xaml_modified_content = re.sub(
            r'Text="([^"]*)"',
            r'Text="\1 (updated)"',
            xaml_content,
            count=1
        )
    if xaml_modified_content == xaml_content:
        raise Exception(
            "Could not find any Text=\"...\" attribute in %s — template may have changed" % xaml_original
        )

    with open(xaml_modified, 'w') as f:
        f.write(xaml_modified_content)
    logger.info(f"Modified MainPage.xaml written to {xaml_modified}")
