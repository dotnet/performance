import os
import shutil
from shared import const, util
from dotnet import shutdown_server
from performance.logger import setup_loggers
from logging import getLogger
from argparse import ArgumentParser


SETUP_BUILD = 'setup_build'
SETUP_NEW = 'setup_new'
CLEANUP = 'cleanup'
operations = (SETUP_BUILD, SETUP_NEW, CLEANUP)

def main():
    parser = ArgumentParser()
    parser.add_argument('operation', choices=operations)
    args = parser.parse_args()

    setup_loggers(True)

    if args.operation == SETUP_BUILD:
        shutdown_dotnet_servers()
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

    if args.operation == SETUP_NEW:
        if not os.path.isdir(const.APPDIR):
            getLogger().info("Creating new project directory...")
            os.mkdir(const.APPDIR)

    if args.operation == CLEANUP:
        if os.path.isdir(const.APPDIR):
            getLogger().info("Removing project directory...")
            shutil.rmtree(const.APPDIR)


def shutdown_dotnet_servers():
    # shutdown_server(verbose=True) # This is the correct way to shut down dotnet build servers, but it has been disabled due to https://github.com/dotnet/sdk/issues/10573
    getLogger().info("Shutting down dotnet build servers...")
    if util.iswin():
        os.system('TASKKILL /F /T /IM dotnet.exe || TASKKILL /F /T /IM VSTest.Console.exe || TASKKILL /F /T /IM msbuild.exe')
    else:
        os.system('killall -9 dotnet || killall -9 VSTest.Console || killall -9 msbuild')

if __name__ == "__main__":
    main()