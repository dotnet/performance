'''
pre-command
'''
import sys
import os, subprocess
import zipfile
from performance.logger import setup_loggers
from os.path import join
from shared.util import helixcorrelationpayload
from performance.common import runninginlab
from shared.precommands import PreCommands
from shared import const
from logging import getLogger
from shutil import copytree

setup_loggers(True)

precommands = PreCommands()

if runninginlab():   
    with zipfile.ZipFile(join(helixcorrelationpayload(), "PDN\PDN.zip"), 'r') as publish:
        publish.extractall(const.PUBDIR)
elif precommands.pathtozip:
    with zipfile.ZipFile(precommands.pathtozip, 'r') as publish:
        publish.extractall(const.PUBDIR)