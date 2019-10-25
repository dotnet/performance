'''
post cleanup script
'''
import os
from performance.common import remove_directory
from shared.const import BINDIR, PUBDIR, APPDIR, TRACEDIR
import shutil

artifacts_dir = 'D:\\a\\1\\s\\artifacts'
dest_dir = os.path.join(artifacts_dir, TRACEDIR)
shutil.copytree(TRACEDIR, dest_dir)
print(os.listdir(dest_dir))

remove_directory(BINDIR)
remove_directory(PUBDIR)
remove_directory(APPDIR)
remove_directory(TRACEDIR)

