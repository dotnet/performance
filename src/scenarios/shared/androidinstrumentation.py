'''
Helper/Runner for Android Instrumentation Scenarios tool.
'''
import time
import sys
import os
import json
from logging import getLogger
from shutil import copytree
from performance.common import runninginlab, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_TOKEN_VAR, UPLOAD_QUEUE
from shared.util import helixuploaddir, uploadtokenpresent, xharnesscommand
from shared.const import *
from subprocess import CalledProcessError

class AndroidInstrumentationHelper(object):

    def runtests(self, packagepath: str, packagename: str, instrumentationname: str):
        '''
        Runs Android Instrumentation tests.
        '''
        try:
            # Do the actual work here
            cmdline = xharnesscommand() + ['android', 'state', '--adb']
            adb = RunCommand(cmdline, verbose=True)
            adb.run()

            # Do not remove, XHarness install seems to fail without an adb command called before the xharness command
            getLogger().info("Preparing ADB")
            adbpath = adb.stdout.strip()
            try:
                installCmd = [
                    adbpath,
                    'install',
                    packagepath,
                ]
                
                clearLogsCmd = [
                    adbpath,
                    'logcat',
                    '-c'
                ]

                startInstrumentationCmd = [
                    adbpath,
                    'shell',
                    'am',
                    'instrument',
                    '-w',
                    f'{packagename}/{instrumentationname}'
                ]
                
                printMauiLogsCmd = [
                    adbpath,
                    'shell',
                    'logcat',
                    '-d',
                    'v',
                    'tag',
                    '-s',
                    '"DOTNET,MAUI"'
                ]

                getLogger().info("Installing APK")
                RunCommand(installCmd, verbose=True).run()

                # Clear logs
                getLogger().info("Clearing logs")
                RunCommand(clearLogsCmd, verbose=True).run()

                # Run instrumentation
                getLogger().info("Running instrumentation")
                RunCommand(startInstrumentationCmd, verbose=True).run()
                
                # Print logs
                getLogger().info("Printing logs")
                RunCommand(printMauiLogsCmd, verbose=True).run()
                
                ## Get logs off device and upload to helix (TODO: Make this optional, potentially add different methods of getting logs)
                defaultDeviceBdnOutputDir = f'/sdcard/Android/data/{packagename}/files/'
                pullFilesFromDeviceCmd = [
                    adbpath,
                    'pull',
                    defaultDeviceBdnOutputDir,
                    TRACEDIR
                ]

                RunCommand(pullFilesFromDeviceCmd, verbose=True).run()

                # Replace the JSON with the correct values
                for (root, dirs, files) in os.walk(TRACEDIR):
                    for file in files:
                        if 'perf-lab-report.json' in file:
                            filePath = os.path.join(root, file)
                            print(file + " found at " + filePath)
                            # Read the file and change the values
                            with open(filePath, 'r') as jsonFile:
                                data = json.load(jsonFile)
                                data['build']['repo'] = os.environ.get('PERFLAB_REPO', "")
                                data['build']['branch'] = os.environ.get('PERFLAB_BRANCH', "")
                                data['build']['architecture'] = os.environ.get('PERFLAB_BUILDARCH', "")
                                data['build']['locale'] = os.environ.get('PERFLAB_LOCALE', "")
                                data['build']['gitHash'] = os.environ.get('PERFLAB_HASH', "")
                                data['build']['buildName'] = os.environ.get('PERFLAB_BUILDNUM', "")
                                data['build']['timeStamp'] = os.environ.get('PERFLAB_BUILDTIMESTAMP', "")
                                data['build']['additionalData']['productVersion'] = os.environ.get('DOTNET_VERSION', "")
                                data['os']['name'] = "Android"
                                data['os']['machineName'] = "Android"
                                data['run']['correlationId'] = os.environ.get('HELIX_CORRELATION_ID', "")
                                data['run']['perfRepoHash'] = os.environ.get('PERFLAB_PERFHASH', "")
                                data['run']['name'] = os.environ.get('PERFLAB_RUNNAME', "")
                                data['run']['queue'] = os.environ.get('PERFLAB_QUEUE', "")
                                data['run']['workItemName'] = os.environ.get('HELIX_WORKITEM_FRIENDLYNAME', "")
                                configs = os.environ.get("PERFLAB_CONFIGS", "")
                                if configs != "":
                                    for kvp in configs.split(';'):
                                        split = kvp.split('=')
                                        data['run']['configurations'][split[0]] = split[1]
                            # write the new json
                            os.remove(filePath)
                            with open(filePath, 'w') as jsonFile:
                                json.dump(data, jsonFile, indent=4)            
            finally:
                getLogger().info("Uninstalling app")
                uninstallAppCmd = xharnesscommand() + [
                    'android',
                    'uninstall',
                    '--package-name',
                    packagename
                ]
                RunCommand(uninstallAppCmd, verbose=True).run()

        except CalledProcessError:
            getLogger().info("Run failure registered")
            # rethrow the original exception 
            raise

        helix_upload_dir = helixuploaddir()
        if runninginlab() and helix_upload_dir is not None:
            copytree(TRACEDIR, os.path.join(helix_upload_dir, 'traces'))
            if uploadtokenpresent():
                import upload
                globpath = os.path.join(
                    TRACEDIR,
                    '**',
                    '*perf-lab-report.json')
                upload_code = upload.upload(globpath, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_TOKEN_VAR, UPLOAD_STORAGE_URI)
                getLogger().info("Device Benchmarks Upload Code: " + str(upload_code))
                if upload_code != 0:
                    sys.exit(upload_code)
