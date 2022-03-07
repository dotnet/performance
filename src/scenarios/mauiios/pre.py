'''
pre-command
'''
import sys
import os
from zipfile import ZipFile
from performance.logger import setup_loggers, getLogger
from shutil import copyfile, copytree, move
from shared.const import PUBDIR
from argparse import ArgumentParser

setup_loggers(True)

parser = ArgumentParser()
parser.add_argument('--unzip', help='Unzip ipa file and report extracted tree', action='store_true', default=False)
parser.add_argument(
        '--name',
        dest='name',
        required=True,
        type=str,
        help='Name of the file/folder to setup (with .app or .ipa)')
args = parser.parse_args()

name = args.name
namezip = '%s.zip' % (name)
if not os.path.exists(PUBDIR):
    os.mkdir(PUBDIR)
if not os.path.exists(name):
    getLogger().error('Cannot find %s' % (name))
    exit(-1)
if args.unzip:
    if not os.path.exists(namezip):
        copyfile(name, namezip)

    with ZipFile(namezip) as zip:
        zip.extractall(os.path.join('.', PUBDIR))

    baseDir = os.path.join(PUBDIR, 'Payload')
    appFolder = os.listdir(baseDir) # Should only be one
    if len(appFolder) > 1:
        getLogger.error("More than one app folder found in payload! Should only be one.")
        exit(-1)

    appFolderPath = os.path.join(baseDir, appFolder[0])
    for subfile in os.listdir(appFolderPath):
        move(os.path.join(appFolderPath, subfile), PUBDIR)
    os.removedirs(appFolderPath)
    
else:
    if(os.path.isdir(name)):
        copytree(name, f"{PUBDIR}/{name}")
    else:
        copyfile(name, f"{PUBDIR}/")
