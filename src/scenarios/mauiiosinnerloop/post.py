'''
post cleanup script
'''

import os
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

    # Determine device type from IOS_RID env var (set by .proj PreCommands)
    ios_rid = os.environ.get('IOS_RID', 'iossimulator-arm64')
    is_physical_device = (ios_rid == 'ios-arm64')

    if is_physical_device:
        device_udid = os.environ.get('IOS_DEVICE_UDID', '').strip()
        if not device_udid:
            # Auto-detect since env var may not have been exported by PreCommands
            try:
                from shared.ioshelper import iOSHelper
                device_udid = iOSHelper.detect_connected_device()
            except Exception:
                device_udid = None
        if device_udid:
            logger.info(f"Uninstalling {bundle_id} from physical device {device_udid}")
            subprocess.run(['xcrun', 'devicectl', 'device', 'uninstall', 'app',
                            '--device', device_udid, bundle_id], check=False)
        else:
            logger.warning("No IOS_DEVICE_UDID available — skipping physical device uninstall")
    else:
        logger.info(f"Uninstalling {bundle_id} from simulator")
        subprocess.run(['xcrun', 'simctl', 'uninstall', 'booted', bundle_id], check=False)

    logger.info("Shutting down dotnet build servers")
    subprocess.run(['dotnet', 'build-server', 'shutdown'], check=False)

    clean_directories()
except Exception as e:
    logger.error(f"Post cleanup failed: {e}\n{traceback.format_exc()}")
    sys.exit(1)
