'''
post cleanup script
'''

import subprocess
import sys
import traceback
from performance.logger import setup_loggers, getLogger
from shared.postcommands import clean_directories
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)

try:
    bundle_id = f'com.companyname.{EXENAME.lower()}'
    logger.info(f"Uninstalling {bundle_id} from simulator")
    subprocess.run(['xcrun', 'simctl', 'uninstall', 'booted', bundle_id], check=False)

    logger.info("Shutting down dotnet build servers")
    subprocess.run(['dotnet', 'build-server', 'shutdown'], check=False)

    clean_directories()
except Exception as e:
    logger.error(f"Post cleanup failed: {e}\n{traceback.format_exc()}")
    sys.exit(1)
