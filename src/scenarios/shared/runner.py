'''
Module for running scenario tasks
'''

import sys
import os

from logging import getLogger
from collections import namedtuple
from argparse import ArgumentParser
from argparse import RawTextHelpFormatter
from shared.crossgen import CrossgenArguments
from shared.startup import StartupWrapper
from shared.util import publishedexe, extension, pythoncommand, iswin
from shared.sod import SODWrapper
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

        elif self.testtype == const.SOD:
            sod = SODWrapper()
            builtdir = const.PUBDIR if os.path.exists(const.PUBDIR) else None
            if not builtdir:
                builtdir = const.BINDIR if os.path.exists(const.BINDIR) else None
            if not (self.dirs or builtdir):
                raise Exception("Dirs was not passed in and neither %s nor %s exist" % (const.PUBDIR, const.BINDIR))
            sod.runtests(scenarioname=self.scenarioname, dirs=self.dirs or builtdir, artifact=self.traits.artifact)
