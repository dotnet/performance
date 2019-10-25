'''
post cleanup script
'''
import os
from performance.common import remove_directory
from shared.const import BINDIR, PUBDIR, APPDIR, TRACEDIR
import shutil


def traverse_dirs(root_dir):
    # traverse root directory, and list directories as dirs and files as files
    for root, dirs, files in os.walk(root_dir):
        path = root.split(os.sep)
        print((len(path) - 1) * '---', os.path.basename(root))
        for file in files:
            print(len(path) * '---', file)

artifacts_dir = 'D:\\a\\1\\s\\artifacts'
dest_dir = os.path.join(artifacts_dir, TRACEDIR)
shutil.copytree(TRACEDIR, dest_dir)

traverse_dirs('D:\\a\\1\\s')

remove_directory(BINDIR)
remove_directory(PUBDIR)
remove_directory(APPDIR)
remove_directory(TRACEDIR)

