'''
Module for running scenario tasks
'''

import sys
import os

from logging import getLogger
from collections import namedtuple
from argparse import ArgumentParser
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

        crossgen2parser = subparsers.add_parser(const.CROSSGEN2)
        crossgen2parser.add_argument('--core-root', dest='coreroot', type=str, required=True)
        crossgen2parser.add_argument('--single', dest='single', type=str, required=False)
        crossgen2parser.add_argument('--composite', dest='composite', type=str, required=False)
        self.add_common_arguments(crossgen2parser)

        sodparser = subparsers.add_parser(const.SOD)
        sodparser.add_argument('--dirs', dest='dirs', type=str)
        self.add_common_arguments(sodparser)

        args = parser.parse_args()

        if not args.testtype:
            getLogger().error("Please specify a test type: %s" % testtypes)
            sys.exit(1)

        self.testtype = args.testtype
    
        if self.testtype == const.SDK:
            self.sdktype = args.sdktype

        if self.testtype == const.CROSSGEN:
            self.crossgenfile = args.testname
            self.coreroot = args.coreroot

        if self.testtype == const.CROSSGEN2:
            self.coreroot = args.coreroot
            self.singlefile = args.single
            self.compositefile = args.composite

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
            crossgenargs = '/nologo /p %s %s\%s' % (self.coreroot, self.coreroot, self.crossgenfile)
            if self.coreroot is not None and not os.path.isdir(self.coreroot):
                getLogger().error('Cannot find CORE_ROOT at %s', self.coreroot)
                return

            self.traits.add_traits(overwrite=True,
                                   startupmetric=const.STARTUP_PROCESSTIME,
                                   workingdir=self.coreroot,
                                   appargs=crossgenargs
                                   )
            self.traits.add_traits(overwrite=False,
                                   scenarioname='Crossgen Throughput - %s' % self.crossgenfile,
                                   scenariotypename='%s - %s' % (const.SCENARIO_NAMES[const.CROSSGEN], self.crossgenfile),
                                   apptorun='%s\%s' % (self.coreroot, crossgenexe),
                                  ) 
            startup.runtests(self.traits)
           
        elif self.testtype == const.CROSSGEN2:
            startup = StartupWrapper()
            if self.coreroot is not None and not os.path.isdir(self.coreroot):
                getLogger().error('Cannot find CORE_ROOT at %s', self.coreroot)
                sys.exit(1)
            if bool(self.singlefile) == bool(self.compositefile):
                getLogger().error("Please specify either --single <single assembly name> or --composite <absolute path of rsp file>")
                sys.exit(1)
        
            compiletype = const.CROSSGEN2_COMPOSITE if self.compositefile else const.CROSSGEN2_SINGLEFILE

            if compiletype == const.CROSSGEN2_SINGLEFILE:
                referencefilenames = ['System.*.dll', 'Microsoft.*.dll', 'netstandard.dll', 'mscorlib.dll']
                referencefiles = [os.path.join(self.coreroot, filename) for filename in referencefilenames]
                # single assembly filename: example.dll
                filename, ext = os.path.splitext(self.singlefile)
                outputdir = os.path.join(self.coreroot, 'out')
                if not os.path.exists(outputdir):
                    os.mkdir(outputdir)
                outputfile = os.path.join(outputdir, filename+'.ni'+ext )
                crossgen2args = '%s -o %s -O %s' % (os.path.join(self.coreroot, self.singlefile), outputfile, ' -r '.join(['']+referencefiles))
            
            elif compiletype == const.CROSSGEN2_COMPOSITE:
                # composite rsp filename: ..\example.dll.rsp
                dllname, _ = os.path.splitext(os.path.basename(self.compositefile))
                filename, ext = os.path.splitext(dllname)
                outputdir = os.path.join(self.coreroot, 'composite.out')
                if not os.path.exists(outputdir):
                    os.mkdir(outputdir)
                outputfile = os.path.join(outputdir, filename+'.ni'+ext )
                crossgen2args = '--composite -o %s -O @%s' % (outputfile, self.compositefile)

            self.traits.add_traits(overwrite=True,
                                   startupmetric=const.STARTUP_CROSSGEN2,
                                   workingdir=self.coreroot,
                                   appargs='%s %s' % (os.path.join('crossgen2', 'crossgen2.dll'), crossgen2args)
                                   )
            self.traits.add_traits(overwrite=False,
                                   scenarioname='Crossgen2 Throughput - %s - %s' % ( compiletype, filename),
                                   apptorun=os.path.join(self.coreroot, 'CoreRun%s' % extension())
                                  ) 
            startup.runtests(self.traits)

        elif self.testtype == const.SOD:
            sod = SODWrapper()
            builtdir = const.PUBDIR if os.path.exists(const.PUBDIR) else None
            if not builtdir:
                builtdir = const.BINDIR if os.path.exists(const.BINDIR) else None
            if not (self.dirs or builtdir):
                raise Exception("Dirs was not passed in and neither %s nor %s exist" % (const.PUBDIR, const.BINDIR))
            sod.runtests(scenarioname=self.scenarioname, dirs=self.dirs or builtdir)
