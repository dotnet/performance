from performance.common import remove_directory, copy_directory
from shared import const
from dotnet import shutdown_server
from performance.logger import setup_loggers
from logging import getLogger


def main():
    setup_loggers(True)
    shutdown_server(verbose=True)
    getLogger().info("Removing project directory...") # set up logger would create new log files, which is not necessary
    remove_directory(const.TMPDIR)
    getLogger().info("Copying clean project directory...")
    copy_directory(const.APPDIR, const.TMPDIR)


if __name__ == "__main__":
    main()
    