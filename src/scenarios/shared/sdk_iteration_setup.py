from performance.common import remove_directory, copy_directory
from shared import const
from dotnet import shutdown_server


def main():
    shutdown_server(verbose=True)
    print("Removing project directory...") # set up logger would create new log files, which is not necessary
    remove_directory(const.APPDIR)
    print("Copying clean project directory...")
    copy_directory(const.TMPDIR, const.APPDIR)


if __name__ == "__main__":
    main()