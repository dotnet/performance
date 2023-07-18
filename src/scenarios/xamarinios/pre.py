'''
pre-command
'''
import shutil
import sys
import subprocess
from performance.logger import setup_loggers, getLogger
from shared import const
from shared.mauisharedpython import remove_aab_files
from shared.precommands import PreCommands
from shared.versionmanager import versions_write_json, get_version_from_dll_powershell_ios
from test import EXENAME

setup_loggers(True)

precommands = PreCommands()
precommands.install_workload('ios')

# Setup the Maui folder
precommands.new(template='ios',
                output_dir=const.APPDIR,
                bin_dir=const.BINDIR,
                exename=EXENAME,
                working_directory=sys.path[0],
                no_restore=False)

# Build the APK
precommands.execute(['/p:_RequireCodeSigning=false', '/p:ApplicationId=net.dot.xamarintesting'])

# Remove the aab files as we don't need them, this saves space
output_dir = const.PUBDIR
if precommands.output:
    output_dir = precommands.output
remove_aab_files(output_dir)