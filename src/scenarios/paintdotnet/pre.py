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

if runninginlab():   
    with zipfile.ZipFile(join(helixcorrelationpayload(), "PDN\PDN3.zip"), 'r') as publish:
        publish.extractall(const.PUBDIR)
else:
    with zipfile.ZipFile("D:\work\PDN3\PDN4\Release\PDN3.zip", 'r') as publish:
        publish.extractall(const.PUBDIR)