'''
post cleanup script
'''

import zipfile
from os import walk, environ
from os.path import isfile, join
from shared.postcommands import clean_directories
from shared import const
from performance.common import runninginlab
from test import EXENAME

if runninginlab():
    with zipfile.ZipFile(join(environ["HELIX_WORKITEM_UPLOAD_ROOT"], "Publish-{}.zip".format(EXENAME)), 'x') as publish:
        f = []
        for (dirpath, dirnames, filenames) in walk(const.PUBDIR):
            for name in filenames:
                f.append(join(dirpath, name))
        for files in f:
            publish.write(files)
        
clean_directories()
