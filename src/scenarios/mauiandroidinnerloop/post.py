'''
post cleanup script
'''

import subprocess
from performance.logger import setup_loggers, getLogger
from shared.postcommands import clean_directories
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)

# Uninstall the app from the connected device so re-runs start from a clean state
package_name = f'com.companyname.{EXENAME.lower()}'
logger.info(f"Uninstalling {package_name} from device")
subprocess.run(['adb', 'uninstall', package_name], check=False)

# Shut down the build server to release file locks before cleanup
logger.info("Shutting down build server")
subprocess.run(['dotnet', 'build-server', 'shutdown'], check=False)

clean_directories()
