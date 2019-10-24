'''
post cleanup script
'''
import os
from performance.common import remove_directory
from shared.const import BINDIR, PUBDIR, APPDIR, TRACEDIR
from shutil import copytree

copytree(TRACEDIR, "D:\\a\\1\\s\\artifacts\\log\\x64_netcoreapp5.0_scenarios\\traces")
files = os.listdir("D:\\a\\1\\s\\artifacts\\log\\x64_netcoreapp5.0_scenarios")
print(files)
remove_directory(BINDIR)
remove_directory(PUBDIR)
remove_directory(APPDIR)
remove_directory(TRACEDIR)

