'''
Wrapper around startup tool.
'''
import sys
import os
import platform
from shutil import copytree
from performance.logger import setup_loggers
from performance.common import get_artifacts_directory, get_packages_directory, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_TOKEN_VAR, UPLOAD_QUEUE
from dotnet import CSharpProject, CSharpProjFile
from shared.util import extension, helixpayload, helixworkitempayload, helixuploaddir, builtexe, publishedexe, runninginlab, uploadtokenpresent, getruntimeidentifier, iswin
from shared.const import *
class StartupWrapper(object):
    '''
    Wraps startup.exe, building it if necessary.
    '''
    def __init__(self):
        startupdir = 'startup'
        if helixpayload() and os.path.exists(os.path.join(helixpayload(), startupdir)):
            self._setstartuppath(os.path.join(helixpayload(), startupdir))
        elif helixworkitempayload() and os.path.exists(os.path.join(helixworkitempayload(), startupdir)):
            self._setstartuppath(os.path.join(helixworkitempayload(), startupdir))
        else:
            relpath = os.path.join(get_artifacts_directory(), startupdir)
            startupproj = os.path.join('..',
                                       '..',
                                       'tools',
                                       'ScenarioMeasurement',
                                       'Startup',
                                       'Startup.csproj')
            startup = CSharpProject(CSharpProjFile(startupproj,
                                                   sys.path[0]),
                                                   os.path.join(os.path.dirname(startupproj),
                                                   os.path.join(get_artifacts_directory(), startupdir)))
            if not os.path.exists(relpath):
                startup.restore(get_packages_directory(),
                                True,
                                getruntimeidentifier())
                startup.publish('Release',
                                relpath,
                                True,
                                get_packages_directory(),
                                None,
                                getruntimeidentifier(),
                                '--no-restore'
                                )
            self._setstartuppath(startup.bin_path)

    
    def _setstartuppath(self, path: str):
        self.startuppath = os.path.join(path, "Startup%s" % extension()) 

    def runtests(self, apptorun: str, **kwargs):
        '''
        Runs tests through startup
        '''
        for key in ['startupmetric', 'guiapp']:
            if not kwargs[key]:
                raise Exception('startup tests require %s' % key)
        reportjson = os.path.join(TRACEDIR, 'perf-lab-report.json')
        defaultiterations = '1' if runninginlab() and not uploadtokenpresent() else '5' # only run 1 iteration for PR-triggered build
        startup_args = [
            self.startuppath,
            '--app-exe', apptorun,
            '--metric-type', kwargs['startupmetric'], 
            '--trace-name', '%s_startup' % (kwargs['scenarioname'] or '%s_%s' % (kwargs['exename'],kwargs['scenariotypename'])),
            '--process-will-exit', (kwargs['processwillexit'] or 'true'),
            '--iterations', '%s' % (kwargs['iterations'] or defaultiterations),
            '--timeout', '%s' % (kwargs['timeout'] or '50'),
            '--warmup', '%s' % (kwargs['warmup'] or 'true'),
            '--gui-app', kwargs['guiapp'],
            '--working-dir', '%s' % (kwargs['workingdir'] or sys.path[0]),
            '--report-json-path', reportjson,
            '--trace-directory', TRACEDIR
        ]
        # optional arguments
        if kwargs['scenarioname']:
            startup_args.extend(['--scenario-name', kwargs['scenarioname']])
        if kwargs['appargs']:
            startup_args.extend(['--app-args', kwargs['appargs']])
        if kwargs['environmentvariables']:
            startup_args.extend(['--environment-variables', kwargs['environmentvariables']])
        if kwargs['iterationsetup']:
            startup_args.extend(['--iteration-setup', kwargs['iterationsetup']])
        if kwargs['setupargs']:
            startup_args.extend(['--setup-args', kwargs['setupargs']])
        if kwargs['iterationcleanup']:
            startup_args.extend(['--iteration-cleanup', kwargs['iterationcleanup']])
        if kwargs['cleanupargs']:
            startup_args.extend(['--cleanup-args', kwargs['cleanupargs']])
        if kwargs['measurementdelay']:
            startup_args.extend(['--measurement-delay', kwargs['measurementdelay']])
            
        RunCommand(startup_args, verbose=True).run()


        if runninginlab():
            copytree(TRACEDIR, os.path.join(helixuploaddir(), 'traces'))
            if uploadtokenpresent():
                import upload
                upload.upload(reportjson, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_TOKEN_VAR, UPLOAD_STORAGE_URI)
