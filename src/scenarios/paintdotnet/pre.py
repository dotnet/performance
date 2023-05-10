'''
pre-command
'''
import sys
import os, subprocess
import zipfile
from performance.logger import setup_loggers
from os.path import join, dirname
from shared.util import helixcorrelationpayload
from performance.common import runninginlab
from shared.precommands import PreCommands
from shared import const
from logging import getLogger
from shutil import copytree, rmtree

setup_loggers(True)

precommands = PreCommands()

output = precommands.output or const.PUBDIR
with zipfile.ZipFile(precommands.pathtozip, 'r') as publish:
    publish.extractall(output)
rmtree(dirname(precommands.pathtozip))
getLogger().info(f"Unpacked {precommands.pathtozip} into {output}.")