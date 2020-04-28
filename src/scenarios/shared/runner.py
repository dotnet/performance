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
from shared.util import publishedexe, extension, pythoncommand, iswin
from shared import const
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
        setup_loggers(True)

    def parseargs(self):
        '''
        Parses input args to the script
        '''
        parser = ArgumentParser()
        subparsers = parser.add_subparsers(title='subcommands for scenario tests', dest='testtype')
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

        if not args.testtype:
            getLogger().error("Please specify a test type: %s" % list((testtypes.keys())))
            sys.exit(1)

        if not getattr(self.traits, args.testtype):
            getLogger().error("Test type %s is not supported by this scenario", args.testtype)
            sys.exit(1)
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
            self.traits.add_traits(overwrite=False,
                                   environmentvariables='COMPlus_EnableEventLog=1' if not iswin() else ''
                                   )
            startup.runtests(**self.traits.get_all_traits(),
                             scenarioname=self.scenarioname,
                             scenariotypename=const.SCENARIO_NAMES[const.STARTUP],
                             apptorun=publishedexe(self.traits.exename),
                             )

        elif self.testtype == const.SDK:
            startup = StartupWrapper()
            envlistbuild = 'DOTNET_MULTILEVEL_LOOKUP=0'
            envlistcleanbuild = ';'.join(['MSBUILDDISABLENODEREUSE=1', envlistbuild])
            # clean build
            if self.sdktype == const.CLEAN_BUILD:
                self.traits.add_traits(
                    overwrite=False,
                    appargs='build',
                    iterationsetup=pythoncommand(),
                    setupargs='%s %s setup_build' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    iterationcleanup=pythoncommand(),
                    cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    workingdir=const.APPDIR,
                    environmentvariables=envlistcleanbuild,
                )
                self.traits.add_traits(overwrite=True, startupmetric=const.STARTUP_PROCESSTIME)
                startup.runtests(**self.traits.get_all_traits(),
                                 scenarioname=self.scenarioname,
                                 scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.CLEAN_BUILD),
                                 apptorun=const.DOTNET
                                 )

            # build(no changes)
            if self.sdktype == const.BUILD_NO_CHANGE:
                self.traits.add_traits(
                    overwrite=False,
                    appargs='build',
                    workingdir=const.APPDIR,
                    environmentvariables=envlistbuild
                )
                self.traits.add_traits(overwrite=True, startupmetric=const.STARTUP_PROCESSTIME)
                startup.runtests(**self.traits.get_all_traits(),
                                 scenarioname=self.scenarioname,
                                 scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.BUILD_NO_CHANGE),
                                 apptorun=const.DOTNET
                                 )

            # new console
            if self.sdktype == const.NEW_CONSOLE:
                self.traits.add_traits(
                    overwrite=False,
                    appargs='new console',
                    iterationsetup=pythoncommand(),
                    setupargs='%s %s setup_new' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    iterationcleanup=pythoncommand(),
                    cleanupargs='%s %s cleanup' % ('-3' if iswin() else '', const.ITERATION_SETUP_FILE),
                    workingdir=const.APPDIR
                )
                self.traits.add_traits(overwrite=True, startupmetric=const.STARTUP_PROCESSTIME)
                startup.runtests(**self.traits.get_all_traits(),
                                 apptorun=const.DOTNET,
                                 scenarioname=self.scenarioname,
                                 scenariotypename='%s_%s' % (const.SCENARIO_NAMES[const.SDK], const.NEW_CONSOLE),
                                 )

        elif self.testtype == const.CROSSGEN:
            startup = StartupWrapper()
            crossgenexe = 'crossgen%s' % extension()
            crossgenargs = '/nologo /p %s %s\%s' % (
                self.coreroot, self.coreroot, self.crossgenfile)
            if self.coreroot is not None and not os.path.isdir(self.coreroot):
                getLogger().error('Cannot find CORE_ROOT at %s', self.coreroot)
                return

            self.traits.add_trait(key='startupmetric', value=const.STARTUP_PROCESSTIME, overwrite=True)
            self.tratis.add_trait(key='workingdir', value=self.coreroot, overwrite=True)
            self.traits.add_trait(key='appargs', value=crossgenargs, overwrite=True)
            self.traits.add_traits(overwrite=True,
                                   startupmetric=const.STARTUP_PROCESSTIME,
                                   workingdir=self.coreroot,
                                   appargs=crossgenargs
                                   )
            startup.runtests(*self.traits.get_all_traits(),
                             scenarioname='Crossgen Throughput - %s' % self.crossgenfile,
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
