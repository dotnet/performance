'''
Helper/Runner for Android Instrumentation Scenarios tool.
'''
import glob
import re
import sys
import os
import json
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

            clearLogsCmd = [
                androidHelper.adbpath,
                'logcat',
                '-c'
            ]

            getUidOfPackageCmd = [
                androidHelper.adbpath,
                'shell',
                'cmd',
                'package',
                'list',
                'packages',
                '-U',
                packagename
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
                RunCommand(androidHelper.stopappcommand, verbose=True).run()
                
                captureUid = RunCommand(getUidOfPackageCmd, verbose=True)
                captureUid.run()

                # Get the Uid from the captureUid output
                # The output format and target are as follows:
                # package:com.companyname.mauiandroiddefault uid:1<capture start>1453<end capture>
                uidSearchString = r"package:" + packagename + r" uid:([0-9]*)"
                uidCapture = re.search(uidSearchString, captureUid.stdout)
                if not uidCapture:
                    raise Exception("Failed to capture the uid!")
                uid = uidCapture.group(1)

                # Include the uid filter to keep the output smaller
                captureBatteryStatsCmd = [
                    androidHelper.adbpath,
                    'shell',
                    'dumpsys',
                    'batterystats',
                    '--charged',
                    packagename,
                    '-c',
                    '|',
                    'grep',
                    uid               
                ]
                
                captureProcStats = RunCommand(captureBatteryStatsCmd, verbose=True)
                captureProcStats.run()

                capturedValues = {}
                # Get the mAh estimated power use based on the Uid from the battery stats
                # Explanation of the 4 groups: https://stackoverflow.com/questions/75390939/android-how-to-interpret-pwi-power-use-item-from-battery-stats-dumpsys
                # Example output section and target:
                # 9,11458,l,pwi,uid,0.0820,0,0.134,0.0342 captures total usage, is battery consumer (consult link below), screen usage, and proportional usage
                totalPowerSearchString = r"[\d]*," + uid + r",l,pwi,uid,([\d]*.?[\d]*),([\d]*.?[\d]*),([\d]*.?[\d]*),([\d]*.?[\d]*)"
                totalPowerCapture = re.search(totalPowerSearchString, captureProcStats.stdout)
                if not totalPowerCapture:
                    raise Exception("Failed to capture the total power!")
                capturedValues["totalPowermAh"] = totalPowerCapture.group(1)
                capturedValues["isSystemBatteryConsumer"] = totalPowerCapture.group(2)
                capturedValues["screenPowermAh"] = totalPowerCapture.group(3)
                capturedValues["proportionalPowermAh"] = totalPowerCapture.group(4)
                
                # Get timing information
                # 9,11458,l,fg,10294,0 # Foreground Time (ms, count)
                # Total cpu time: u=567ms s=167ms               #(user and system/kernel?)
                foregroundTimeSearchString = r"[\d]*," + uid + r",l,fg,([\d]*),([\d]*)"
                foregroundTimeCapture = re.search(foregroundTimeSearchString, captureProcStats.stdout)
                if not foregroundTimeCapture:
                    raise Exception("Failed to capture the foreground running time!")
                capturedValues["foregroundTimeMs"] = foregroundTimeCapture.group(1)
                capturedValues["foregroundTimeCount"] = foregroundTimeCapture.group(2)

                # Get timing information https://cs.android.com/android/platform/superproject/+/master:frameworks/base/core/java/android/os/BatteryStats.java;l=4894
                # 9,11458,l,cpu,668,155,0 # cpu time (user, system, 0)
                cpuTimeSearchString = r"[\d]*," + uid + r",l,cpu,([\d]*),([\d]*),[\d]*"
                cpuTimeCapture = re.search(cpuTimeSearchString, captureProcStats.stdout)
                if not cpuTimeCapture:
                    raise Exception("Failed to capture the cpu running time!")
                capturedValues["cpuTimeUserMs"] = cpuTimeCapture.group(1)
                capturedValues["cpuTimeSystemMs"] = cpuTimeCapture.group(2)

                allResults.append(json.dumps(capturedValues))
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
