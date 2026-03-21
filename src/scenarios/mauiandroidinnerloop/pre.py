'''
pre-command: Set up a MAUI Android app for deploy measurement.
Creates the template (without restore) and prepares the modified file for incremental deploy.
NuGet packages are restored on the Helix machine, not shipped in the payload.
'''
import os
import re
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
    install_latest_maui(precommands)
    precommands.print_dotnet_info()

    # Create template without restoring packages — packages will be restored
    # on the Helix machine to avoid shipping ~1-2GB in the workitem payload.
    precommands.new(template='maui',
                    output_dir=const.APPDIR,
                    bin_dir=const.BINDIR,
                    exename=EXENAME,
                    working_directory=sys.path[0],
                    no_restore=True)

    # Copy the merged NuGet.config (which includes MAUI feed URLs) into the app
    # directory so the Helix machine can find feeds during restore.
    # MauiNuGetConfigContext temporarily merges MAUI feeds into the repo-root
    # NuGet.config; we must copy it before the context manager restores the original.
    repo_root = os.path.normpath(os.path.join(sys.path[0], '..', '..', '..'))
    repo_nuget_config = os.path.join(repo_root, 'NuGet.config')
    app_nuget_config = os.path.join(const.APPDIR, 'NuGet.config')
    shutil.copy2(repo_nuget_config, app_nuget_config)
    logger.info(f"Copied merged NuGet.config from {repo_nuget_config} to {app_nuget_config}")

    # Fix the .csproj to target only Android (remove iOS, MacCatalyst, Windows TFMs).
    # The MAUI template targets all platforms, but the Helix machine only has the Android SDK.
    csproj_path = os.path.join(const.APPDIR, f'{EXENAME}.csproj')
    with open(csproj_path, 'r') as f:
        csproj_content = f.read()

    android_tfm = f'{precommands.framework}-android'
    csproj_content = re.sub(
        r'<TargetFrameworks>.*?</TargetFrameworks>',
        f'<TargetFrameworks>{android_tfm}</TargetFrameworks>',
        csproj_content
    )

    with open(csproj_path, 'w') as f:
        f.write(csproj_content)

    logger.info(f"Updated {csproj_path}: TargetFrameworks set to {android_tfm}")

    # Copy the modified MainPage.xaml.cs into src/ for the incremental deploy simulation.
    # test.py will copy this over app/MainPage.xaml.cs between deploys.
    src_dir = os.path.join(sys.path[0], const.SRCDIR)
    os.makedirs(src_dir, exist_ok=True)

    original_file = os.path.join(const.APPDIR, 'MainPage.xaml.cs')
    modified_file = os.path.join(src_dir, 'MainPage.xaml.cs')

    with open(original_file, 'r') as f:
        content = f.read()

    # Modify a string literal to trigger assembly recompilation
    modified_content = content.replace('Hello, World!', 'Hello, World! ')

    if modified_content == content:
        # Fallback: append a partial class extension to guarantee a code change
        modified_content = content + '\npartial class MainPage { static string _ts = "modified"; }\n'

    with open(modified_file, 'w') as f:
        f.write(modified_content)

    logger.info(f"Modified MainPage.xaml.cs written to {modified_file}")
