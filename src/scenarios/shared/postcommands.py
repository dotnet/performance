import shutil, os
from shared.const import APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR, ARTIFACTDIR
from performance.common import remove_directory, make_directory

def clean_directories():
    to_remove = (APPDIR, TMPDIR, TRACEDIR, PUBDIR, BINDIR)
    for dir in to_remove:
        remove_directory(dir)

def move_artifacts(artifact:str):
    make_directory(ARTIFACTDIR)
    if not os.path.exists(artifact):
      print(f'Artifact {artifact} does not exist')
      exit(1)
    shutil.copy(artifact, ARTIFACTDIR)
    