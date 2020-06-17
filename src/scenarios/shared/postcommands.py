from shared.const import APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR
from performance.common import remove_directory

def clean_directories():
    to_remove = (APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR)
    for dir in to_remove:
        remove_directory(dir)