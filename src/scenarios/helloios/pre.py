'''
pre-command
'''
import sys
import os
from zipfile import ZipFile
from performance.logger import setup_loggers
from shutil import copytree
from shared.precommands import PreCommands
from shared.const import PUBDIR
from argparse import ArgumentParser

setup_loggers(True)

copytree('app', PUBDIR)
