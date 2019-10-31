import os
import shutil
from shared import const
from dotnet import shutdown_server
from performance.logger import setup_loggers
from logging import getLogger


def main():
    setup_loggers(True)
    shutdown_server(verbose=True)
    getLogger().info("Removing project directory...")
    if os.path.isdir(const.TMPDIR):
        shutil.rmtree(const.TMPDIR)
    getLogger().info("Copying clean project directory...")
    shutil.copytree(const.APPDIR, const.TMPDIR)


if __name__ == "__main__":
    main()