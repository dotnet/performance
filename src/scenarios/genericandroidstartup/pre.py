'''
pre-command
'''
import sys
import os
from zipfile import ZipFile
from performance.logger import setup_loggers, getLogger
from shutil import copyfile
from shared.precommands import PreCommands
from shared.const import PUBDIR
from argparse import ArgumentParser

setup_loggers(True)

parser = ArgumentParser()
parser.add_argument(
        '--apk-name',
        dest='apk',
        required=True,
        type=str,
        help='Name of the APK to setup (with .apk)')
args = parser.parse_args()

if not os.path.exists(PUBDIR):
    os.mkdir(PUBDIR)
apkname = args.apk
if not os.path.exists(apkname):
    getLogger().error('Cannot find %s' % (apkname))
    exit(-1)
else:
    copyfile(apkname, os.path.join(PUBDIR, apkname))



