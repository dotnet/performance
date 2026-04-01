'''
post cleanup script
'''

import subprocess
import sys
import traceback
from performance.logger import setup_loggers, getLogger
from shared.postcommands import clean_directories
from shared.util import xharness_adb
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)

try:
    # Uninstall the app from the connected device so re-runs start from a clean state
    package_name = f'com.companyname.{EXENAME.lower()}'
    logger.info(f"Uninstalling {package_name} from device")
    subprocess.run(xharness_adb() + ['uninstall', package_name], check=False)

    logger.info("Shutting down dotnet build servers")
    subprocess.run(['dotnet', 'build-server', 'shutdown'], check=False)

    clean_directories()
except Exception as e:
    logger.error(f"Post cleanup failed: {e}\n{traceback.format_exc()}")
    sys.exit(1)
