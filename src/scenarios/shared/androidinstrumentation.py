'''
Helper/Runner for Android Instrumentation Scenarios tool.
'''
import sys
import os
import json
from logging import getLogger
from shutil import copytree
from performance.common import runninginlab, RunCommand
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_QUEUE
from shared.util import helixuploaddir, xharnesscommand, xharness_adb
from shared.const import *
from subprocess import CalledProcessError

class AndroidInstrumentationHelper(object):

    def runtests(self, packagepath: str, packagename: str, instrumentationname: str, upload_to_perflab_container: bool):
        '''
        Runs Android Instrumentation tests.
        '''
        try:
            # Try calling xharness with stdout=None and stderr=None to hopefully bypass the hang
            getLogger().info("Clearing xharness stdout and stderr to avoid hang")
            cmdline = xharness_adb() + [
                'shell',
                'echo', 'Hello World'
            ]
            RunCommand(cmdline, verbose=False).run()
            getLogger().info("Ran echo command to clear stdout and stderr")

            installCmd = xharness_adb() + [
                'install',
                packagepath,
            ]
            
            clearLogsCmd = xharness_adb() + [
                'logcat',
                '-c'
            ]

            startInstrumentationCmd = xharness_adb() + [
                'shell',
                'am',
                'instrument',
                '-w',
                f'{packagename}/{instrumentationname}'
            ]
            
            printMauiLogsCmd = xharness_adb() + [
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
            pullFilesFromDeviceCmd = xharness_adb() + [
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

        except CalledProcessError:
            getLogger().info("Run failure registered")
            # rethrow the original exception
            raise

        finally:
            getLogger().info("Uninstalling app")
            uninstallAppCmd = xharnesscommand() + [
                'android',
                'uninstall',
                '--package-name',
                packagename
            ]
            RunCommand(uninstallAppCmd, verbose=True).run()

        helix_upload_dir = helixuploaddir()
        if runninginlab() and helix_upload_dir is not None:
            copytree(TRACEDIR, os.path.join(helix_upload_dir, 'traces'))
            if upload_to_perflab_container:
                import upload
                globpath = os.path.join(
                    TRACEDIR,
                    '**',
                    '*perf-lab-report.json')
                upload_code = upload.upload(globpath, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_STORAGE_URI)
                getLogger().info("Device Benchmarks Upload Code: " + str(upload_code))
                if upload_code != 0:
                    sys.exit(upload_code)
