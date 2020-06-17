'''
post cleanup script
'''
import os
from shared.postcommands import move_artifacts
from shared.const import APPDIR

move_artifacts(os.path.join(APPDIR, 'obj', 'Release', 'netstandard2.1', 'blazor', 'linker', 'linker-dependencies.xml.gz'))
