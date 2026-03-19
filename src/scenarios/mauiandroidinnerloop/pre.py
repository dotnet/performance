'''
pre-command: Set up a MAUI Android app for deploy measurement.
Creates the template, restores packages, and prepares the modified file for incremental deploy.
'''
import os
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

    precommands.new(template='maui',
                    output_dir=const.APPDIR,
                    bin_dir=const.BINDIR,
                    exename=EXENAME,
                    working_directory=sys.path[0],
                    no_restore=False)

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
