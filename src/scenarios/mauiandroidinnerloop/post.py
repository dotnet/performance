'''
post cleanup script
'''

import os
import subprocess
from performance.logger import setup_loggers, getLogger
from shared.postcommands import clean_directories
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)

def resolve_adb():
    '''Find the adb binary by checking known locations.

    Order:
      1. ANDROID_HOME or ANDROID_SDK_ROOT env var → platform-tools/adb
      2. HELIX_WORKITEM_ROOT env var → android-sdk/platform-tools/adb
         (run.sh and run.cmd create the SDK there)
      3. Bare 'adb' (hope it's on PATH)
    '''
    for env_var in ('ANDROID_HOME', 'ANDROID_SDK_ROOT'):
        sdk_root = os.environ.get(env_var)
        if sdk_root:
            candidate = os.path.join(sdk_root, 'platform-tools', 'adb')
            if os.path.isfile(candidate):
                return candidate

    helix_root = os.environ.get('HELIX_WORKITEM_ROOT')
    if helix_root:
        candidate = os.path.join(helix_root, 'android-sdk', 'platform-tools', 'adb')
        if os.path.isfile(candidate):
            return candidate

    return 'adb'

adb = resolve_adb()
logger.info(f"Resolved adb: {adb}")

# Pin to the emulator device to avoid "more than one device/emulator" errors
# on the Android 36 queue which registers two ADB transports.
# 'emulator-5554' is the standard ADB serial for the first Android emulator instance
# and is stable across all Android SDK versions.
if 'ANDROID_SERIAL' not in os.environ:
    os.environ['ANDROID_SERIAL'] = 'emulator-5554'
    logger.info("Set ANDROID_SERIAL=emulator-5554")

# Uninstall the app from the connected device so re-runs start from a clean state
package_name = f'com.companyname.{EXENAME.lower()}'
logger.info(f"Uninstalling {package_name} from device")
subprocess.run([adb, 'uninstall', package_name], check=False)

logger.info("Shutting down dotnet build servers")
subprocess.run(['dotnet', 'build-server', 'shutdown'], check=False)

clean_directories()
