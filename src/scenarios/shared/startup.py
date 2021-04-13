'''
Wrapper around startup tool.
'''
import sys
import os
import platform
from shutil import copytree
from performance.logger import setup_loggers
from performance.common import helixpayload, get_artifacts_directory, get_packages_directory, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_TOKEN_VAR, UPLOAD_QUEUE
from dotnet import CSharpProject, CSharpProjFile
from shared.util import extension, helixworkitempayload, helixuploaddir, builtexe, publishedexe, runninginlab, uploadtokenpresent, getruntimeidentifier, iswin
from shared.const import *
from shared.testtraits import TestTraits
from subprocess import CalledProcessError
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

    def runtests(self, traits: TestTraits):
        '''
        Runs tests through startup
        '''
        # make sure required arguments are present
        for key in ['apptorun', 'startupmetric', 'guiapp']:
            if not getattr(traits, key):
                raise Exception('startup tests require %s' % key)
        reportjson = os.path.join(TRACEDIR, 'perf-lab-report.json')
        defaultiterations = '1' if runninginlab() and not uploadtokenpresent() else '5' # only run 1 iteration for PR-triggered build
        # required arguments & optional arguments with default values
        startup_args = [
            self.startuppath,
            '--app-exe', traits.apptorun,
            '--metric-type', traits.startupmetric, 
            '--trace-name', '%s_startup' % (traits.scenarioname or '%s_%s' % (traits.exename,traits.scenariotypename)),
            '--gui-app', traits.guiapp,
            '--process-will-exit', (traits.processwillexit or 'true'),
            '--iterations', '%s' % (traits.iterations or defaultiterations),
            '--timeout', '%s' % (traits.timeout or '50'),
            '--warmup', '%s' % (traits.warmup or 'true'),
            '--working-dir', '%s' % (traits.workingdir or sys.path[0]),
            '--report-json-path', reportjson,
            '--trace-directory', TRACEDIR
        ]
        # optional arguments without default values
        if traits.scenarioname:
            startup_args.extend(['--scenario-name', traits.scenarioname])
        if traits.appargs:
            startup_args.extend(['--app-args', traits.appargs])
        if traits.environmentvariables:
            startup_args.extend(['--environment-variables', traits.environmentvariables])
        if traits.iterationsetup:
            startup_args.extend(['--iteration-setup', traits.iterationsetup])
        if traits.setupargs:
            startup_args.extend(['--setup-args', traits.setupargs])
        if traits.iterationcleanup:
            startup_args.extend(['--iteration-cleanup', traits.iterationcleanup])
        if traits.cleanupargs:
            startup_args.extend(['--cleanup-args', traits.cleanupargs])
        if traits.measurementdelay:
            startup_args.extend(['--measurement-delay', traits.measurementdelay])
        if traits.skipprofile:
            startup_args.extend(['--skip-profile-iteration'])
        if traits.innerloopcommand:
            startup_args.extend(['--inner-loop-command', traits.innerloopcommand])
        if traits.innerloopcommandargs:
            startup_args.extend(['--inner-loop-command-args', traits.innerloopcommandargs])
        if traits.runwithoutexit:
            startup_args.extend(['--run-without-exit', traits.runwithoutexit])
        if traits.hotreloaditers:
            startup_args.extend(['--hot-reload-iters', traits.hotreloaditers])
        if traits.skipmeasurementiteration:
            startup_args.extend(['--skip-measurement-iteration', traits.skipmeasurementiteration])
            
        upload_container = UPLOAD_CONTAINER

        try:
            RunCommand(startup_args, verbose=True).run()
        except CalledProcessError:
            upload_container = 'failedresults'
            reportjson = os.path.join(
                TRACEDIR,
                'FailureReporter', 
                'failure-report.json')
            cmdline = [
                'FailureReporting.exe', reportjson
            ]
            reporterpath = os.path.join(helixpayload(), 'FailureReporter')
            if not os.path.exists(reporterpath):
                raise FileNotFoundError
            RunCommand(cmdline, verbose=True).run(reporterpath)

        if runninginlab():
            copytree(TRACEDIR, os.path.join(helixuploaddir(), 'traces'))
            if uploadtokenpresent():
                import upload
                upload.upload(reportjson, upload_container, UPLOAD_QUEUE, UPLOAD_TOKEN_VAR, UPLOAD_STORAGE_URI)
