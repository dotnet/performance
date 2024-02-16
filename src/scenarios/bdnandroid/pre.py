'''
pre-command
'''
import os
import time
from argparse import ArgumentParser
from zipfile import ZipFile
from shutil import copyfile
from shared.const import PUBDIR
from shared.util import xharnesscommand

from performance.common import RunCommand
from performance.logger import setup_loggers, getLogger

setup_loggers(True)

parser = ArgumentParser()
parser.add_argument('--unzip', help='Unzip APK and report extracted tree', action='store_true', default=False)
parser.add_argument(
        '--apk-name',
        dest='apk',
        required=True,
        type=str,
        help='Name of the APK to setup (with .apk)')
parser.add_argument('--restart-device', help='Restart the device before running the tests', action='store_true', default=False)
args = parser.parse_args()

if not os.path.exists(PUBDIR):
    os.mkdir(PUBDIR)
apkname = args.apk
apknamezip = '%s.zip' % (apkname)
if not os.path.exists(apkname):
    getLogger().error('Cannot find %s' % (apkname))
    exit(-1)
if args.unzip:
    if not os.path.exists(apknamezip):
        copyfile(apkname, apknamezip)

    with ZipFile(apknamezip) as zip:
        zip.extractall(os.path.join('.', PUBDIR))

    assets_dir = os.path.join(PUBDIR, 'assets')
    assets_zip = os.path.join(assets_dir, 'assets.zip')
    with ZipFile(assets_zip) as zip:
        zip.extractall(assets_dir)

    os.remove(assets_zip)
else:
    copyfile(apkname, os.path.join(PUBDIR, apkname))

if args.restart_device:
    cmdline = xharnesscommand() + ['android', 'state', '--adb']
    adb = RunCommand(cmdline, verbose=True)
    adb.run()

    # Do not remove, XHarness install seems to fail without an adb command called before the xharness command
    getLogger().info("Preparing ADB")
    adbpath = adb.stdout.strip()
    waitForDeviceCmd = [
        adbpath,
        'wait-for-device'
    ]

    checkDeviceBootCmd = [
        adbpath,
        'shell',
        'getprop',
        'sys.boot_completed'
    ]

    getLogger().info("Waiting for device to come back online")
    RunCommand(waitForDeviceCmd, verbose=True).run()
    
    # Wait for the device to boot
    getLogger().info("Waiting for device to boot")
    boot_completed = False
    boot_attempts = 0
    while not boot_completed and boot_attempts < 10:
        time.sleep(5)
        boot_check = RunCommand(checkDeviceBootCmd, verbose=True)
        boot_check.run()
        boot_completed = boot_check.stdout.strip() == '1'
        boot_attempts += 1
        getLogger().info("Device not booted yet, waiting 5 seconds")

    if not boot_completed:
        getLogger().error("Android device did not boot in a reasonable time")
        raise TimeoutError("Android device did not boot in a reasonable time")

