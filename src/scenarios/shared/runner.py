'''
Module for running scenario tasks
'''

from genericpath import exists
import sys
import os
import glob
import re
import time

from logging import getLogger
from collections import namedtuple
from argparse import ArgumentParser
from argparse import RawTextHelpFormatter
from io import StringIO
from shutil import move
from shared.crossgen import CrossgenArguments
from shared.startup import StartupWrapper
from shared.util import publishedexe, pythoncommand, appfolder, xharnesscommand
from shared.sod import SODWrapper
from shared import const
from performance.common import RunCommand, iswin, extension
from performance.logger import setup_loggers
from shared.testtraits import TestTraits, testtypes


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
        self.add_common_arguments(startupparser)

        # parse only command
        parseonlyparser = subparsers.add_parser(const.DEVICESTARTUP,
                                              description='measure time to main for Android apps')
        parseonlyparser.add_argument('--device-type', choices=['android','ios'],type=str.lower,help='Device type for testing', dest='devicetype')
        parseonlyparser.add_argument('--package-path', help='Location of test application', dest='packagepath')
        parseonlyparser.add_argument('--package-name', help='Classname of application', dest='packagename')
        parseonlyparser.add_argument('--startup-iterations', help='Startups to run (1+)', type=int, default=5, dest='startupiterations')
        parseonlyparser.add_argument('--disable-animations', help='Disable Android device animations', action='store_true', dest='animationsdisabled')
        self.add_common_arguments(parseonlyparser)

        # inner loop command
        innerloopparser = subparsers.add_parser(const.INNERLOOP,
                                              description='measure time to main and difference between two runs in a row')
        self.add_common_arguments(innerloopparser)

        # inner loop msbuild command
        innerloopparser = subparsers.add_parser(const.INNERLOOPMSBUILD,
                                              description='measure time to main and difference between two runs in a row')
        self.add_common_arguments(innerloopparser)

        # dotnet watch command
        dotnetwatchparser = subparsers.add_parser(const.DOTNETWATCH,
                                              description='measure time to main and time for hot reload')
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
        self.add_common_arguments(sdkparser)

        crossgenparser = subparsers.add_parser(const.CROSSGEN,
                                               description='measure duration of the crossgen compilation',
                                               formatter_class=RawTextHelpFormatter)
        self.crossgen_arguments.add_crossgen_arguments(crossgenparser)
        self.add_common_arguments(crossgenparser)

        crossgen2parser = subparsers.add_parser(const.CROSSGEN2,
                                                description='measure duration of the crossgen compilation',
                                                formatter_class=RawTextHelpFormatter)
        self.crossgen_arguments.add_crossgen2_arguments(crossgen2parser)
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

        if args.scenarioname:
            self.scenarioname = args.scenarioname

    
    def add_common_arguments(self, parser: ArgumentParser):
        "Common arguments to add to subparsers"
        parser.add_argument('--scenario-name',
                            dest='scenarioname')

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
            cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE))
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
            cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE))
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
            cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE))
            self.traits.add_traits(workingdir = const.APPDIR)
            startup.runtests(self.traits)

        if self.testtype == const.STARTUP:
            startup = StartupWrapper()
            self.traits.add_traits(overwrite=False,
                                   environmentvariables='COMPlus_EnableEventLog=1' if not iswin() else '',
                                   scenarioname=self.scenarioname,
                                   scenariotypename=const.SCENARIO_NAMES[const.STARTUP],
                                   apptorun=publishedexe(self.traits.exename),
                                   )
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
                                   appargs=' '.join(crossgenargs)
                                   )
            self.traits.add_traits(overwrite=False,
                                   scenarioname='Crossgen Throughput - %s' % scenario_filename,
                                   scenariotypename='%s - %s' % (const.SCENARIO_NAMES[const.CROSSGEN], scenario_filename),
                                   apptorun='%s\%s' % (coreroot, crossgenexe),
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
                                   appargs='%s %s' % (os.path.join('crossgen2', 'crossgen2.dll'), ' '.join(crossgen2args))
                                   )
            self.traits.add_traits(overwrite=False,
                                   scenarioname=scenarioname,
                                   apptorun=os.path.join(self.crossgen_arguments.coreroot, 'corerun%s' % extension()),
                                   environmentvariables='COMPlus_EnableEventLog=1' if not iswin() else '' # turn on clr user events
                                  ) 
            startup.runtests(self.traits)


        elif self.testtype == const.DEVICESTARTUP:
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
            runSplitRegex = ":\s(.+)" 
            screenWasOff = False
            getLogger().info("Clearing potential previous run nettraces")
            for file in glob.glob(os.path.join(const.TRACEDIR, 'PerfTest', 'runoutput.trace')):
                if exists(file):   
                    getLogger().info("Removed: " + os.path.join(const.TRACEDIR, file))
                    os.remove(file)

            cmdline = xharnesscommand() + [self.devicetype, 'state', '--adb']
            adb = RunCommand(cmdline, verbose=True)
            adb.run()

            # Do not remove, XHarness install seems to fail without an adb command called before the xharness command
            getLogger().info("Preparing ADB")
            cmdline = [
                adb.stdout.strip(),
                'shell',
                'wm',
                'size'
            ]
            RunCommand(cmdline, verbose=True).run()

            # Get animation values
            getLogger().info("Getting animation values")
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'get', 'global', 'window_animation_scale'
            ]
            window_animation_scale_cmd = RunCommand(cmdline, verbose=True)
            window_animation_scale_cmd.run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'get', 'global', 'transition_animation_scale'
            ]
            transition_animation_scale_cmd = RunCommand(cmdline, verbose=True)
            transition_animation_scale_cmd.run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'get', 'global', 'animator_duration_scale'
            ]
            animator_duration_scale_cmd = RunCommand(cmdline, verbose=True)
            animator_duration_scale_cmd.run()
            getLogger().info(f"Retrieved values window {window_animation_scale_cmd.stdout.strip()}, transition {transition_animation_scale_cmd.stdout.strip()}, animator {animator_duration_scale_cmd.stdout.strip()}")

            # Make sure animations are set to 1 or disabled
            getLogger().info("Setting animation values")
            if(self.animationsdisabled):
                animationValue = 0
            else:
                animationValue = 1
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'put', 'global', 'window_animation_scale', str(animationValue)
            ]
            RunCommand(cmdline, verbose=True).run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'put', 'global', 'transition_animation_scale', str(animationValue)
            ]
            RunCommand(cmdline, verbose=True).run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'put', 'global', 'animator_duration_scale', str(animationValue)
            ]
            RunCommand(cmdline, verbose=True).run()

            # Check for success
            getLogger().info("Getting animation values to verify it worked")
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'get', 'global', 'window_animation_scale'
            ]
            RunCommand(cmdline, verbose=True).run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'get', 'global', 'transition_animation_scale'
            ]
            RunCommand(cmdline, verbose=True).run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'get', 'global', 'animator_duration_scale'
            ]
            RunCommand(cmdline, verbose=True).run()
 

            installCmd = xharnesscommand() + [
                self.devicetype,
                'install',
                '--app', self.packagepath,
                '--package-name',
                self.packagename,
                '-o',
                const.TRACEDIR,
                '-v'
            ]
            RunCommand(installCmd, verbose=True).run()

            getLogger().info("Completed install, running shell.")
            cmdline = [ 
                adb.stdout.strip(),
                'shell',
                f'cmd package resolve-activity --brief {self.packagename} | tail -n 1'
            ]
            getActivity = RunCommand(cmdline, verbose=True)
            getActivity.run()
            getLogger().info(f"Target Activity {getActivity.stdout}")

            # More setup stuff
            checkScreenOnCmd = [ 
                adb.stdout.strip(),
                'shell',
                f'dumpsys input_method | grep mInteractive'
            ]
            checkScreenOn = RunCommand(checkScreenOnCmd, verbose=True)
            checkScreenOn.run()

            keyInputCmd = [
                adb.stdout.strip(),
                'shell',
                'input',
                'keyevent'
            ]

            if("mInteractive=false" in checkScreenOn.stdout): 
                # Turn on the screen to make interactive and see if it worked
                getLogger().info("Screen was off, turning on.")
                screenWasOff = True
                RunCommand(keyInputCmd + ['26'], verbose=True).run() # Press the power key
                RunCommand(keyInputCmd + ['82'], verbose=True).run() # Unlock the screen with menu key (only works if it is not a password lock)

                checkScreenOn = RunCommand(checkScreenOnCmd, verbose=True)
                checkScreenOn.run()
                if("mInteractive=false" in checkScreenOn.stdout):
                    getLogger().exception("Failed to make screen interactive.")

            # Actual testing some run stuff
            getLogger().info("Test run to check if permissions are needed")
            activityname = getActivity.stdout

            startAppCmd = [ 
                adb.stdout.strip(),
                'shell',
                'am',
                'start-activity',
                '-W',
                '-n',
                activityname
            ]
            testRun = RunCommand(startAppCmd, verbose=True)
            testRun.run()
            testRunStats = re.findall(runSplitRegex, testRun.stdout) # Split results saving value (List: Starting, Status, LaunchState, Activity, TotalTime, WaitTime) 
            getLogger().info(f"Test run activity: {testRunStats[3]}")

            stopAppCmd = [ 
                adb.stdout.strip(),
                'shell',
                'am',
                'force-stop',
                self.packagename
            ]
            RunCommand(stopAppCmd, verbose=True).run()

            if "com.google.android.permissioncontroller" in testRunStats[3]:
                # On perm screen, use the buttons to close it. it will stay away until the app is reinstalled
                RunCommand(keyInputCmd + ['22'], verbose=True).run() # Select next button
                time.sleep(1)
                RunCommand(keyInputCmd + ['22'], verbose=True).run() # Select next button
                time.sleep(1)
                RunCommand(keyInputCmd + ['66'], verbose=True).run() # Press enter to close main perm screen
                time.sleep(1)
                RunCommand(keyInputCmd + ['22'], verbose=True).run() # Select next button
                time.sleep(1)
                RunCommand(keyInputCmd + ['66'], verbose=True).run() # Press enter to close out of second screen
                time.sleep(1)

                # Check to make sure it worked
                testRun = RunCommand(startAppCmd, verbose=True)
                testRun.run()
                testRunStats = re.findall(runSplitRegex, testRun.stdout) 
                getLogger().info(f"Test run activity: {testRunStats[3]}")
                RunCommand(stopAppCmd, verbose=True).run() 
                
                if "com.google.android.permissioncontroller" in testRunStats[3]:
                    getLogger().exception("Failed to get past permission screen, run locally to see if enough next button presses were used.")

            allResults = []
            for i in range(self.startupiterations):
                startStats = RunCommand(startAppCmd, verbose=True)
                startStats.run()
                RunCommand(stopAppCmd, verbose=True).run()
                allResults.append(startStats.stdout) # Save results (List is Intent, Status, LaunchState Activity, TotalTime, WaitTime)
                time.sleep(3) # Delay in seconds for ensuring a cold start

            getLogger().info("Stopping App for uninstall")
            RunCommand(stopAppCmd, verbose=True).run()
                    
            getLogger().info("Uninstalling app")
            uninstallAppCmd = xharnesscommand() + [
                'android',
                'uninstall',
                '--package-name',
                self.packagename
            ]
            RunCommand(uninstallAppCmd, verbose=True).run()

            # Reset animation values 
            getLogger().info("Resetting animation values to pretest values")
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'put', 'global', 'window_animation_scale', window_animation_scale_cmd.stdout.strip()
            ]
            RunCommand(cmdline, verbose=True).run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'put', 'global', 'transition_animation_scale', transition_animation_scale_cmd.stdout.strip()
            ]
            RunCommand(cmdline, verbose=True).run()
            cmdline = [
                adb.stdout.strip(),
                'shell', 'settings', 'put', 'global', 'animator_duration_scale', animator_duration_scale_cmd.stdout.strip()
            ]
            RunCommand(cmdline, verbose=True).run()

            if screenWasOff:
                RunCommand(keyInputCmd + ['26'], verbose=True).run() # Turn the screen back off

            # Create traces to store the data so we can keep the current general parse trace flow
            getLogger().info(f"Logs: \n{allResults}")
            os.makedirs(f"{const.TRACEDIR}/PerfTest", exist_ok=True)
            traceFile = open(f"{const.TRACEDIR}/PerfTest/runoutput.trace", "w")
            for result in allResults:
                traceFile.write(result)
            traceFile.close()

            startup = StartupWrapper()
            self.traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.STARTUP_DEVICETIMETOMAIN, tracefolder='PerfTest/', tracename='runoutput.trace', scenarioname='Device Startup - Android %s' % (self.packagename))
            startup.parsetraces(self.traits)

        elif self.testtype == const.SOD:
            sod = SODWrapper()
            builtdir = const.PUBDIR if os.path.exists(const.PUBDIR) else None
            if not builtdir:
                builtdir = const.BINDIR if os.path.exists(const.BINDIR) else None
            if not (self.dirs or builtdir):
                raise Exception("Dirs was not passed in and neither %s nor %s exist" % (const.PUBDIR, const.BINDIR))
            sod.runtests(scenarioname=self.scenarioname, dirs=self.dirs or builtdir, artifact=self.traits.artifact)
