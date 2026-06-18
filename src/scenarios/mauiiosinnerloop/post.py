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
    try:
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
    except Exception as e:
        logger.warning("iOS uninstall failed (continuing): %s", e)

    workitem_root = os.environ.get('HELIX_WORKITEM_ROOT', '')
    sim_udid_path = os.path.join(workitem_root, 'sim_udid.txt') if workitem_root else ''
    if sim_udid_path and os.path.isfile(sim_udid_path):
        try:
            with open(sim_udid_path, 'r', encoding='utf-8') as f:
                created_udid = f.read().strip()
            if created_udid:
                logger.info("Deleting per-workitem simulator: %s", created_udid)
                iOSHelper.delete_simulator(created_udid)
        except Exception as e:
            logger.warning("Could not delete per-workitem simulator: %s", e)

    subprocess.run(['dotnet', 'build-server', 'shutdown'], check=False)
    clean_directories()
except Exception as e:
    logger.warning(f"Post cleanup encountered an error (best-effort, continuing): {e}\n{traceback.format_exc()}")
