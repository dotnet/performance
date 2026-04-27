'''
pre-command: Set up a MAUI Android app for deploy measurement.
Creates the template (without restore) and prepares the modified file for incremental deploy.
NuGet packages are restored on the Helix machine, not shipped in the payload.
'''
import os
import shutil
import sys
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import install_latest_maui, MauiNuGetConfigContext
from shared.precommands import PreCommands
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)
logger.info("Starting pre-command for MAUI Android deploy measurement")

precommands = PreCommands()

with MauiNuGetConfigContext(precommands.framework):
    install_latest_maui(
        precommands,
        workloads=["microsoft.net.sdk.android"],
        workload_name='maui-android',
    )

    # Log the generated rollback file for diagnostics. install_latest_maui
    # writes this; if it's missing the install failed and we want to crash.
    with open("rollback_maui.json", "r") as f:
        logger.info(f"Generated rollback_maui.json contents:\n{f.read()}")

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
