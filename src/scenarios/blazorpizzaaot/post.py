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
import subprocess

if runninginlab():   
    with zipfile.ZipFile(join(environ["HELIX_WORKITEM_UPLOAD_ROOT"], "Publish-{}.zip".format(EXENAME)), 'x') as publish:
        f = []
        for (dirpath, dirnames, filenames) in walk(const.PUBDIR):
            for name in filenames:
                f.append(join(dirpath, name))
        for files in f:
            publish.write(files)
        
subprocess.run(["dotnet", "workload", "uninstall", "microsoft-net-sdk-blazorwebassembly-aot"])
clean_directories()
