'''
post cleanup script
'''

from performance.common import remove_directory
from shared.const import BINDIR, PUBDIR, APPDIR, TRACEDIR, TMPDIR

remove_directory(BINDIR)
remove_directory(PUBDIR)
remove_directory(APPDIR)
remove_directory(TRACEDIR)
remove_directory(TMPDIR)

