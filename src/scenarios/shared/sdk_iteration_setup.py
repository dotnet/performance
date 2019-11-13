import os
import shutil
from shared import const
from dotnet import shutdown_server
from performance.logger import setup_loggers
from logging import getLogger
from argparse import ArgumentParser

SETUP = 'setup'
CLEANUP = 'cleanup'
operations = (SETUP, CLEANUP)

def main():
    parser = ArgumentParser()
    parser.add_argument('operation', choices=operations)
    args = parser.parse_args()

    setup_loggers(True)

    if args.operation == SETUP:
        shutdown_server(verbose=True)
        if not os.path.isdir(const.TMPDIR):
            if not os.path.isdir(const.APPDIR):
                raise Exception("\'app\' folder should exist. Please run pre.py.")
            getLogger().info("Backing up project directory...")
            shutil.copytree(const.APPDIR, const.TMPDIR) # backup from app to tmp
        else:
            if os.path.isdir(const.APPDIR):
                shutil.rmtree(const.APPDIR)
            getLogger().info("Copying clean project directory...")
            shutil.copytree(const.TMPDIR, const.APPDIR) # use the copy

    if args.operation == CLEANUP:
        if os.path.isdir(const.APPDIR):
            getLogger().info("Removing project directory...")
            shutil.rmtree(const.APPDIR)


if __name__ == "__main__":
    main()