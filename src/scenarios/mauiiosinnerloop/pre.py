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

    # TEMPORARY PIN: Hardcode the iOS workload version to 26.2.x instead of
    # querying NuGet feeds dynamically. The dynamic query resolves to the
    # latest nightly packs which now require Xcode 26.4, but Helix machines
    # only have Xcode 26.2 installed.
    #
    # Cross-band note: the Helix SDK is preview.4 but the 26.2.x manifests
    # only exist on the preview.3 band. Using --from-rollback-file to attempt
    # cross-band installation — this is experimental and may need adjustment.
    #
    # TODO: Remove this pin when Helix machines have Xcode 26.4, and restore
    # the dynamic NuGet feed query that was here before (see git history).
    pinned_version = "26.2.11591-net11-p4"
    pinned_band = "11.0.100-preview.3"
    logger.info(
        f"Using PINNED iOS workload: {workload} version={pinned_version} "
        f"band={pinned_band} (Xcode 26.2 compatible)"
    )

    # Create rollback file with only the iOS workload
    rollback_value = f"{pinned_version}/{pinned_band}"
    rollback_dict = {workload: rollback_value}
    logger.info(f"Rollback dictionary: {rollback_dict}")
    with open("rollback_maui.json", "w", encoding="utf-8") as f:
        f.write(json.dumps(rollback_dict, indent=4))
    logger.info("Created rollback_maui.json file")

    # Install maui-ios (not 'maui') — only installs iOS components.
    # Uses --from-rollback-file to pin to the latest nightly packs from the
    # feed. When a new manifest is published, referenced SDK packs may not
    # have propagated to all NuGet feeds yet, causing "package NOT FOUND".
    # Fall back to installing without the rollback file, which lets the SDK
    # resolve a recent stable version that is already fully available.
    try:
        precommands.install_workload('maui-ios', ['--from-rollback-file', 'rollback_maui.json'])
    except Exception as e:
        logger.warning(f"Workload install with rollback file failed (possible NuGet version skew): {e}")
        logger.info("Retrying without rollback file (will use SDK default version)...")
        precommands.install_workload('maui-ios')
    logger.info("########## Finished installing maui-ios workload ##########")

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
