'''
Wrapper around startup tool.
'''
from logging import getLogger
import sys
import os
import json
from shutil import copytree, copy
from typing import Optional
from performance.common import helixpayload, extension, runninginlab, get_artifacts_directory, get_packages_directory, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_QUEUE
from dotnet import CSharpProject, CSharpProjFile
from shared.util import helixworkitempayload, helixuploaddir, getruntimeidentifier
from shared.const import *
class SODWrapper(object):
    '''
    Wraps sod.exe, building it if necessary.
    '''
    def __init__(self):
        helix_payload = helixpayload()
        helix_workitem_payload = helixworkitempayload()
        if helix_payload and os.path.exists(os.path.join(helix_payload, 'SOD')):
            self._setsodpath(os.path.join(helix_payload, 'SOD'))
        elif helix_workitem_payload and os.path.exists(os.path.join(helix_workitem_payload, 'SOD')):
            self._setsodpath(os.path.join(helix_workitem_payload, 'SOD'))
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
                                None, 
                                '--no-restore'
                                )
            self._setsodpath(sod.bin_path)

    
    def _setsodpath(self, path: str):
        self.sodexe = os.path.join(path, 'SizeOnDisk%s' % extension())

    def runtests(self, scenarioname: Optional[str], dirs: str, upload_to_perflab_container: bool, artifact: str):
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

        helix_upload_dir = helixuploaddir()
        if runninginlab() and helix_upload_dir is not None:
            copytree(TRACEDIR, os.path.join(helix_upload_dir, 'traces'))

            with open(reportjson, 'r') as json_file:
                json_result = json.load(json_file)
                # Check all SOD tests for files being found
                for test in json_result['tests']:
                    results_found = False
                    if 'SizeOnDisk' in test['categories']:
                        for counter in test['counters']:
                            # Check for any files being counted
                            if counter['metricName'] == 'count' and 0 not in counter['results']:
                                results_found = True
                                break
                        if not results_found:
                            raise ValueError(f'No files found for sizing in scenario {test["name"]}')
                
            if upload_to_perflab_container:
                import upload
                upload_code = upload.upload(reportjson, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_STORAGE_URI)
                getLogger().info("SoD Upload Code: " + str(upload_code))
                if upload_code != 0:
                    sys.exit(upload_code)
