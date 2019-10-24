'''
post cleanup script
'''

from performance.common import remove_directory
from shared.const import BINDIR, PUBDIR, APPDIR, TRACEDIR
from shutil import copytree

copytree(TRACEDIR, "D:\\a\\1\\s\\artifacts\\log\\traces")
remove_directory(BINDIR)
remove_directory(PUBDIR)
remove_directory(APPDIR)
remove_directory(TRACEDIR)

