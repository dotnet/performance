'''
pre-command
'''
from performance.logger import setup_loggers
from shutil import copytree
from shared.const import PUBDIR

setup_loggers(True)

copytree('app', PUBDIR)
