'''
Wrapper around startup tool.
'''
import sys
import os
import platform
from shutil import copytree, copy
from performance.logger import setup_loggers
from performance.common import get_artifacts_directory, get_packages_directory, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_TOKEN_VAR, UPLOAD_QUEUE
from dotnet import CSharpProject, CSharpProjFile
from shared.util import helixpayload, helixworkitempayload, helixuploaddir, builtexe, publishedexe, runninginlab, uploadtokenpresent, getruntimeidentifier, extension
from shared.const import *
class SODWrapper(object):
    '''
    Wraps sod.exe, building it if necessary.
    '''
    def __init__(self):
        if helixpayload() and os.path.exists(os.path.join(helixpayload(), 'SOD')):
            self._setsodpath(os.path.join(helixpayload(), 'SOD'))
        elif helixworkitempayload() and os.path.exists(os.path.join(helixworkitempayload(), 'SOD')):
            self._setsodpath(os.path.join(helixworkitempayload(), 'SOD'))
        else:
            relpath = os.path.join(get_artifacts_directory(), 'SOD')
            sodproj = os.path.join('..',
                                       '..',
                                       'tools',
                                       'ScenarioMeasurement',
                                       'SizeOnDisk',
                                       'SizeOnDisk.csproj')
            sod = CSharpProject(CSharpProjFile(sodproj,
                                                   sys.path[0]),
                                                   os.path.join(os.path.dirname(sodproj),
                                    os.path.join(get_artifacts_directory(), 'SOD')))
            if not os.path.exists(relpath):
                sod.restore(get_packages_directory(),
                                True,
                                getruntimeidentifier())
                sod.publish('Release',
                                relpath,
                                True,
                                get_packages_directory(),
                                None,
                                getruntimeidentifier(),
                                '--no-restore'
                                )
            self._setsodpath(sod.bin_path)

    
    def _setsodpath(self, path: str):
        self.sodexe = os.path.join(path, 'SizeOnDisk%s' % extension())

    def runtests(self, scenarioname, dirs, artifact=None):
        '''
        Runs tests through sod tool
        '''
        if not os.path.exists(TRACEDIR):
            os.mkdir(TRACEDIR)
        reportjson = os.path.join(TRACEDIR, 'perf-lab-report.json')
        sod_args = [
            self.sodexe,
            '--report-json-path', reportjson,
            '--scenario-name', (scenarioname or "Empty Scenario Name"),
            '--dirs'
        ]
        sod_args += dirs.split(';')

        RunCommand(sod_args, verbose=True).run()
 
        if artifact:
          if not os.path.exists(artifact):
            raise FileNotFoundError(f'Artifact {artifact} is not found.')
          else:
            copy(artifact, TRACEDIR)

        if runninginlab():
            copytree(TRACEDIR, os.path.join(helixuploaddir(), 'traces'))
            if uploadtokenpresent():
                import upload
                upload.upload(reportjson, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_TOKEN_VAR, UPLOAD_STORAGE_URI)
