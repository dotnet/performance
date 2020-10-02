from shared.const import APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR
from performance.common import remove_directory

def clean_directories():
    to_remove = (APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR)
    print(f"Removing {','.join(to_remove)} if exist ...")
    for dir in to_remove:
        remove_directory(dir)