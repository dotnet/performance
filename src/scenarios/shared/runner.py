'''
Module for running scenario tasks
'''

import sys
import os
import glob
import re
import time
import json

from genericpath import exists
from datetime import datetime, timedelta
from logging import exception, getLogger
from collections import namedtuple
from argparse import ArgumentParser
from argparse import RawTextHelpFormatter
from io import StringIO
from shutil import move, rmtree, copytree
from shared.androidhelper import AndroidHelper
from shared.androidinstrumentation import AndroidInstrumentationHelper
from shared.devicepowerconsumption import DevicePowerConsumptionHelper
from shared.crossgen import CrossgenArguments
from shared.startup import StartupWrapper
from shared.memoryconsumption import MemoryConsumptionWrapper
from shared.util import publishedexe, pythoncommand, appfolder, xharnesscommand, publisheddll, helixuploaddir
from shared.sod import SODWrapper
from shared import const
from performance.common import RunCommand, iswin, extension, helixworkitemroot, runninginlab
from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_TOKEN_VAR, UPLOAD_QUEUE
from performance.logger import setup_loggers
from shared.testtraits import TestTraits, testtypes
from subprocess import CalledProcessError


class Runner:
    '''
    Wrapper for running all the things
    '''

    def __init__(self, traits: TestTraits):
        self.traits = traits
        self.testtype = None
        self.sdktype = None
        self.scenarioname = None
        self.coreroot = None
        self.crossgenfile = None
        self.dirs = None
        self.crossgen_arguments = CrossgenArguments()
        self.affinity = None
        setup_loggers(True)

    def parseargs(self):
        '''
        Parses input args to the script
        '''
        parser = ArgumentParser(description='test.py runs the test with specified commands. Usage: test.py <command> <optional subcommands> <options>',
                                formatter_class=RawTextHelpFormatter)
        subparsers = parser.add_subparsers(title='subcommands for scenario tests', 
                                           dest='testtype')

        # startup command
        startupparser = subparsers.add_parser(const.STARTUP,
                                              description='measure time to main of running the project')
        self.add_affinity_argument(startupparser)
        self.add_common_arguments(startupparser)

        # parse only command
        devicestartupparser = subparsers.add_parser(const.DEVICESTARTUP,
                                              description='measure time to startup for Android/iOS apps')
        devicestartupparser.add_argument('--device-type', choices=['android','ios'],type=str.lower,help='Device type for testing', dest='devicetype')
        devicestartupparser.add_argument('--package-path', help='Location of test application', dest='packagepath')
        devicestartupparser.add_argument('--package-name', help='Classname (Android) or Bundle ID (iOS) of application', dest='packagename')
        devicestartupparser.add_argument('--startup-iterations', help='Startups to run (1+)', type=int, default=10, dest='startupiterations')
        devicestartupparser.add_argument('--disable-animations', help='Disable Android device animations, does nothing on iOS.', action='store_true', dest='animationsdisabled')
        devicestartupparser.add_argument('--use-fully-drawn-time', help='Use the startup time from reportFullyDrawn for android, the equivalent for iOS is handled via logging a magic string and passing it to --fully-drawn-magic-string', action='store_true', dest='usefullydrawntime')
        devicestartupparser.add_argument('--fully-drawn-extra-delay', help='Set an additional delay time for an Android app to reportFullyDrawn (seconds), not on iOS. This should be greater than the greatest amount of extra time expected between first frame draw and reportFullyDrawn being called. Default = 3 seconds', type=int, default=3, dest='fullyDrawnDelaySecMax')
        devicestartupparser.add_argument('--fully-drawn-magic-string', help='Set the magic string that is logged by the app to indicate when the app is fully drawn. Required when using --use-fully-drawn-time on iOS.', type=str, dest='fullyDrawnMagicString')
        devicestartupparser.add_argument('--time-from-kill-to-start', help='Set an additional delay time for ensuring an app is cleared after closing the app on Android, not on iOS. This should be greater than the greatest amount of expected time needed between closing an app and starting it again for a cold start. Default = 3 seconds', type=int, default=3, dest='closeToStartDelay')
        self.add_common_arguments(devicestartupparser)

        devicememoryconsumptionparser = subparsers.add_parser(const.DEVICEMEMORYCONSUMPTION,
                                              description='measure memory consumption to startup for Android/iOS apps')
        devicememoryconsumptionparser.add_argument('--device-type', choices=['android'],type=str.lower,help='Device type for testing', dest='devicetype')
        devicememoryconsumptionparser.add_argument('--package-path', help='Location of test application', dest='packagepath')
        devicememoryconsumptionparser.add_argument('--package-name', help='Classname (Android) or Bundle ID (iOS) of application', dest='packagename')
        devicememoryconsumptionparser.add_argument('--test-iterations', help='Iterations to run (1+)', type=int, default=1, dest='testiterations')
        devicememoryconsumptionparser.add_argument('--disable-animations', help='Disable Android device animations, does nothing on iOS.', action='store_true', dest='animationsdisabled')
        devicememoryconsumptionparser.add_argument('--runtime', help='Amount of time to run the app between clearing procstats and dumping them', type=int, default=60, dest='runtimeseconds')
        devicememoryconsumptionparser.add_argument('--time-from-kill-to-start', help='Set an additional delay time for ensuring an app is cleared after closing the app on Android, not on iOS. This should be greater than the greatest amount of expected time needed between closing an app and starting it again for a cold start. Default = 3 seconds', type=int, default=3, dest='closeToStartDelay')
        self.add_common_arguments(devicememoryconsumptionparser)

        androidinstrumentationparser = subparsers.add_parser(const.ANDROIDINSTRUMENTATION,
                                              description='Run device BDN instrumentation to startup for Android apps')
        androidinstrumentationparser.add_argument('--package-path', help='Location of test application', dest='packagepath')
        androidinstrumentationparser.add_argument('--package-name', help='Classname (Android) or Bundle ID (iOS) of application', dest='packagename')
        androidinstrumentationparser.add_argument('--instrumentation-name', help='Name of the instrumentation to run', dest='instrumentationname')
        self.add_common_arguments(androidinstrumentationparser)

        devicepowerconsumptionparser = subparsers.add_parser(const.DEVICEPOWERCONSUMPTION,
                                              description='Run device BDN instrumentation to startup for Android apps')
        devicepowerconsumptionparser.add_argument('--device-type', choices=['android'],type=str.lower,help='Device type for testing', dest='devicetype') # choices=['android','ios'] Only android is supported for now
        devicepowerconsumptionparser.add_argument('--package-path', help='Location of test application', dest='packagepath')
        devicepowerconsumptionparser.add_argument('--package-name', help='Classname (Android)', dest='packagename')
        devicepowerconsumptionparser.add_argument('--test-iterations', help='Iterations to run (1+)', type=int, default=4, dest='testiterations')
        devicepowerconsumptionparser.add_argument('--runtime', help='Amount of time to run the app between clearing procstats and dumping them', type=int, default=60, dest='runtimeseconds')
        devicepowerconsumptionparser.add_argument('--time-from-kill-to-start', help='Set an additional delay time for ensuring an app is cleared after closing the app on Android, not on iOS. This should be greater than the greatest amount of expected time needed between closing an app and starting it again for a cold start. Default = 3 seconds', type=int, default=3, dest='closeToStartDelay')
        self.add_common_arguments(devicepowerconsumptionparser)

        # inner loop command
        innerloopparser = subparsers.add_parser(const.INNERLOOP,
                                              description='measure time to main and difference between two runs in a row')
        self.add_affinity_argument(innerloopparser)
        self.add_common_arguments(innerloopparser)

        # inner loop msbuild command
        innerloopparser = subparsers.add_parser(const.INNERLOOPMSBUILD,
                                              description='measure time to main and difference between two runs in a row')
        self.add_affinity_argument(innerloopparser)
        self.add_common_arguments(innerloopparser)

        # dotnet watch command
        dotnetwatchparser = subparsers.add_parser(const.DOTNETWATCH,
                                              description='measure time to main and time for hot reload')
        self.add_affinity_argument(dotnetwatchparser)
        self.add_common_arguments(dotnetwatchparser)

        # sdk command
        sdkparser = subparsers.add_parser(const.SDK, 
                                          description='subcommands for sdk scenario',
                                          formatter_class=RawTextHelpFormatter)
        sdkparser.add_argument('sdktype', 
                                choices=[const.CLEAN_BUILD, const.BUILD_NO_CHANGE, const.NEW_CONSOLE], 
                                type=str.lower,
                                help= 
'''
clean_build:     measure duration of building from source in each iteration
build_no_change: measure duration of building with existing output in each iteration
new_console:     measure duration of creating a new console template
'''
                               )
        self.add_affinity_argument(sdkparser)
        self.add_common_arguments(sdkparser)

        crossgenparser = subparsers.add_parser(const.CROSSGEN,
                                               description='measure duration of the crossgen compilation',
                                               formatter_class=RawTextHelpFormatter)
        self.crossgen_arguments.add_crossgen_arguments(crossgenparser)
        self.add_affinity_argument(crossgenparser)
        self.add_common_arguments(crossgenparser)

        crossgen2parser = subparsers.add_parser(const.CROSSGEN2,
                                                description='measure duration of the crossgen compilation',
                                                formatter_class=RawTextHelpFormatter)
        self.crossgen_arguments.add_crossgen2_arguments(crossgen2parser)
        self.add_affinity_argument(crossgen2parser)
        self.add_common_arguments(crossgen2parser)

        sodparser = subparsers.add_parser(const.SOD,
                                          description='measure size on disk of the specified directory and its children')
        sodparser.add_argument('--dirs', 
                               dest='dirs', 
                               type=str,
                               help=
r'''
directories to measure separated by semicolon
ex: C:\repos\performance;C:\repos\runtime
'''                            )
        self.add_common_arguments(sodparser)

        args = parser.parse_args()

        if not args.testtype:
            getLogger().error("Please specify a test type: %s. Type test.py <test type> -- help for more type-specific subcommands" % testtypes)
            sys.exit(1)

        self.testtype = args.testtype
    
        if self.testtype == const.SDK:
            self.sdktype = args.sdktype

        if self.testtype == const.CROSSGEN:
            self.crossgen_arguments.parse_crossgen_args(args)

        if self.testtype == const.CROSSGEN2:
            self.crossgen_arguments.parse_crossgen2_args(args)

        if self.testtype == const.SOD:
            self.dirs = args.dirs
        
        if self.testtype == const.DEVICESTARTUP:
            self.packagepath = args.packagepath
            self.packagename = args.packagename
            self.devicetype = args.devicetype
            self.startupiterations = args.startupiterations
            self.animationsdisabled = args.animationsdisabled
            self.usefullydrawntime = args.usefullydrawntime
            self.fullyDrawnDelaySecMax = args.fullyDrawnDelaySecMax
            self.fullyDrawnMagicString = args.fullyDrawnMagicString
            self.closeToStartDelay = args.closeToStartDelay

        if self.testtype == const.DEVICEMEMORYCONSUMPTION:
            self.packagepath = args.packagepath
            self.packagename = args.packagename
            self.devicetype = args.devicetype
            self.testiterations = args.testiterations
            self.animationsdisabled = args.animationsdisabled
            self.runtimeseconds = args.runtimeseconds
            self.closeToStartDelay = args.closeToStartDelay

        if self.testtype == const.ANDROIDINSTRUMENTATION:
            self.packagepath = args.packagepath
            self.packagename = args.packagename
            self.instrumentationname = args.instrumentationname

        if self.testtype == const.DEVICEPOWERCONSUMPTION:
            self.devicetype = args.devicetype
            self.packagepath = args.packagepath
            self.packagename = args.packagename
            self.testiterations = args.testiterations
            self.runtimeseconds = args.runtimeseconds
            self.closeToStartDelay = args.closeToStartDelay

        if args.scenarioname:
            self.scenarioname = args.scenarioname

        if self.testtype in [const.STARTUP, const.INNERLOOP, const.INNERLOOPMSBUILD, const.DOTNETWATCH, const.SDK, const.CROSSGEN, const.CROSSGEN2] and (args.affinity or os.environ.get('PERFLAB_DATA_AFFINITY')): # Set affinity if doing a Startup based test
            self.affinity = args.affinity if args.affinity else os.environ.get('PERFLAB_DATA_AFFINITY')


    def add_common_arguments(self, parser: ArgumentParser):
        "Common arguments to add to subparsers"
        parser.add_argument('--scenario-name',
                            dest='scenarioname')
        
    def add_affinity_argument(self, parser: ArgumentParser):
        "Affinity arguments to add to subparsers"
        parser.add_argument('--affinity',
                            dest='affinity',
                            type=str,
                            help='Processor affinity to run the test on. Passed as integer. EX. 1 for first processor, 2 for second processor, 3 for first and second processor, 4 for third processor, etc.')

    def run(self):
        '''
        Runs the specified scenario
        '''
        self.parseargs()
        if self.testtype == const.INNERLOOP:
            startup = StartupWrapper()
            self.traits.add_traits(scenarioname=self.scenarioname,
            scenariotypename=const.SCENARIO_NAMES[const.INNERLOOP],
            apptorun='dotnet', appargs='run --project %s' % appfolder(self.traits.exename, self.traits.projext),
            innerloopcommand=pythoncommand(),
            iterationsetup=pythoncommand(),
            setupargs='%s %s setup_build' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
            iterationcleanup=pythoncommand(),
            cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
            affinity=self.affinity)
            startup.runtests(self.traits)

        if self.testtype == const.INNERLOOPMSBUILD:
            startup = StartupWrapper()
            self.traits.add_traits(scenarioname=self.scenarioname,
            scenariotypename=const.SCENARIO_NAMES[const.INNERLOOPMSBUILD],
            apptorun='dotnet', appargs='run --project %s' % appfolder(self.traits.exename, self.traits.projext),
            innerloopcommand=pythoncommand(),
            iterationsetup=pythoncommand(),
            setupargs='%s %s setup_build' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
            iterationcleanup=pythoncommand(),
            cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
            affinity=self.affinity)
            startup.runtests(self.traits)
            
        if self.testtype == const.DOTNETWATCH:
            startup = StartupWrapper()
            self.traits.add_traits(scenarioname=self.scenarioname,
            scenariotypename=const.SCENARIO_NAMES[const.DOTNETWATCH],
            apptorun='dotnet', appargs='watch -v',
            innerloopcommand=pythoncommand(),
            iterationsetup=pythoncommand(),
            setupargs='%s %s setup_build' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
            iterationcleanup=pythoncommand(),
            cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
            affinity=self.affinity)
            self.traits.add_traits(workingdir = const.APPDIR)
            startup.runtests(self.traits)

        if self.testtype == const.STARTUP:
            startup = StartupWrapper()
            self.traits.add_traits(overwrite=False,
                                   environmentvariables='COMPlus_EnableEventLog=1' if not iswin() else '',
                                   scenarioname=self.scenarioname,
                                   scenariotypename=const.SCENARIO_NAMES[const.STARTUP],
                                   apptorun=publishedexe(self.traits.exename),
                                   affinity=self.affinity
                                   )
            if self.traits.runwithdotnet:
                self.traits.add_traits(overwrite=True,
                                        apptorun=const.DOTNET,
                                        appargs=publisheddll(self.traits.exename))
            startup.runtests(self.traits)

        elif self.testtype == const.SDK:
            startup = StartupWrapper()
            envlistbuild = 'DOTNET_MULTILEVEL_LOOKUP=0'
            envlistcleanbuild = ';'.join(['MSBUILDDISABLENODEREUSE=1', envlistbuild])
            # clean build
            if self.sdktype == const.CLEAN_BUILD:
                self.traits.add_traits(
                    overwrite=False,
                    scenarioname=self.scenarioname,
                    scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.CLEAN_BUILD),
                    apptorun=const.DOTNET,
                    appargs='build',
                    iterationsetup=pythoncommand(),
                    setupargs='%s %s setup_build' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    iterationcleanup=pythoncommand(),
                    cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    workingdir=const.APPDIR,
                    environmentvariables=envlistcleanbuild,
                )
                self.traits.add_traits(overwrite=True, startupmetric=const.STARTUP_PROCESSTIME)
                startup.runtests(self.traits)

            # build(no changes)
            if self.sdktype == const.BUILD_NO_CHANGE:
                self.traits.add_traits(
                    overwrite=False,
                    scenarioname=self.scenarioname,
                    scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.BUILD_NO_CHANGE),
                    apptorun=const.DOTNET,
                    appargs='build',
                    workingdir=const.APPDIR,
                    environmentvariables=envlistbuild
                )
                self.traits.add_traits(overwrite=True, startupmetric=const.STARTUP_PROCESSTIME)
                startup.runtests(self.traits)

            # new console
            if self.sdktype == const.NEW_CONSOLE:
                self.traits.add_traits(
                    overwrite=False,
                    appargs='new console',
                    apptorun=const.DOTNET,
                    scenarioname=self.scenarioname,
                    scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.NEW_CONSOLE),
                    iterationsetup=pythoncommand(),
                    setupargs='%s %s setup_new' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    iterationcleanup=pythoncommand(),
                    cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    workingdir=const.APPDIR
                )
                self.traits.add_traits(overwrite=True, startupmetric=const.STARTUP_PROCESSTIME)
                self.traits.add_traits(overwrite=True, affinity=self.affinity)
                startup.runtests(self.traits)

        elif self.testtype == const.CROSSGEN:
            startup = StartupWrapper()
            crossgenexe = 'crossgen%s' % extension()
            crossgenargs = self.crossgen_arguments.get_crossgen_command_line()
            coreroot = self.crossgen_arguments.coreroot
            scenario_filename = self.crossgen_arguments.crossgen2_scenario_filename()

            self.traits.add_traits(overwrite=True,
                                   startupmetric=const.STARTUP_PROCESSTIME,
                                   workingdir=coreroot,
                                   appargs=' '.join(crossgenargs),
                                   affinity=self.affinity
                                   )
            self.traits.add_traits(overwrite=False,
                                   scenarioname='Crossgen Throughput - %s' % scenario_filename,
                                   scenariotypename='%s - %s' % (const.SCENARIO_NAMES[const.CROSSGEN], scenario_filename),
                                   apptorun='%s\\%s' % (coreroot, crossgenexe),
                                  ) 
            startup.runtests(self.traits)
           
        elif self.testtype == const.CROSSGEN2:
            startup = StartupWrapper()
            scenario_filename = self.crossgen_arguments.crossgen2_scenario_filename()
            crossgen2args = self.crossgen_arguments.get_crossgen2_command_line()
            compiletype = self.crossgen_arguments.crossgen2_compiletype()
            scenarioname = 'Crossgen2 Throughput - %s - %s' % (compiletype, scenario_filename)
            if self.crossgen_arguments.singlethreaded:
                scenarioname = 'Crossgen2 Throughput - Single Threaded - %s - %s' % (compiletype, scenario_filename)

            if compiletype == const.CROSSGEN2_COMPOSITE:
                self.traits.add_traits(overwrite=True,
                                       skipprofile='true')

            self.traits.add_traits(overwrite=True,
                                   startupmetric=const.STARTUP_CROSSGEN2,
                                   workingdir=self.crossgen_arguments.coreroot,
                                   appargs='%s %s' % (os.path.join('crossgen2', 'crossgen2.dll'), ' '.join(crossgen2args)),
                                   affinity=self.affinity
                                   )
            self.traits.add_traits(overwrite=False,
                                   scenarioname=scenarioname,
                                   apptorun=os.path.join(self.crossgen_arguments.coreroot, 'corerun%s' % extension()),
                                   environmentvariables='COMPlus_EnableEventLog=1' if not iswin() else '' # turn on clr user events
                                  ) 
            startup.runtests(self.traits)

        elif self.testtype == const.ANDROIDINSTRUMENTATION:
            androidInstrumentation = AndroidInstrumentationHelper()
            androidInstrumentation.runtests(self.packagepath, self.packagename, self.instrumentationname)

        elif self.testtype == const.DEVICEPOWERCONSUMPTION:
            devicePowerConsumption = DevicePowerConsumptionHelper()
            self.traits.add_traits(overwrite=True, apptorun="app", powerconsumptionmetric=const.POWERCONSUMPTION_ANDROID, tracefolder='PerfTest/', tracename='runoutput.trace', scenarioname=self.scenarioname)
            devicePowerConsumption.runtests(self.devicetype, self.packagepath, self.packagename, self.testiterations, self.runtimeseconds, self.closeToStartDelay, self.traits)
  
        elif self.testtype == const.DEVICEMEMORYCONSUMPTION and self.devicetype == 'android':
            getLogger().info("Clearing potential previous run nettraces")
            for file in glob.glob(os.path.join(const.TRACEDIR, 'PerfTest', 'runoutput.trace')):
                if exists(file):   
                    getLogger().info("Removed: " + os.path.join(const.TRACEDIR, file))
                    os.remove(file)

            androidHelper = AndroidHelper()
            try:
                androidHelper.setup_device(self.packagename, self.packagepath, self.animationsdisabled)

                # Create the fullydrawn command
                clearProcStatsCmd = [ 
                    androidHelper.adbpath,
                    'shell',
                    'dumpsys',
                    'procstats',
                    '--clear'
                ]

                captureProcStatsCmd = [ 
                    androidHelper.adbpath,
                    'shell',
                    'dumpsys',
                    'procstats',
                    self.packagename,
                    '--section',
                    'proc'
                ]

                clearLogsCmd = [
                    androidHelper.adbpath,
                    'logcat',
                    '-c'
                ]

                allResults = []
                for i in range(self.testiterations):
                    # Clear logs
                    RunCommand(clearLogsCmd, verbose=True).run()
                    RunCommand(clearProcStatsCmd, verbose=True).run()
                    startStats = RunCommand(androidHelper.startappcommand, verbose=True)
                    startStats.run()
                    time.sleep(self.runtimeseconds)
                    captureProcStats = RunCommand(captureProcStatsCmd, verbose=True)
                    captureProcStats.run()

                    # Save the results and get them from the log
                    RunCommand(androidHelper.stopappcommand, verbose=True).run()
                    
                    # Part of the output we are regexing:
                    # Process summary:
                    # * net.dot.HelloAndroid / u0a1219 / v1:
                    #        TOTAL: ###% (<Part we want>52MB-52MB-52MB/44MB-44MB-44MB/135MB-135MB-135MB over 1</Part we want>)
                    #        Top: 100% (52MB-52MB-52MB/44MB-44MB-44MB/135MB-135MB-135MB over 1)
                    regexSearchString = r"TOTAL: [0-9]{2,3}% \((\d+MB-\d+MB-\d+MB\/\d+MB-\d+MB-\d+MB\/\d+MB-\d+MB-\d+MB over \d+)\)"
                    dirtyCapture = re.search(regexSearchString, captureProcStats.stdout)
                    if not dirtyCapture:
                        raise Exception("Failed to capture the reported start time!")
                    splitNumber = dirtyCapture.group(1).replace("MB", "").strip().split(" over ")
                    splitMemory = splitNumber[0].split("/")
                    pss = splitMemory[0].split("-")
                    uss = splitMemory[1].split("-")
                    rss = splitMemory[2].split("-")
                    memoryCapture = f"PSS: min {pss[0]}, avg {pss[1]}, max {pss[2]}; USS: min {uss[0]}, avg {uss[1]}, max {uss[2]}; RSS: min {rss[0]}, avg {rss[1]}, max {rss[2]}; Number: {splitNumber[1]}\n"
                    print(f"Memory Capture: {memoryCapture}")
                    allResults.append(memoryCapture)
                    time.sleep(self.closeToStartDelay) # Delay in seconds for ensuring a cold start
                
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

            memoryconsumption = MemoryConsumptionWrapper()
            self.traits.add_traits(overwrite=True, apptorun="app", memoryconsumptionmetric=const.MEMORYCONSUMPTION_ANDROID, tracefolder='PerfTest/', tracename='runoutput.trace', scenarioname=self.scenarioname)
            memoryconsumption.parsetraces(self.traits)

        elif self.testtype == const.DEVICESTARTUP and self.devicetype == 'android':
            # ADB Key Event corresponding numbers: https://gist.github.com/arjunv/2bbcca9a1a1c127749f8dcb6d36fb0bc
            # Regex used to split the response from starting the activity and saving each value
            #Example:
            #    Starting: Intent { cmp=net.dot.HelloAndroid/net.dot.MainActivity }
            #    Status: ok
            #    LaunchState: COLD
            #    Activity: net.dot.HelloAndroid/net.dot.MainActivity
            #    TotalTime: 241
            #    WaitTime: 242
            #    Complete
            # Saves: [Intent { cmp=net.dot.HelloAndroid/net.dot.MainActivity }, ok, COLD, net.dot.HelloAndroid/net.dot.MainActivity, 241, 242]
            # Split results (start at 0) (List is Starting (Intent activity), Status (ok...), LaunchState ([HOT, COLD, WARM]), Activity (started activity name), TotalTime(toFrameOne), WaitTime(toFullLoad)) 
            runSplitRegex = r":\s(.+)"
            screenWasOff = False
            getLogger().info("Clearing potential previous run nettraces")
            for file in glob.glob(os.path.join(const.TRACEDIR, 'PerfTest', 'runoutput.trace')):
                if exists(file):   
                    getLogger().info("Removed: " + os.path.join(const.TRACEDIR, file))
                    os.remove(file)

            androidHelper = AndroidHelper()
            try:
                androidHelper.setup_device(self.packagename, self.packagepath, self.animationsdisabled)
                
                # Create the fullydrawn command
                fullyDrawnRetrieveCmd = [ 
                    androidHelper.adbpath,
                    'shell',
                    f"logcat -d | grep 'ActivityTaskManager: Fully drawn {self.packagename}'"
                ]

                basicStartupRetrieveCmd = [ 
                    androidHelper.adbpath,
                    'shell',
                    f"logcat -d | grep 'ActivityTaskManager: Displayed {androidHelper.activityname}'"
                ]

                clearLogsCmd = [
                    androidHelper.adbpath,
                    'logcat',
                    '-c'
                ]

                allResults = []
                for i in range(self.startupiterations):
                    # Clear logs
                    RunCommand(clearLogsCmd, verbose=True).run()
                    startStats = RunCommand(androidHelper.startappcommand, verbose=True)
                    startStats.run()
                    # Make sure we cold started (TODO Add other starts)
                    if "LaunchState: COLD" not in startStats.stdout:
                        getLogger().error("App Start not COLD!")
                        
                    # Save the results and get them from the log
                    if self.usefullydrawntime: time.sleep(self.fullyDrawnDelaySecMax) # Start command doesn't wait for fully drawn report, force a wait for it. -W in the start command waits for the app to finish initial draw.
                    RunCommand(androidHelper.stopappcommand, verbose=True).run()
                    if self.usefullydrawntime:
                        retrieveTimeCmd = RunCommand(fullyDrawnRetrieveCmd, verbose=True)
                    else:
                        retrieveTimeCmd = RunCommand(basicStartupRetrieveCmd, verbose=True)
                    retrieveTimeCmd.run()
                    dirtyCapture = re.search(r"\+(\d*s?\d+)ms", retrieveTimeCmd.stdout)
                    if not dirtyCapture:
                        raise Exception("Failed to capture the reported start time!")
                    captureList = dirtyCapture.group(1).split('s')
                    if len(captureList) == 1: # Only have the ms, everything should be good
                        formattedTime = f"TotalTime: {captureList[0]}\n"
                    elif len(captureList) == 2: # Have s and ms, but maybe not padded ms, pad and combine (zfill left pads with 0)
                        formattedTime = f"TotalTime: {captureList[0]}{captureList[1].zfill(3)}\n"
                    else:
                        getLogger().error("Time capture failed, found {len(captureList)}")
                        raise Exception("Android Time Capture Failed! Incorrect number of captures found.")
                    allResults.append(formattedTime) # append TotalTime: (TIME)
                    time.sleep(self.closeToStartDelay) # Delay in seconds for ensuring a cold start
                
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

            startup = StartupWrapper()
            self.traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.STARTUP_DEVICETIMETOMAIN, tracefolder='PerfTest/', tracename='runoutput.trace', scenarioname=self.scenarioname)
            startup.parsetraces(self.traits)

        elif self.testtype == const.DEVICESTARTUP and self.devicetype == 'ios':

            getLogger().info("Clearing potential previous run nettraces")
            for file in glob.glob(os.path.join(const.TRACEDIR, 'PerfTest', 'runoutput.trace')):
                if exists(file):   
                    getLogger().info("Removed: " + os.path.join(const.TRACEDIR, file))
                    os.remove(file)

            if not exists(const.TMPDIR):
                os.mkdir(const.TMPDIR)

            getLogger().info("Clearing potential previous run *.logarchive")
            for logarchive in glob.glob(os.path.join(const.TMPDIR, '*.logarchive')):
                if exists(logarchive):
                    getLogger().info("Removed: " + os.path.join(const.TMPDIR, logarchive))
                    rmtree(logarchive)

            getLogger().info("Checking device state.")
            cmdline = xharnesscommand() + ['apple', 'state']
            apple_state = RunCommand(cmdline, verbose=True)
            apple_state.run()

            # Get the name and version of the device from the output of apple_state above
            # Example output expected (PERFIOS-01 is the device name, and 17.0.2 is the version):
            #  Connected Devices:
            #    PERFIOS-01 00008101-001A09223E08001E    17.0.2        iPhone iOS
            deviceInfoMatch = re.search(r'Connected Devices:\s+(?P<deviceName>\S+)\s+\S+\s+(?P<deviceVersion>\S+)', apple_state.stdout)
            if deviceInfoMatch:
                deviceName = deviceInfoMatch.group('deviceName')
                deviceVersion = deviceInfoMatch.group('deviceVersion')
                getLogger().info(f"Device Name: {deviceName}")
                getLogger().info(f"Device Version: {deviceVersion}")
            else:
                raise Exception("Device name or version not found in the output of apple_state command.")
            
            getLogger().info("Installing app on device.")
            installCmd = xharnesscommand() + [
                'apple',
                'install',
                '--app', self.packagepath,
                '--target', 'ios-device',
                '-o',
                const.TRACEDIR,
                '-v'
            ]
            RunCommand(installCmd, verbose=True).run()
            getLogger().info("Completed install.")

            allResults = []
            timeToFirstDrawEventEndDateTime = datetime.now() # This is used to keep track of the latest time to draw end event, we use this to calculate time to draw and also as a reference point for the next iteration log time.
            for i in range(self.startupiterations + 1): # adding one iteration to account for the warmup iteration
                getLogger().info("Waiting 10 secs to ensure we're not getting confused with previous app run.")
                time.sleep(10)

                getLogger().info(f"Collect startup data for iteration {i}.")
                runCmdTimestamp = timeToFirstDrawEventEndDateTime + timedelta(seconds=1)
                runCmd = xharnesscommand() + [
                    'apple',
                    'mlaunch',
                    '--',
                    '--launchdev', self.packagepath,
                    '--devname', deviceName
                ]
                runCmdCommand = RunCommand(runCmd, verbose=True)

                try:
                    runCmdCommand.run()
                except CalledProcessError as ex:
                    if ex.returncode == 70:
                        # Exit code 70 from xharness means time out, this can happen sometimes when the device is in a screwed state
                        # and doesn't correctly launch apps anymore. In that case we reboot the device
                        getLogger().error("Device is in a broken state, rebooting.")
                        rebootCmd = xharnesscommand() + [
                            'apple',
                            'mlaunch',
                            '--',
                            '--rebootdev',
                        ]
                        rebootCmdCommand = RunCommand(rebootCmd, verbose=True)
                        rebootCmdCommand.run()

                        getLogger().info("Waiting 30 secs for the device to boot.")
                        time.sleep(30)

                        # if we're in Helix, schedule the work item for retry by writing a special file to the workitem root
                        if helixworkitemroot():
                            getLogger().info("Requesting retry from Helix.")
                            with open(f"{helixworkitemroot()}/.retry", "w") as retryFile:
                                retryFile.write("Device was in a broken state, rebooted the device and retrying work item.")

                    # rethrow exception so we end the process
                    getLogger().error("App launch failed, please rerun the script to start a new measurement.")
                    raise

                # If the device version is less than 17 we need to use the old pid search
                # otherwise we use the new pid search
                if deviceVersion < '17':
                    app_pid_search = re.search(r"Launched application.*with pid (?P<app_pid>\d+)", runCmdCommand.stdout)
                else:
                    app_pid_search = re.search(r"The app.*launched with pid (?P<app_pid>\d+)", runCmdCommand.stdout)
                app_pid = int(app_pid_search.group('app_pid'))

                logarchive_filename = os.path.join(const.TMPDIR, f'iteration{i}.logarchive')
                getLogger().info(f"Waiting 5 secs to ensure app with PID {app_pid} is fully started.")
                time.sleep(5)
                collectCmd = [
                    'sudo',
                    'log',
                    'collect',
                    '--device',
                    '--start', runCmdTimestamp.strftime("%Y-%m-%d %H:%M:%S%z"),
                    '--output', logarchive_filename,
                ]
                RunCommand(collectCmd, verbose=True).run()

                getLogger().info(f"Kill app with PID {app_pid}.")
                killCmd = xharnesscommand() + [
                    'apple',
                    'mlaunch',
                    '--',
                    f'--killdev={app_pid}',
                    '--devname', deviceName
                ]
                killCmdCommand = RunCommand(killCmd, verbose=True)
                killCmdCommand.run()

                # Process Data

                # There are four watchdog events from SpringBoard during an application startup:
                #
                # [application<net.dot.maui>:770] [realTime] Now monitoring resource allowance of 20.00s (at refreshInterval -1.00s)
                # [application<net.dot.maui>:770] [realTime] Stopped monitoring.
                # [application<net.dot.maui>:770] [realTime] Now monitoring resource allowance of 19.28s (at refreshInterval -1.00s)
                # [application<net.dot.maui>:770] [realTime] Stopped monitoring.
                #
                # The first two are monitoring the time it takes the OS to create the process, load .dylibs and call into the app's main()
                # The second two are monitoring the time it takes the app to draw the first frame of UI from main()
                #
                # An app has 20 seconds to complete this sequence or the OS will kill the app.
                # We collect these log events to do our measurements.

                logShowCmd = [
                    'log',
                    'show',
                    '--predicate', '(process == "SpringBoard") && (category == "Watchdog")',
                    '--info',
                    '--style', 'ndjson',
                    logarchive_filename,
                ]
                logShowCmdCommand = RunCommand(logShowCmd, verbose=True)
                logShowCmdCommand.run()

                events = []
                for line in logShowCmdCommand.stdout.splitlines():
                    try:
                        lineData = json.loads(line)
                        if 'Now monitoring resource allowance' in lineData['eventMessage'] or 'Stopped monitoring' in lineData['eventMessage']:
                            events.append(lineData)
                    except:
                        break

                # the startup measurement relies on the date/time of the device to be pretty much in sync with the host
                # since we use the timestamps from the host to decide which parts of the device log to get and
                # we then use that to calculate the time delta from watchdog events
                if len(events) != 4:
                    raise Exception("Didn't get the right amount of watchdog events, this could mean the app crashed or the device clock is not in sync with the host.")

                timeToMainEventStart = events[0]
                timeToMainEventStop = events[1]
                timeToFirstDrawEventStart = events[2]
                timeToFirstDrawEventStop = events[3]

                # validate log messages
                if f'{self.packagename}' not in timeToMainEventStart['eventMessage'] or 'Now monitoring resource allowance of 20.00s' not in timeToMainEventStart['eventMessage']:
                    raise Exception(f"Invalid timeToMainEventStart: {timeToMainEventStart['eventMessage']}")

                if f'{self.packagename}' not in timeToMainEventStop['eventMessage'] or 'Stopped monitoring' not in timeToMainEventStop['eventMessage']:
                    raise Exception(f"Invalid timeToMainEventStop: {timeToMainEventStop['eventMessage']}")

                if f'{self.packagename}' not in timeToFirstDrawEventStart['eventMessage'] or 'Now monitoring resource allowance of' not in timeToFirstDrawEventStart['eventMessage']:
                    raise Exception(f"Invalid timeToFirstDrawEventStart: {timeToFirstDrawEventStart['eventMessage']}")

                if f'{self.packagename}' not in timeToFirstDrawEventStop['eventMessage'] or 'Stopped monitoring' not in timeToFirstDrawEventStop['eventMessage']:
                    raise Exception(f"Invalid timeToFirstDrawEventStop: {timeToFirstDrawEventStop['eventMessage']}")

                timeToMainEventStartDateTime = datetime.strptime(timeToMainEventStart['timestamp'], '%Y-%m-%d %H:%M:%S.%f%z')
                timeToMainEventEndDateTime = datetime.strptime(timeToMainEventStop['timestamp'], '%Y-%m-%d %H:%M:%S.%f%z')
                timeToMainMilliseconds = (timeToMainEventEndDateTime - timeToMainEventStartDateTime).total_seconds() * 1000

                timeToFirstDrawEventStartDateTime = datetime.strptime(timeToFirstDrawEventStart['timestamp'], '%Y-%m-%d %H:%M:%S.%f%z')
                timeToFirstDrawEventEndDateTime = datetime.strptime(timeToFirstDrawEventStop['timestamp'], '%Y-%m-%d %H:%M:%S.%f%z')
                timeToFirstDrawMilliseconds = (timeToFirstDrawEventEndDateTime - timeToFirstDrawEventStartDateTime).total_seconds() * 1000

                if self.usefullydrawntime:
                    # grab log event with the magic string in it
                    logShowMagicStringCmd = [
                        'log',
                        'show',
                        '--predicate', f'(processIdentifier == {app_pid}) && (composedMessage contains "{self.fullyDrawnMagicString}")',
                        '--info',
                        '--style', 'ndjson',
                        logarchive_filename,
                    ]
                    logShowMagicStringCmd = RunCommand(logShowMagicStringCmd, verbose=True)
                    logShowMagicStringCmd.run()

                    magicStringEvent = ''
                    for line in logShowMagicStringCmd.stdout.splitlines():
                        try:
                            lineData = json.loads(line)
                            if self.fullyDrawnMagicString in lineData['eventMessage']:
                                magicStringEvent = lineData
                        except:
                            break

                    if magicStringEvent == '':
                        raise Exception("Didn't get the fully-drawn magic string event.")

                    timeToMagicStringEventDateTime = datetime.strptime(magicStringEvent['timestamp'], '%Y-%m-%d %H:%M:%S.%f%z')

                    # startup time is time to the magic string event
                    totalTimeMilliseconds = (timeToMagicStringEventDateTime - timeToMainEventStartDateTime).total_seconds() * 1000
                else:
                    # startup time is time to first draw
                    totalTimeMilliseconds = timeToMainMilliseconds + timeToFirstDrawMilliseconds

                if i == 0:
                    # ignore the warmup iteration
                    getLogger().info(f'Warmup iteration took {totalTimeMilliseconds}')
                else:
                    # TODO: this isn't really a COLD run, we should have separate measurements for starting the app right after install
                    launchState = 'COLD'
                    allResults.append(f'LaunchState: {launchState}\nTotalTime: {int(totalTimeMilliseconds)}\nTimeToMain: {int(timeToMainMilliseconds)}\n\n')

            # Done with testing, uninstall the app
            getLogger().info("Uninstalling app")
            uninstallAppCmd = xharnesscommand() + [
                'apple',
                'uninstall',
                '--app', self.packagename,
                '--target', 'ios-device',
                '-o',
                const.TRACEDIR,
                '-v'
            ]
            RunCommand(uninstallAppCmd, verbose=True).run()

            # Create traces to store the data so we can keep the current general parse trace flow
            getLogger().info(f"Logs: \n{allResults}")
            outputdir = os.path.join(const.TRACEDIR,"PerfTest")
            os.makedirs(outputdir, exist_ok=True)
            outputtracefile = os.path.join(outputdir, "runoutput.trace")
            tracefile = open(outputtracefile, "w")
            for result in allResults:
                tracefile.write(result)
            tracefile.close()

            startup = StartupWrapper()
            self.traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.STARTUP_DEVICETIMETOMAIN, tracefolder='PerfTest/', tracename='runoutput.trace', scenarioname=self.scenarioname)
            startup.parsetraces(self.traits)

        elif self.testtype == const.SOD:
            sod = SODWrapper()
            builtdir = const.PUBDIR if os.path.exists(const.PUBDIR) else None
            if not builtdir:
                builtdir = const.BINDIR if os.path.exists(const.BINDIR) else None
            if not (self.dirs or builtdir):
                raise Exception("Dirs was not passed in and neither %s nor %s exist" % (const.PUBDIR, const.BINDIR))
            sod.runtests(scenarioname=self.scenarioname, dirs=self.dirs or builtdir, artifact=self.traits.artifact)
