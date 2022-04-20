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
    
else:
    if(os.path.isdir(name)):
        copytree(name, PUBDIR, dirs_exist_ok=True)
    else:
        copyfile(name, os.path.join(PUBDIR, name))
