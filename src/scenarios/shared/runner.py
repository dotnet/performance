'''
Module for running scenario tasks
'''

import sys
import os

from logging import getLogger
from collections import namedtuple
from argparse import ArgumentParser
from shared.startup import StartupWrapper
from shared.sod import SODWrapper
from shared.util import publishedexe, extension
from shared import const
from performance.logger import setup_loggers


reqfields = ('exename',
            )
optfields = ('guiapp',
             'startupmetric',
             'appargs',
             'environmentvariables',
             'iterations',
             'timeout',
             'warmup',
             'workingdir',
             'iterationsetup',
             'setupargs',
             'iterationcleanup',
             'cleanupargs',
             'processwillexit',
             'measurementdelay'
             )


TestTraits = namedtuple('TestTraits',
                        reqfields + optfields,
                        defaults=(None,) * len(optfields))

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
        setup_loggers(True)

    def parseargs(self):
        '''
        Parses input args to the script
        '''
        parser = ArgumentParser()
        subparsers = parser.add_subparsers(title='subcommands for scenario tests', required=True, dest='testtype')
        startupparser = subparsers.add_parser(const.STARTUP)
        self.add_common_arguments(startupparser)

        sdkparser = subparsers.add_parser(const.SDK)
        sdkparser.add_argument('sdktype', choices=[const.CLEAN_BUILD, const.BUILD_NO_CHANGE, const.NEW_CONSOLE], type=str.lower)
        self.add_common_arguments(sdkparser)

        crossgenparser = subparsers.add_parser(const.CROSSGEN)
        crossgenparser.add_argument('--test-name', dest='testname', type=str, required=True)
        crossgenparser.add_argument('--core-root', dest='coreroot', type=str, required=True)
        self.add_common_arguments(crossgenparser)

        sodparser = subparsers.add_parser(const.SOD)
        sodparser.add_argument('--dirs', dest='dirs', type=str)
        self.add_common_arguments(sodparser)

        args = parser.parse_args()

        self.testtype = args.testtype

        if self.testtype == const.SDK:
            self.sdktype = args.sdktype
        if args.scenarioname:
            self.scenarioname = args.scenarioname

        if self.testtype == const.CROSSGEN:
            self.crossgenfile = args.testname
            self.coreroot = args.coreroot

        if self.testtype == const.SOD:
            self.dirs = args.dirs

    
    def add_common_arguments(self, parser: ArgumentParser):
        "Common arguments to add to subparsers"
        parser.add_argument('--scenario-name',
                            dest='scenarioname')

        
    def run(self):
        '''
        Runs the specified scenario
        '''
        self.parseargs()
        if self.testtype == const.STARTUP:
            startup = StartupWrapper()
            startup.runtests(**self.traits._asdict(),
                             scenarioname=self.scenarioname,
                             scenariotypename=const.SCENARIO_NAMES[const.STARTUP],
                             apptorun=publishedexe(self.traits.exename))
        elif self.testtype == const.SDK:
            startup = StartupWrapper()
            envlistbuild = 'DOTNET_MULTILEVEL_LOOKUP=0'
            envlistcleanbuild= ';'.join(['MSBUILDDISABLENODEREUSE=1', envlistbuild])
            # clean build
            if self.sdktype == const.CLEAN_BUILD:
                startup.runtests(scenarioname=self.scenarioname,
                                exename=self.traits.exename,
                                guiapp=self.traits.guiapp,
                                startupmetric=const.STARTUP_PROCESSTIME,
                                appargs='build',
                                timeout=self.traits.timeout,
                                warmup='true',
                                iterations=self.traits.iterations,
                                scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.CLEAN_BUILD),
                                apptorun=const.DOTNET,
                                iterationsetup='py' if sys.platform == 'win32' else 'py3',
                                setupargs='-3 %s setup_build' % const.ITERATION_SETUP_FILE if sys.platform == 'win32' else const.ITERATION_SETUP_FILE,
                                iterationcleanup='py' if sys.platform == 'win32' else 'py3',
                                cleanupargs='-3 %s cleanup' % const.ITERATION_SETUP_FILE if sys.platform == 'win32' else const.ITERATION_SETUP_FILE,
                                workingdir= const.APPDIR if not self.traits.workingdir else os.path.join(const.APPDIR, self.traits.workingdir),
                                environmentvariables=envlistcleanbuild,
                                processwillexit=self.traits.processwillexit,
                                measurementdelay=self.traits.measurementdelay
                             )
            # build(no changes)
            if self.sdktype == const.BUILD_NO_CHANGE:
                startup.runtests(scenarioname=self.scenarioname,
                                exename=self.traits.exename,
                                guiapp=self.traits.guiapp,
                                startupmetric=const.STARTUP_PROCESSTIME,
                                appargs='build',
                                timeout=self.traits.timeout,
                                warmup='true',
                                iterations=self.traits.iterations,
                                scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.BUILD_NO_CHANGE),
                                apptorun=const.DOTNET,
                                iterationsetup=None,
                                setupargs=None,
                                iterationcleanup=None,
                                cleanupargs=None,
                                workingdir= const.APPDIR if not self.traits.workingdir else os.path.join(const.APPDIR, self.traits.workingdir),
                                environmentvariables=envlistbuild,
                                processwillexit=self.traits.processwillexit,
                                measurementdelay=self.traits.measurementdelay
                                )
            # new console
            if self.sdktype == const.NEW_CONSOLE:
                startup.runtests(scenarioname=self.scenarioname,
                                exename=self.traits.exename,
                                guiapp=self.traits.guiapp,
                                startupmetric=const.STARTUP_PROCESSTIME,
                                appargs='new console',
                                timeout=self.traits.timeout,
                                warmup='true',
                                iterations=self.traits.iterations,
                                scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.NEW_CONSOLE),
                                apptorun=const.DOTNET,
                                iterationsetup='py' if sys.platform == 'win32' else 'py3',
                                setupargs='-3 %s setup_new' % const.ITERATION_SETUP_FILE if sys.platform == 'win32' else const.ITERATION_SETUP_FILE,
                                iterationcleanup='py' if sys.platform == 'win32' else 'py3',
                                cleanupargs='-3 %s cleanup' % const.ITERATION_SETUP_FILE if sys.platform == 'win32' else const.ITERATION_SETUP_FILE,
                                workingdir= const.APPDIR if not self.traits.workingdir else os.path.join(const.APPDIR, self.traits.workingdir),
                                environmentvariables=envlistcleanbuild,
                                processwillexit=self.traits.processwillexit,
                                measurementdelay=self.traits.measurementdelay
                                )

        elif self.testtype == const.CROSSGEN:
            startup = StartupWrapper()
            crossgenexe = 'crossgen%s' % extension()
            crossgenargs = '/nologo /p %s %s\%s' % (self.coreroot, self.coreroot, self.crossgenfile)
            if self.coreroot is not None and not os.path.isdir(self.coreroot):
                getLogger().error('Cannot find CORE_ROOT at %s', self.coreroot)
                return

            startup.runtests(scenarioname='Crossgen Throughput - %s' % self.crossgenfile,
                             exename=self.traits.exename,
                             guiapp=self.traits.guiapp,
                             startupmetric=const.STARTUP_PROCESSTIME,
                             appargs=crossgenargs,
                             timeout=self.traits.timeout,
                             warmup='true',
                             iterations=self.traits.iterations,
                             scenariotypename='%s - %s' % (const.SCENARIO_NAMES[const.CROSSGEN], self.crossgenfile),
                             apptorun='%s\%s' % (self.coreroot, crossgenexe),
                             iterationsetup=None,
                             setupargs=None,
                             workingdir=self.coreroot,
                             processwillexit=self.traits.processwillexit,
                             measurementdelay=self.traits.measurementdelay,
                             environmentvariables=None,
                             iterationcleanup=None,
                             cleanupargs=None,
                             )
        elif self.testtype == const.SOD:
            sod = SODWrapper()
            builtdir = const.PUBDIR if os.path.exists(const.PUBDIR) else None
            if not builtdir:
                builtdir = const.BINDIR if os.path.exists(const.BINDIR) else None
            if not (self.dirs or builtdir):
                raise Exception("Dirs was not passed in and neither %s nor %s exist" % (const.PUBDIR, const.BINDIR))
            sod.runtests(scenarioname=self.scenarioname, dirs=self.dirs or builtdir)