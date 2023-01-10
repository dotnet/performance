'''
pre-command
'''
import sys
import os, subprocess
import zipfile
from performance.logger import setup_loggers
from os.path import join, normpath
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
copytree(join(output, 'PDN10', 'Release'), output, dirs_exist_ok=True)
rmtree(join(output, 'PDN10'))
getLogger().info(f"Unpacked {precommands.pathtozip} into {output}.")