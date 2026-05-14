'''
Module for running scenario tasks
'''

import sys
import os
import glob
import re
import shlex
import time
import json

from genericpath import exists
from datetime import datetime, timedelta
from logging import getLogger
from argparse import ArgumentParser
from argparse import RawTextHelpFormatter
from shutil import rmtree
from typing import Optional
from shared.androidhelper import AndroidHelper
from shared.androidinstrumentation import AndroidInstrumentationHelper
from shared.ioshelper import iOSHelper
from shared.devicepowerconsumption import DevicePowerConsumptionHelper
from shared.crossgen import CrossgenArguments
from shared.startup import StartupWrapper
from shared.memoryconsumption import MemoryConsumptionWrapper
from shared.util import publishedexe, pythoncommand, appfolder, xharnesscommand, xharness_adb, publisheddll
from shared.sod import SODWrapper
from shared import const
from performance.common import RunCommand, iswin, extension, helixworkitemroot
from performance.logger import setup_loggers
from shared.testtraits import TestTraits, testtypes
from subprocess import CalledProcessError


# ── iOS Inner Loop report helpers ────────────────────────────────────

def _make_counter(name, metric, results, top=False):
    """Create a performance counter dict for JSON reports."""
    return {
        "name": name,
        "topCounter": top,
        "defaultCounter": False,
        "higherIsBetter": False,
        "metricName": metric,
        "results": results,
    }


def _merge_deploy_report(build_report_path, install_results, startup_results, output_path, app_size_bytes=None):
    """Merge build metrics with install/startup timing into a single E2E report.

    If the build report doesn't exist (local runs without PERFLAB_INLAB=1),
    creates a minimal report with only install and startup counters.
    """
    if os.path.exists(build_report_path):
        with open(build_report_path, 'r') as f:
            report = json.load(f)
        if not report.get("tests"):
            report["tests"] = [{"counters": []}]
        elif "counters" not in report["tests"][0]:
            report["tests"][0]["counters"] = []
    else:
        report = {"tests": [{"counters": []}]}

    report["tests"][0]["counters"].append(_make_counter("Install Time", "ms", install_results))
    report["tests"][0]["counters"].append(_make_counter("Cold Startup Time", "ms", startup_results, top=True))
    if app_size_bytes is not None:
        report["tests"][0]["counters"].append(_make_counter("App Bundle Size", "bytes", [app_size_bytes], top=True))

    with open(output_path, 'w') as f:
        json.dump(report, f, indent=2)


def _measure_app_size(app_bundle_path):
    """Return the total size of a .app bundle directory in bytes.

    Lightweight inline equivalent of the SizeOnDisk tool (SizeOnDisk.cs / sod.py)
    which is designed for standalone size-focused scenarios with per-file breakdowns.
    This helper just needs the total for a single counter in the E2E report.
    """
    total = 0
    for dirpath, _dirnames, filenames in os.walk(app_bundle_path):
        for f in filenames:
            fp = os.path.join(dirpath, f)
            if not os.path.islink(fp):
                total += os.path.getsize(fp)
    return total


class Runner:
    '''
    Wrapper for running all the things
    '''

    def __init__(self, traits: TestTraits):
        self.traits = traits
        self.testtype = None
        self.sdktype = None
        self.scenarioname: Optional[str] = None
        self.coreroot = None
        self.crossgenfile = None
        self.dirs = None
        self.crossgen_arguments = CrossgenArguments()
        self.affinity = None
        self.upload_to_perflab_container = False
        self.binlogpath = None
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
        devicestartupparser.add_argument('--trace-perfetto', help='Android Only. Trace the startup with Perfetto and save to the "traces" directory.', action='store_true', dest='traceperfetto')
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

        buildtimeparser = subparsers.add_parser(const.BUILDTIME,
                                              description='measure build time from a binlog')
        buildtimeparser.add_argument('--binlog-path', help='Location of binlog', dest='binlogpath')
        self.add_common_arguments(buildtimeparser)

        iosinnerloopparser = subparsers.add_parser(const.IOSINNERLOOP,
                                                 description='measure first and incremental build+deploy time via binlogs (iOS)')
        iosinnerloopparser.add_argument('--csproj-path', help='Path to .csproj file to build', dest='csprojpath')
        iosinnerloopparser.add_argument('--edit-src', help='Modified source file paths, semicolon-separated', dest='editsrc')
        iosinnerloopparser.add_argument('--edit-dest', help='Destination paths for modified files, semicolon-separated', dest='editdest')
        iosinnerloopparser.add_argument('--framework', '-f', help='Target framework (e.g., net11.0-ios)', dest='framework')
        iosinnerloopparser.add_argument('--configuration', '-c', help='Build configuration', dest='configuration', default='Debug')
        iosinnerloopparser.add_argument('--msbuild-args', help='Additional MSBuild arguments', dest='msbuildargs', default='')
        iosinnerloopparser.add_argument('--bundle-id', help='iOS bundle identifier', dest='bundleid')
        iosinnerloopparser.add_argument('--device-id', help='iOS device ID (UDID for physical device, simulator ID or "booted" for simulator)', dest='deviceid', default='booted')
        iosinnerloopparser.add_argument('--device-type', choices=['simulator', 'device'], help='Target device type: simulator (default) or physical device. Auto-detected from RuntimeIdentifier if not set.', dest='devicetype', default=None)
        iosinnerloopparser.add_argument('--inner-loop-iterations', help='Number of incremental build+deploy+startup iterations (1+)', type=int, default=10, dest='innerloopiterations')
        self.add_common_arguments(iosinnerloopparser)

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

        if self.testtype == const.BUILDTIME:
            self.binlogpath = args.binlogpath

        if self.testtype == const.IOSINNERLOOP:
            self.csprojpath = args.csprojpath
            self.editsrcs = args.editsrc.split(';') if args.editsrc else []
            self.editdests = args.editdest.split(';') if args.editdest else []
            self.framework = args.framework
            self.configuration = args.configuration
            self.msbuildargs = args.msbuildargs or os.environ.get('PERFLAB_MSBUILD_ARGS', '')
            # If IOS_RID is set (by .proj PreCommands or setup_helix.py arch
            # detection), ensure RuntimeIdentifier in msbuildargs matches it.
            ios_rid_env = os.environ.get('IOS_RID', '')
            if ios_rid_env and 'RuntimeIdentifier=' in self.msbuildargs:
                self.msbuildargs = re.sub(
                    r'RuntimeIdentifier=\S+',
                    f'RuntimeIdentifier={ios_rid_env}',
                    self.msbuildargs)
            self.bundleid = args.bundleid
            self.deviceid = args.deviceid
            self.innerloopiterations = args.innerloopiterations
            # Determine device type: explicit arg, or infer from RuntimeIdentifier
            # in msbuildargs (ios-arm64 → device, iossimulator-* → simulator)
            if args.devicetype:
                self.devicetype = args.devicetype
            elif 'RuntimeIdentifier=ios-arm64' in self.msbuildargs:
                self.devicetype = 'device'
            else:
                self.devicetype = 'simulator'
        
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
            self.traceperfetto = args.traceperfetto

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

        self.upload_to_perflab_container = args.upload_to_perflab_container

        if self.testtype in [const.STARTUP, const.INNERLOOP, const.INNERLOOPMSBUILD, const.DOTNETWATCH, const.SDK, const.CROSSGEN, const.CROSSGEN2] and (args.affinity or os.environ.get('PERFLAB_DATA_AFFINITY')): # Set affinity if doing a Startup based test
            self.affinity = args.affinity if args.affinity else os.environ.get('PERFLAB_DATA_AFFINITY')


    def add_common_arguments(self, parser: ArgumentParser):
        "Common arguments to add to subparsers"
        parser.add_argument('--scenario-name',
                            dest='scenarioname')
        
        parser.add_argument('--upload-to-perflab-container',
            dest="upload_to_perflab_container",
            required=False,
            help="Causes results files to be uploaded to perf container",
            action='store_true')
        
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

        python_command = pythoncommand().split(' ')
        python_exe = python_command[0]
        python_args = " ".join(python_command[1:])
        self.traits.add_traits(upload_to_perflab_container=self.upload_to_perflab_container)

        if self.testtype == const.INNERLOOP:
            startup = StartupWrapper()
            self.traits.add_traits(scenarioname=self.scenarioname,
            scenariotypename=const.SCENARIO_NAMES[const.INNERLOOP],
            apptorun='dotnet', appargs='run --project %s' % appfolder(self.traits.exename, self.traits.projext),
            innerloopcommand=python_exe,
            iterationsetup=python_exe,
            setupargs='%s %s setup_build' % (python_args, const.ITERATION_SETUP_FILE),
            iterationcleanup=python_exe,
            cleanupargs='%s %s cleanup' % (python_args, const.ITERATION_SETUP_FILE),
            affinity=self.affinity)
            startup.runtests(self.traits)

        if self.testtype == const.INNERLOOPMSBUILD:
            startup = StartupWrapper()
            self.traits.add_traits(scenarioname=self.scenarioname,
            scenariotypename=const.SCENARIO_NAMES[const.INNERLOOPMSBUILD],
            apptorun='dotnet', appargs='run --project %s' % appfolder(self.traits.exename, self.traits.projext),
            innerloopcommand=python_exe,
            iterationsetup=python_exe,
            setupargs='%s %s setup_build' % (python_args, const.ITERATION_SETUP_FILE),
            iterationcleanup=python_exe,
            cleanupargs='%s %s cleanup' % (python_args, const.ITERATION_SETUP_FILE),
            affinity=self.affinity)
            startup.runtests(self.traits)
            
        if self.testtype == const.DOTNETWATCH:
            startup = StartupWrapper()
            self.traits.add_traits(scenarioname=self.scenarioname,
            scenariotypename=const.SCENARIO_NAMES[const.DOTNETWATCH],
            apptorun='dotnet', appargs='watch -v',
            innerloopcommand=python_exe,
            iterationsetup=python_exe,
            setupargs='%s %s setup_build' % (python_args, const.ITERATION_SETUP_FILE),
            iterationcleanup=python_exe,
            cleanupargs='%s %s cleanup' % (python_args, const.ITERATION_SETUP_FILE),
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
                    iterationsetup=python_exe,
                    setupargs='%s %s setup_build' % (python_args, const.ITERATION_SETUP_FILE),
                    iterationcleanup=python_exe,
                    cleanupargs='%s %s cleanup' % (python_args, const.ITERATION_SETUP_FILE),
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
                    iterationsetup=python_exe,
                    setupargs='%s %s setup_new' % (python_args, const.ITERATION_SETUP_FILE),
                    iterationcleanup=python_exe,
                    cleanupargs='%s %s cleanup' % (python_args, const.ITERATION_SETUP_FILE),
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
            androidInstrumentation.runtests(self.packagepath, self.packagename, self.instrumentationname, self.upload_to_perflab_container)

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
                clearProcStatsCmd = xharness_adb() + [
                    'shell',
                    'dumpsys',
                    'procstats',
                    '--clear'
                ]

                captureProcStatsCmd = xharness_adb() + [
                    'shell',
                    'dumpsys',
                    'procstats',
                    self.packagename,
                    '--section',
                    'proc'
                ]

                clearLogsCmd = xharness_adb() + [
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
                fullyDrawnRetrieveCmd = xharness_adb() + [ 
                    'shell',
                    f"logcat -d | grep -E 'ActivityManager|ActivityTaskManager' | grep ': Fully drawn {self.packagename}'"
                ]

                basicStartupRetrieveCmd = xharness_adb() + [ 
                    'shell',
                    f"logcat -d | grep -E 'ActivityManager|ActivityTaskManager' | grep ': Displayed {androidHelper.activityname}'"
                ]

                clearLogsCmd = xharness_adb() + [
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
                        getLogger().error(f"Time capture failed, found {len(captureList)}")
                        raise Exception("Android Time Capture Failed! Incorrect number of captures found.")
                    allResults.append(formattedTime) # append TotalTime: (TIME)
                    time.sleep(self.closeToStartDelay) # Delay in seconds for ensuring a cold start

                if self.traceperfetto:
                    perfetto_device_save_file = f'/data/misc/perfetto-traces/perfetto_trace_{time.time()}'
                    original_traced_enable = None
                    perfetto_terminated = False

                    stop_perfetto_cmd = xharness_adb() + [ # Stop perfetto now that the app. Sending a Terminate signal should be enough per the longer trace capturing guidance here: https://perfetto.dev/docs/concepts/config#android.
                        'shell',
                        'pkill -TERM perfetto'
                    ]

                    try:
                        getLogger().info("Clearing potential previous running perfetto traces")
                        RunCommand(stop_perfetto_cmd, verbose=True).run()

                        # Get the current value of persist.traced.enable
                        getLogger().info("Getting current persist.traced.enable value")
                        get_traced_cmd = xharness_adb() + [
                            'shell',
                            'getprop persist.traced.enable'
                        ]
                        get_traced_result = RunCommand(get_traced_cmd, verbose=True)
                        get_traced_result.run()
                        original_traced_enable = get_traced_result.stdout.strip()

                        # Setup the phone props to allow perfetto to run properly
                        getLogger().info("Setting up the device for Perfetto")
                        setup_perfetto_cmd = xharness_adb() + [
                            'shell',
                            'setprop persist.traced.enable 1'
                        ]
                        RunCommand(setup_perfetto_cmd, verbose=True).run()

                        getLogger().info("Tracing with Perfetto")
                        # Get the max TotalTime from the allResults list in seconds
                        max_startup_time_sec = int(max(int(re.search(r"TotalTime: (\d+)", str(result)).group(1)) for result in allResults) / 1000)
                        perfetto_max_trace_time_sec = max_startup_time_sec * 2 # Set the max trace time to be double the max startup time
                        if max_startup_time_sec > 60:
                            getLogger().error(f"Max startup time is greater than 60 seconds (Max startup time: {max_startup_time_sec}), this means something probably went wrong.")
                            raise Exception("Max startup time is greater than 60 seconds, this means something probably went wrong.")

                        perfetto_cmd = xharness_adb() + [
                            'shell',
                            f'perfetto --background --txt -o {perfetto_device_save_file} --time {perfetto_max_trace_time_sec}s -b 64mb sched freq idle am wm gfx view binder_driver hal dalvik camera input res memory'
                        ]
                        RunCommand(perfetto_cmd, verbose=True).run()

                        # Run the startup test with the trace running (only once)
                        getLogger().info("Running startup test with Perfetto trace running")
                        traced_start = RunCommand(androidHelper.startappcommand, verbose=True)
                        traced_start.run()

                        getLogger().info("Stopping perfetto trace capture")
                        RunCommand(stop_perfetto_cmd, verbose=True).run()
                        perfetto_terminated = True

                        # Pull the trace from the device and store in the traceperfetto directory
                        pull_trace_cmd = xharness_adb() + [
                            'pull',
                            perfetto_device_save_file,
                            os.path.join(os.getcwd(), const.TRACEDIR, f'perfetto_startup_trace_{self.packagename}_{datetime.now().strftime("%Y-%m-%d_%H-%M-%S")}.trace')
                        ]
                        RunCommand(pull_trace_cmd, verbose=True).run()

                        # Delete the trace file on the android device
                        getLogger().info("Deleting the trace file on the device.")
                        delete_trace_cmd = xharness_adb() + [
                            'shell',
                            f'rm -rf {perfetto_device_save_file}'
                        ]
                        RunCommand(delete_trace_cmd, verbose=True).run()

                    finally:
                        if not perfetto_terminated:
                            # If we didn't terminate perfetto, we need to stop it now, although it will time out as well if this manages to fail.
                            getLogger().info("Perfetto not terminated, stopping it now")
                            RunCommand(stop_perfetto_cmd, verbose=True).run()

                        # Restore original persist.traced.enable value if we saved one
                        if original_traced_enable is not None and original_traced_enable != "1":
                            getLogger().info(f"Restoring persist.traced.enable to original value: {original_traced_enable}")
                            restore_traced_cmd = xharness_adb() + [
                                'shell',
                                f'setprop persist.traced.enable {original_traced_enable}'
                            ]
                            RunCommand(restore_traced_cmd, verbose=True).run()

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

            # Get the name, UDID, and version of the device from the output of apple_state above
            # Example output expected (PERFIOS-01 is the device name, 00008101-001A09223E08001E is the UDID, and 17.0.2 is the version):
            #  Connected Devices:
            #    PERFIOS-01 00008101-001A09223E08001E    17.0.2        iPhone iOS
            deviceInfoMatch = re.search(r'Connected Devices:\s+(?P<deviceName>\S+)\s+(?P<deviceUDID>\S+)\s+(?P<deviceVersion>\S+)', apple_state.stdout)
            if deviceInfoMatch:
                deviceName = deviceInfoMatch.group('deviceName')
                deviceUDID = deviceInfoMatch.group('deviceUDID')
                deviceVersion = deviceInfoMatch.group('deviceVersion')
                getLogger().info(f"Device Name: {deviceName}")
                getLogger().info(f"Device UDID: {deviceUDID}")
                getLogger().info(f"Device Version: {deviceVersion}")
            else:
                raise Exception("Device name, UDID, or version not found in the output of apple_state command.")
            
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
            timeToFirstDrawEventEndDateTime = datetime.now() + timedelta(minutes=-10) # This is used to keep track of the latest time to draw end event, we use this to calculate time to draw and also as a reference point for the next iteration log time.
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
                    '--devname', deviceUDID
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
                    '--devname', deviceUDID
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
                        continue

                
                if i == 0: # Use the warmup iteration to get the current device time
                    if len(events) > 0:
                        timeToFirstDrawEventEndDateTime = datetime.strptime(events[-1]['timestamp'], '%Y-%m-%d %H:%M:%S.%f%z')
                        getLogger().info("Time on device: %s", timeToFirstDrawEventEndDateTime)
                        continue

                    getLogger().error("No watchdog events found in the log, this could mean the app crashed or the device clock is not in sync with the host.")
                    raise Exception("No watchdog events found in the log, this could mean the app crashed or the device clock is not in sync with the host.")

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
            sod.runtests(scenarioname=self.scenarioname, dirs=self.dirs or builtdir, upload_to_perflab_container=self.upload_to_perflab_container, artifact=self.traits.artifact)

        elif self.testtype == const.BUILDTIME:
            startup = StartupWrapper()
            if not (self.binlogpath and os.path.exists(os.path.join(const.TRACEDIR, self.binlogpath))):
                raise Exception("For build time measurements a valid binlog path must be provided.")
            self.traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.BUILDTIME, tracename=self.binlogpath, scenarioname=self.scenarioname)
            startup.parsetraces(self.traits)

        elif self.testtype == const.IOSINNERLOOP:
            import hashlib
            from shutil import copy2, copytree
            from performance.common import runninginlab
            from performance.constants import UPLOAD_CONTAINER, UPLOAD_STORAGE_URI, UPLOAD_QUEUE
            from shared.util import helixuploaddir
            import upload

            # --- Validate inputs ---
            if not self.csprojpath:
                raise Exception("--csproj-path is required for iOS inner loop measurements.")
            if not self.bundleid:
                raise Exception("--bundle-id is required for iOS inner loop measurements.")
            is_physical = (self.devicetype == 'device')
            if is_physical and self.deviceid == 'booted':
                detected = iOSHelper.detect_connected_device()
                if not detected:
                    raise Exception("Physical device mode requires a device UDID. "
                                    "Set --device-id or IOS_DEVICE_UDID, or connect a device.")
                self.deviceid = detected

            # --- Cross-arch RID correction for simulator builds ---
            # The .proj defaults RuntimeIdentifier to iossimulator-x64 because
            # the original Helix queue (Mac.iPhone.17.Perf) was Intel x64.
            # Other queues (e.g. Mac.iPhone.13.Perf) are Apple Silicon, where
            # an x64 .app cannot install on an arm64 simulator (mlaunch fails
            # with HE0046 "Failed to find matching arch"). Detect the host
            # arch here and rewrite the RID in --msbuild-args so the build
            # produces a binary matching the simulator we'll deploy to.
            # Physical-device builds (ios-arm64) target the iPhone hardware,
            # not the host, so we never override their RID.
            if not is_physical and self.msbuildargs:
                import platform as _platform
                host_arch = _platform.machine()
                if host_arch == 'arm64' and 'iossimulator-x64' in self.msbuildargs:
                    self.msbuildargs = self.msbuildargs.replace(
                        'iossimulator-x64', 'iossimulator-arm64')
                    getLogger().info(
                        "Cross-arch RID correction: host=arm64, rewrote msbuild "
                        "args to use iossimulator-arm64 (was iossimulator-x64)")
                elif host_arch == 'x86_64' and 'iossimulator-arm64' in self.msbuildargs:
                    self.msbuildargs = self.msbuildargs.replace(
                        'iossimulator-arm64', 'iossimulator-x64')
                    getLogger().info(
                        "Cross-arch RID correction: host=x86_64, rewrote msbuild "
                        "args to use iossimulator-x64 (was iossimulator-arm64)")
                # Keep IOS_RID env var aligned so versionmanager uses the same
                # RID for the linked-DLL lookup below.
                m = re.search(r'/p:RuntimeIdentifier=(\S+)', self.msbuildargs)
                if m:
                    os.environ['IOS_RID'] = m.group(1)

            getLogger().info("iOS inner loop: device_type=%s, device_id=%s", self.devicetype, self.deviceid)
            scenarioprefix = self.scenarioname or "MAUI iOS Build and Deploy"

            os.makedirs(const.TRACEDIR, exist_ok=True)
            # Prefix binlog filenames with runtime flavor to avoid overwrites between runs
            runtime_flavor = os.environ.get('RUNTIME_FLAVOR', '')
            binlog_prefix = f'{runtime_flavor}-' if runtime_flavor else ''
            first_binlog = os.path.join(const.TRACEDIR, f'{binlog_prefix}first-build-and-deploy.binlog')

            # Build base command (no -t:Install for iOS — plain dotnet build)
            base_cmd = ['dotnet', 'build', self.csprojpath]
            if self.configuration:
                base_cmd.extend(['-c', self.configuration])
            if self.framework:
                base_cmd.extend(['-f', self.framework])
            if self.msbuildargs:
                base_cmd.extend(shlex.split(self.msbuildargs.replace(';', ' ')))

            project_dir = os.path.dirname(os.path.abspath(self.csprojpath))
            exename = self.traits.exename

            # --- First build ---
            try:
                RunCommand(base_cmd + [f'-bl:{first_binlog}'], verbose=True).run()
            except CalledProcessError:
                getLogger().error("First build failed. Binlog: %s", first_binlog)
                raise

            # --- Log SDK and workload versions ---
            RunCommand(['dotnet', '--info'], verbose=True).run()
            try:
                from shared.versionmanager import versions_write_json, versions_write_env, get_sdk_versions
                rid = 'ios-arm64' if is_physical else os.environ.get('IOS_RID', 'iossimulator-arm64')
                linked_dir = os.path.join(project_dir, 'obj', self.configuration or 'Debug',
                                          self.framework or 'net11.0-ios', rid, 'linked')
                if os.path.isdir(linked_dir):
                    version_dict = get_sdk_versions(linked_dir, False)
                    versions_file = os.path.join(const.TRACEDIR, f'{binlog_prefix}versions.json')
                    versions_write_json(version_dict, versions_file)
                    versions_write_env(version_dict)
                    getLogger().info("SDK versions: %s", version_dict)
                else:
                    getLogger().warning("Linked DLL dir not found at %s — skipping versions.json", linked_dir)
            except Exception as e:
                getLogger().warning("Could not extract SDK versions: %s", e)

            # --- Device setup + first deploy ---
            iosHelper = iOSHelper()
            try:
                app_bundle = iosHelper.find_app_bundle(project_dir, exename, self.configuration, is_physical=is_physical)
                first_app_size = _measure_app_size(app_bundle)
                getLogger().info("App bundle size: %.2f MB (%d bytes)", first_app_size / 1048576, first_app_size)
                iosHelper.setup_device(self.bundleid, app_bundle, self.deviceid, is_physical=is_physical)
                iosHelper.sign_app_for_device(app_bundle)
                first_install_ms = iosHelper.install_app(app_bundle)
                first_startup_ms = iosHelper.measure_cold_startup(self.bundleid)
                if first_startup_ms < 0:
                    raise RuntimeError("First deploy cold startup measurement failed (watchdog event parsing error)")
                getLogger().info("First deploy: install=%.1f ms, startup=%d ms", first_install_ms, first_startup_ms)

                # Parse first build binlog
                startup = StartupWrapper()
                first_build_report = os.path.join(const.TRACEDIR, 'first-build-and-deploy-perf-lab-report.json')
                startup.reportjson = first_build_report
                saved_upload = self.traits.upload_to_perflab_container
                self.traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.IOSINNERLOOP,
                                       tracename=f'{binlog_prefix}first-build-and-deploy.binlog',
                                       scenarioname=scenarioprefix + " - First Build and Deploy",
                                       upload_to_perflab_container=False)
                startup.parsetraces(self.traits)

                # Merge first build metrics + install/startup → first E2E report
                first_e2e_report = os.path.join(const.TRACEDIR, 'first-debug-e2e-perf-lab-report.json')
                _merge_deploy_report(first_build_report, [first_install_ms], [first_startup_ms], first_e2e_report, app_size_bytes=first_app_size)

                # --- Incremental loop ---
                if not self.editsrcs or not self.editdests:
                    raise Exception("--edit-src and --edit-dest are required for incremental builds")
                if len(self.editsrcs) != len(self.editdests):
                    raise Exception("--edit-src and --edit-dest must have the same number of semicolon-separated paths")

                edit_pairs = []
                for src, dest in zip(self.editsrcs, self.editdests):
                    with open(dest, 'r') as f:
                        original = f.read()
                    with open(src, 'r') as f:
                        modified = f.read()
                    edit_pairs.append((dest, original, modified))

                num_iterations = self.innerloopiterations
                getLogger().info("Starting %d incremental iterations", num_iterations)

                incremental_startup_results = []
                incremental_install_results = []
                incremental_app_size_results = []
                aggregated_counters = {}
                report_template = None
                intermediate_binlogs = []

                for iteration in range(1, num_iterations + 1):
                    getLogger().info("=== Incremental iteration %d/%d ===", iteration, num_iterations)

                    # Toggle source files (odd → modified, even → original)
                    for dest, original, modified in edit_pairs:
                        content = modified if iteration % 2 == 1 else original
                        with open(dest, 'w') as f:
                            f.write(content)

                    # Build
                    iter_binlog_name = '%sincremental-build-and-deploy-%d.binlog' % (binlog_prefix, iteration)
                    iter_binlog = os.path.join(const.TRACEDIR, iter_binlog_name)
                    try:
                        RunCommand(base_cmd + [f'-bl:{iter_binlog}'], verbose=True).run()
                    except CalledProcessError:
                        getLogger().error("Incremental build %d failed. Binlog: %s", iteration, iter_binlog)
                        raise

                    # Sign (device only — no-op for simulator)
                    iosHelper.sign_app_for_device(app_bundle)

                    # Measure app size after incremental build
                    iter_app_size = _measure_app_size(app_bundle)
                    getLogger().info("Iteration %d app bundle size: %.2f MB", iteration, iter_app_size / 1048576)

                    # Install + startup
                    install_ms = iosHelper.install_app(app_bundle)
                    startup_ms = iosHelper.measure_cold_startup(self.bundleid)
                    if startup_ms < 0:
                        raise RuntimeError("Incremental deploy %d cold startup measurement failed (watchdog event parsing error)" % iteration)
                    getLogger().info("Iteration %d: install=%.1f ms, startup=%d ms", iteration, install_ms, startup_ms)

                    incremental_install_results.append(install_ms)
                    incremental_startup_results.append(startup_ms)
                    incremental_app_size_results.append(iter_app_size)
                    intermediate_binlogs.append(iter_binlog)

                    # Parse iteration binlog → temp report
                    iter_report = os.path.join(const.TRACEDIR, 'incremental-build-report-%d.json' % iteration)
                    startup.reportjson = iter_report
                    self.traits.add_traits(overwrite=True, apptorun="app", startupmetric=const.IOSINNERLOOP,
                                           tracename=iter_binlog_name,
                                           scenarioname=scenarioprefix + " - Incremental Build and Deploy",
                                           upload_to_perflab_container=False)
                    # Clear stale traces upload dir so copytree in parsetraces doesn't collide
                    helix_upload_dir = helixuploaddir()
                    if helix_upload_dir is not None:
                        traces_upload = os.path.join(helix_upload_dir, 'traces')
                        if os.path.exists(traces_upload):
                            rmtree(traces_upload)

                    startup.parsetraces(self.traits)

                    # Extract counters from temp report (may not exist on local runs)
                    if os.path.exists(iter_report):
                        with open(iter_report, 'r') as f:
                            iter_data = json.load(f)
                        test_obj = iter_data["tests"][0]
                        if report_template is None:
                            report_template = {k: v for k, v in test_obj.items() if k != "counters"}
                        for counter in test_obj["counters"]:
                            name = counter["name"]
                            if name not in aggregated_counters:
                                aggregated_counters[name] = {
                                    "name": name,
                                    "topCounter": counter.get("topCounter", False),
                                    "defaultCounter": counter.get("defaultCounter", False),
                                    "higherIsBetter": counter.get("higherIsBetter", False),
                                    "metricName": counter.get("metricName", "ms"),
                                    "results": []
                                }
                            aggregated_counters[name]["results"].extend(counter.get("results", []))
                        os.remove(iter_report)

                # --- Build final incremental report ---
                incremental_e2e_report = os.path.join(const.TRACEDIR, 'incremental-debug-e2e-perf-lab-report.json')
                final_counters = list(aggregated_counters.values())
                final_counters.append(_make_counter("Install Time", "ms", incremental_install_results))
                final_counters.append(_make_counter("Cold Startup Time", "ms", incremental_startup_results, top=True))
                final_counters.append(_make_counter("App Bundle Size", "bytes", incremental_app_size_results, top=True))

                final_test = dict(report_template or {})
                final_test["counters"] = final_counters
                with open(incremental_e2e_report, 'w') as f:
                    json.dump({"tests": [final_test]}, f, indent=2)

                # --- Persist reports for local runs ---
                results_dir = os.path.join(os.getcwd(), 'results', os.environ.get('RUNTIME_FLAVOR', 'unknown'))
                try:
                    os.makedirs(results_dir, exist_ok=True)
                    for report in [first_e2e_report, incremental_e2e_report]:
                        copy2(report, os.path.join(results_dir, os.path.basename(report)))
                except Exception as e:
                    getLogger().warning("Failed to persist reports: %s", e)

                # --- Upload to Helix ---
                self.traits.add_traits(overwrite=True, upload_to_perflab_container=saved_upload)
                helix_upload_dir = helixuploaddir()
                if runninginlab() and helix_upload_dir is not None:
                    traces_upload = os.path.join(helix_upload_dir, 'traces')
                    if os.path.exists(traces_upload):
                        rmtree(traces_upload)
                    copytree(const.TRACEDIR, traces_upload, dirs_exist_ok=True)
                    if self.traits.upload_to_perflab_container:
                        for report_path in [first_e2e_report, incremental_e2e_report]:
                            upload_code = upload.upload(report_path, UPLOAD_CONTAINER, UPLOAD_QUEUE, UPLOAD_STORAGE_URI)
                            if upload_code != 0:
                                sys.exit(upload_code)

            finally:
                iosHelper.cleanup(skip_uninstall=True)
