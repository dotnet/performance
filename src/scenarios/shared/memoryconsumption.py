'''
Wrapper around memoryconsumption tool.
'''
import sys
import os
from logging import getLogger
from shutil import copytree
from performance.common import extension, helixpayload, runninginlab, get_artifacts_directory, get_packages_directory, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_QUEUE
from dotnet import CSharpProject, CSharpProjFile
from shared.util import helixworkitempayload, helixuploaddir, getruntimeidentifier
from shared.const import *
from shared.testtraits import TestTraits
from subprocess import CalledProcessError
class MemoryConsumptionWrapper(object):
    '''
    Wraps memoryconsumption.exe, building it if necessary.
    '''
    def __init__(self):
        memoryconsumptiondir = 'memoryconsumption'
        self.reportjson = os.path.join(TRACEDIR, 'perf-lab-report.json')
        if helixpayload() and os.path.exists(os.path.join(helixpayload(), memoryconsumptiondir)):
            self._setmemoryconsumptionpath(os.path.join(helixpayload(), memoryconsumptiondir))
        elif helixworkitempayload() and os.path.exists(os.path.join(helixworkitempayload(), memoryconsumptiondir)):
            self._setmemoryconsumptionpath(os.path.join(helixworkitempayload(), memoryconsumptiondir))
        else:
            relpath = os.path.join(get_artifacts_directory(), memoryconsumptiondir)
            memoryconsumptionproj = os.path.join('..',
                                       '..',
                                       'tools',
                                       'ScenarioMeasurement',
                                       'MemoryConsumption',
                                       'MemoryConsumption.csproj')
            memoryconsumption = CSharpProject(CSharpProjFile(memoryconsumptionproj,
                                                   sys.path[0]),
                                                   os.path.join(os.path.dirname(memoryconsumptionproj),
                                                   os.path.join(get_artifacts_directory(), memoryconsumptiondir)))
            if not os.path.exists(relpath):
                memoryconsumption.restore(get_packages_directory(),
                                True,
                                getruntimeidentifier())
                memoryconsumption.publish('Release',
                                relpath,
                                True,
                                get_packages_directory(),
                                None,
                                getruntimeidentifier(),
                                None,
                                '--no-restore'
                                )
            self._setmemoryconsumptionpath(memoryconsumption.bin_path)

    
    def _setmemoryconsumptionpath(self, path: str):
        self.memoryconsumptionpath = os.path.join(path, "memoryconsumption%s" % extension()) 

    def parsetraces(self, traits: TestTraits):
        directory = TRACEDIR
        if traits.tracefolder:
            directory = TRACEDIR + '/' + traits.tracefolder
            getLogger().info("Parse Directory: " + directory)

        memoryconsumption_args = [
            self.memoryconsumptionpath,
            '--app-exe', traits.apptorun,
            '--parse-only',
            '--memory-metric-type', traits.memoryconsumptionmetric, 
            '--trace-name', traits.tracename,
            '--report-json-path', self.reportjson,
            '--trace-directory', directory
        ]
        if traits.scenarioname:
            memoryconsumption_args.extend(['--scenario-name', traits.scenarioname])

        upload_container = UPLOAD_CONTAINER

        try:
            RunCommand(memoryconsumption_args, verbose=True).run()
        except CalledProcessError:
            getLogger().info("Run failure registered")
            # rethrow the original exception 
            raise

        helix_upload_dir = helixuploaddir()
        if runninginlab() and helix_upload_dir is not None:
            copytree(TRACEDIR, os.path.join(helix_upload_dir, 'traces'))
            if traits.upload_to_perflab_container:
                import upload
                upload.upload(self.reportjson, upload_container, UPLOAD_QUEUE, UPLOAD_STORAGE_URI)

    def runtests(self, traits: TestTraits):
        '''
        Runs tests through memoryconsumption
        '''
        # make sure required arguments are present
        for key in ['apptorun', 'memoryconsumptionmetric', 'guiapp']:
            if not getattr(traits, key):
                raise Exception('memoryconsumption tests require %s' % key)
        
        defaultiterations = '1' if runninginlab() and not traits.upload_to_perflab_container else '5' # only run 1 iteration for PR-triggered build
        # required arguments & optional arguments with default values
        memoryconsumption_args = [
            self.memoryconsumptionpath,
            '--app-exe', traits.apptorun,
            '--memory-metric-type', traits.memoryconsumptionmetric, 
            '--trace-name', '%s_memoryconsumption' % (traits.scenarioname or '%s_%s' % (traits.exename,traits.scenariotypename)),
            '--gui-app', traits.guiapp,
            '--process-will-exit', (traits.processwillexit or 'true'),
            '--iterations', '%s' % (traits.iterations or defaultiterations),
            '--timeout', '%s' % (traits.timeout or '50'),
            '--warmup', '%s' % (traits.warmup or 'true'),
            '--working-dir', '%s' % (traits.workingdir or sys.path[0]),
            '--report-json-path', self.reportjson,
            '--trace-directory', TRACEDIR
        ]
        # optional arguments without default values
        if traits.scenarioname:
            memoryconsumption_args.extend(['--scenario-name', traits.scenarioname])
        if traits.appargs:
            memoryconsumption_args.extend(['--app-args', traits.appargs])
        if traits.environmentvariables:
            memoryconsumption_args.extend(['--environment-variables', traits.environmentvariables])
        if traits.iterationsetup:
            memoryconsumption_args.extend(['--iteration-setup', traits.iterationsetup])
        if traits.setupargs:
            memoryconsumption_args.extend(['--setup-args', traits.setupargs])
        if traits.iterationcleanup:
            memoryconsumption_args.extend(['--iteration-cleanup', traits.iterationcleanup])
        if traits.cleanupargs:
            memoryconsumption_args.extend(['--cleanup-args', traits.cleanupargs])
        if traits.measurementdelay:
            memoryconsumption_args.extend(['--measurement-delay', traits.measurementdelay])
        if traits.skipprofile:
            memoryconsumption_args.extend(['--skip-profile-iteration'])
        if traits.innerloopcommand:
            memoryconsumption_args.extend(['--inner-loop-command', traits.innerloopcommand])
        if traits.innerloopcommandargs:
            memoryconsumption_args.extend(['--inner-loop-command-args', traits.innerloopcommandargs])
        if traits.runwithoutexit:
            memoryconsumption_args.extend(['--run-without-exit', traits.runwithoutexit])
        if traits.hotreloaditers:
            memoryconsumption_args.extend(['--hot-reload-iters', traits.hotreloaditers])
        if traits.skipmeasurementiteration:
            memoryconsumption_args.extend(['--skip-measurement-iteration', traits.skipmeasurementiteration])
        if traits.runwithdotnet:
            memoryconsumption_args.extend(['--run-with-dotnet'])
            
        upload_container = UPLOAD_CONTAINER

        try:
            RunCommand(memoryconsumption_args, verbose=True).run()
        except CalledProcessError:
            getLogger().info("Run failure registered")
            # rethrow the original exception 
            raise

        helix_upload_dir = helixuploaddir()
        if runninginlab() and helix_upload_dir is not None:
            copytree(TRACEDIR, os.path.join(helix_upload_dir, 'traces'))
            if traits.upload_to_perflab_container:
                import upload
                upload_code = upload.upload(self.reportjson, upload_container, UPLOAD_QUEUE, UPLOAD_STORAGE_URI)
                getLogger().info("memoryconsumption Upload Code: " + str(upload_code))
                if upload_code != 0:
                    sys.exit(upload_code)
