'''
Helper/Runner for Android Instrumentation Scenarios tool.
'''
import glob
import re
import sys
import os
from logging import getLogger
from shutil import copytree
import time
from performance.common import extension, helixpayload, runninginlab, get_artifacts_directory, get_packages_directory, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_TOKEN_VAR, UPLOAD_QUEUE
from dotnet import CSharpProject, CSharpProjFile
from shared import const
from shared.androidhelper import AndroidHelper
from shared.util import helixworkitempayload, helixuploaddir, uploadtokenpresent, getruntimeidentifier, xharnesscommand
from shared.const import *
from shared.testtraits import TestTraits
from subprocess import CalledProcessError

class DevicePowerConsumptionHelper(object):
    '''
    Wraps powerconsumption.exe, building it if necessary.
    '''
    #def __init__(self):
        # powerconsumptiondir = 'powerconsumption'
        # self.reportjson = os.path.join(TRACEDIR, 'perf-lab-report.json')
        # if helixpayload() and os.path.exists(os.path.join(helixpayload(), powerconsumptiondir)):
        #     self._setpowerconsumptionpath(os.path.join(helixpayload(), powerconsumptiondir))
        # elif helixworkitempayload() and os.path.exists(os.path.join(helixworkitempayload(), powerconsumptiondir)):
        #     self._setpowerconsumptionpath(os.path.join(helixworkitempayload(), powerconsumptiondir))
        # else:
        #     relpath = os.path.join(get_artifacts_directory(), powerconsumptiondir)
        #     powerconsumptionproj = os.path.join('..',
        #                                '..',
        #                                'tools',
        #                                'ScenarioMeasurement',
        #                                'PowerConsumption',
        #                                'PowerConsumption.csproj')
        #     powerconsumption = CSharpProject(CSharpProjFile(powerconsumptionproj,
        #                                            sys.path[0]),
        #                                            os.path.join(os.path.dirname(powerconsumptionproj),
        #                                            os.path.join(get_artifacts_directory(), powerconsumptiondir)))
        #     if not os.path.exists(relpath):
        #         powerconsumption.restore(get_packages_directory(),
        #                         True,
        #                         getruntimeidentifier())
        #         powerconsumption.publish('Release',
        #                         relpath,
        #                         True,
        #                         get_packages_directory(),
        #                         None,
        #                         getruntimeidentifier(),
        #                         None,
        #                         '--no-restore'
        #                         )
        #     self._setpowerconsumptionpath(powerconsumption.bin_path)
  
    def _setpowerconsumptionpath(self, path: str):
        self.powerconsumptionpath = os.path.join(path, "powerconsumption%s" % extension()) 

    def parsetraces(self, traits: TestTraits):
        directory = TRACEDIR
        if traits.tracefolder:
            directory = TRACEDIR + '/' + traits.tracefolder
            getLogger().info("Parse Directory: " + directory)

        powerconsumption_args = [
            self.powerconsumptionpath,
            '--app-exe', traits.apptorun,
            '--parse-only',
            '--power-metric-type', traits.powerconsumptionmetric, 
            '--trace-name', traits.tracename,
            '--report-json-path', self.reportjson,
            '--trace-directory', directory
        ]
        if traits.scenarioname:
            powerconsumption_args.extend(['--scenario-name', traits.scenarioname])

        upload_container = UPLOAD_CONTAINER

        try:
            RunCommand(powerconsumption_args, verbose=True).run()
        except CalledProcessError:
            getLogger().info("Run failure registered")
            # rethrow the original exception 
            raise

        if runninginlab():
            copytree(TRACEDIR, os.path.join(helixuploaddir(), 'traces'))
            if uploadtokenpresent():
                import upload
                upload.upload(self.reportjson, upload_container, UPLOAD_QUEUE, UPLOAD_TOKEN_VAR, UPLOAD_STORAGE_URI)

    def runtestsandroid(self, packagepath: str, packagename: str, testiterations: int, runtimeseconds: int, closeToStartDelay: int, traits: TestTraits):
        getLogger().info("Clearing potential previous run nettraces")
        for file in glob.glob(os.path.join(const.TRACEDIR, 'PerfTest', 'runoutput.trace')):
            if os.path.exists(file):   
                getLogger().info("Removed: " + os.path.join(const.TRACEDIR, file))
                os.remove(file)

        androidHelper = AndroidHelper()
        try:
            androidHelper.setup_device(packagename, packagepath, False, False)

            listYepkitBoardsCmd = [
                'ykushcmd',
                'ykushxs',
                '-l'
            ]

            # Create the fullydrawn command
            clearBatteryStatsCmd = [ 
                androidHelper.adbpath,
                'shell',
                'dumpsys',
                'batterystats',
                '--reset'
            ]

            disconnectYepKitPowerCmd = [
                'ykushcmd',
                'ykushxs',
                '-d'
            ]

            reconnectYepKitPowerCmd = [
                'ykushcmd',
                'ykushxs',
                '-u'
            ]

            captureBatteryStatsCmd = [ 
                androidHelper.adbpath,
                'shell',
                'dumpsys',
                'batterystats',
                '--charged',
                packagename,
            ]

            clearLogsCmd = [
                androidHelper.adbpath,
                'logcat',
                '-c'
            ]

            allResults = []
            # Verify that a yepkit board is found
            yepkitCheck = RunCommand(listYepkitBoardsCmd, verbose=True)
            yepkitCheck.run()
            if "Board found with serial number" not in yepkitCheck.stdout:
                raise EnvironmentError("Yepkit board not connected.")
            RunCommand(reconnectYepKitPowerCmd, verbose=True).run() # Make sure the board is connected
            
            for i in range(testiterations):
                # Clear logs
                RunCommand(clearLogsCmd, verbose=True).run()
                RunCommand(clearBatteryStatsCmd, verbose=True).run()
                startStats = RunCommand(androidHelper.startappcommand, verbose=True)
                startStats.run()
                RunCommand(disconnectYepKitPowerCmd, verbose=True).run()
                time.sleep(runtimeseconds)
                RunCommand(reconnectYepKitPowerCmd, verbose=True).run()
                time.sleep(5) # Wait for the phone to reconnect to the commputer
                captureProcStats = RunCommand(captureBatteryStatsCmd, verbose=True)
                captureProcStats.run()

                # Save the results and get them from the log
                RunCommand(androidHelper.stopappcommand, verbose=True).run()
                
                # # Part of the output we are regexing:
                # # Process summary:
                # # * net.dot.HelloAndroid / u0a1219 / v1:
                # #        TOTAL: ###% (<Part we want>52MB-52MB-52MB/44MB-44MB-44MB/135MB-135MB-135MB over 1</Part we want>)
                # #        Top: 100% (52MB-52MB-52MB/44MB-44MB-44MB/135MB-135MB-135MB over 1)
                # regexSearchString = r"TOTAL: [0-9]{2,3}% \((\d+MB-\d+MB-\d+MB\/\d+MB-\d+MB-\d+MB\/\d+MB-\d+MB-\d+MB over \d+)\)"
                # dirtyCapture = re.search(regexSearchString, captureProcStats.stdout)
                # if not dirtyCapture:
                #     raise Exception("Failed to capture the reported start time!")
                # splitNumber = dirtyCapture.group(1).replace("MB", "").strip().split(" over ")
                # splitMemory = splitNumber[0].split("/")
                # pss = splitMemory[0].split("-")
                # uss = splitMemory[1].split("-")
                # rss = splitMemory[2].split("-")
                # memoryCapture = f"PSS: min {pss[0]}, avg {pss[1]}, max {pss[2]}; USS: min {uss[0]}, avg {uss[1]}, max {uss[2]}; RSS: min {rss[0]}, avg {rss[1]}, max {rss[2]}; Number: {splitNumber[1]}\n"
                # print(f"Memory Capture: {memoryCapture}")
                # allResults.append(memoryCapture)
                time.sleep(closeToStartDelay) # Delay in seconds for ensuring a cold start
                
        finally:
            androidHelper.close_device()

        # Create traces to store the data so we can keep the current general parse trace flow
        getLogger().info(f"Logs: \n{allResults}")
        outputdir = os.path.join(const.TRACEDIR,"PerfTest")
        os.makedirs(outputdir, exist_ok=True)
        outputtracefile = os.path.join(outputdir, "runoutput.trace")
        tracefile = open(outputtracefile, "w")
        for result in allResults:
            tracefile.write(result)
        tracefile.close()

        # self.traits.add_traits(overwrite=True, apptorun="app", powerconsumptionmetric=const.POWERCONSUMPTION_ANDROID, tracefolder='PerfTest/', tracename='runoutput.trace', scenarioname=self.scenarioname)
        # self.parsetraces(self.traits)

    def runtests(self, devicetype: str, packagepath: str, packagename: str, testiterations: int, runtimeseconds: int, closeToStartDelay: int, traits: TestTraits):
        '''
        Runs Device Power Consumption tests.
        '''
        try:
            if devicetype == 'android':
                self.runtestsandroid(packagepath, packagename, testiterations, runtimeseconds, closeToStartDelay, traits)

        except CalledProcessError:
            getLogger().info("Run failure registered")
            # rethrow the original exception 
            raise
