'''
post cleanup script
'''
import os
from performance.common import remove_directory
from shared.const import BINDIR, PUBDIR, APPDIR, TRACEDIR
import shutil

files = os.listdir(TRACEDIR)
for file in files:
    shutil.copyfile(os.path.join(TRACEDIR, file),
                    os.path.join('D:\\a\\1\\s\\artifacts\\log\\x64_netcoreapp5.0_scenarios', file))
print(os.listdir('D:\\a\\1\\s\\artifacts\\log\\x64_netcoreapp5.0_scenarios'))
remove_directory(BINDIR)
remove_directory(PUBDIR)
remove_directory(APPDIR)
remove_directory(TRACEDIR)

