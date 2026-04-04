'''
post cleanup script
'''

import os
import subprocess
import sys
import traceback
from performance.logger import setup_loggers, getLogger
from shared.ioshelper import iOSHelper
from shared.postcommands import clean_directories
from test import EXENAME

setup_loggers(True)
logger = getLogger(__name__)

try:
    bundle_id = f'com.companyname.{EXENAME.lower()}'
    ios_rid = os.environ.get('IOS_RID', 'iossimulator-arm64')
    is_physical = (ios_rid == 'ios-arm64')

    helper = iOSHelper()
    if is_physical:
        device_udid = iOSHelper.detect_connected_device()
        if device_udid:
            helper.setup_device(bundle_id, None, device_udid, is_physical=True)
            helper.cleanup()
        else:
            logger.warning("No device UDID available — skipping uninstall")
    else:
        helper.setup_device(bundle_id, None, 'booted', is_physical=False)
        helper.cleanup()

    subprocess.run(['dotnet', 'build-server', 'shutdown'], check=False)
    clean_directories()
except Exception as e:
    logger.error(f"Post cleanup failed: {e}\n{traceback.format_exc()}")
    sys.exit(1)
