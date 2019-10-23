'''
Module for running scenario tasks
'''

import sys
import os

from logging import getLogger
from collections import namedtuple
from argparse import ArgumentParser
from shared.startup import StartupWrapper
from shared.util import publishedexe
from shared import const
from performance.logger import setup_loggers

reqfields = ('scenarioname',
             'exename',
             )
optfields = ('guiapp',
             'startupmetric',
             'appargs',
             'iterations',
             'timeout',
             'warmup',
             'workingdir',
             'iterationsetup',
             'setupargs',
             )

# These are the kinds of scenarios we run. Default here indicates whether ALL
# scenarios should try and run a given test type.
testtypes = {const.STARTUP: True,
             const.SDK: True}

TestTraits = namedtuple('TestTraits', 
                        reqfields  + tuple(testtypes.keys()) + optfields, 
                        defaults=tuple(testtypes.values()) + (None,) * len(optfields))


class Runner:
    '''
    Wrapper for running all the things
    '''

    def __init__(self, traits: TestTraits):
        self.traits = traits
        self.testtype = None
        setup_loggers(True)

    def parseargs(self):
        '''
        Parses input args to the script
        '''
        parser = ArgumentParser()
        parser.add_argument('testtype', choices=testtypes, type=str.lower)
        args = parser.parse_args()
        if not getattr(self.traits, args.testtype):
            getLogger().error("Test type %s is not supported by this scenario", args.testtype)
            sys.exit(1)
        self.testtype = args.testtype

    def run(self):
        '''
        Runs the specified scenario
        '''
        self.parseargs()
        if self.testtype == const.STARTUP:
            startup = StartupWrapper()
            startup.runtests(**self.traits._asdict(),
                             scenariotypename=const.SCENARIO_NAMES[const.STARTUP],
                             apptorun=publishedexe(self.traits.exename))
        # TODO: what if we want to use other tools to test SDK in the future? adding more tests?
        # TODO: for SDK tests, choosing to run clean build or build(no changes) in test.py will make different measurements
        # TODO: more scalable --> ex: doing clean build and build no change in parallel
        # TODO: created another branch for separating clean build, build no change, and other scenarios  --> add more subcommands
        elif self.testtype == const.SDK:
            startup = StartupWrapper()
            # clean build
            startup.runtests(scenarioname=self.traits.scenarioname,
                             exename=self.traits.exename,
                             guiapp=self.traits.guiapp,
                             startupmetric=const.STARTUP_PROCESSTIME,
                             appargs='build',
                             timeout=self.traits.timeout,
                             warmup='false',
                             iterations=self.traits.iterations,
                             scenariotypename='%s (%s)' % (const.SCENARIO_NAMES[const.SDK], const.BUILD_CLEAN),
                             apptorun=const.DOTNET,  # TODO: not using traits.exename here bc we want to use dotnet.exe
                             iterationsetup=const.PYTHON,
                             setupargs='-3 %s' % const.ITERATION_SETUP_FILE,
                             workingdir=const.APPDIR,
                             )
            # build(no changes)
            startup.runtests(scenarioname=self.traits.scenarioname,
                             exename=self.traits.exename,
                             guiapp=self.traits.guiapp,
                             startupmetric=const.STARTUP_PROCESSTIME,
                             appargs='build',
                             timeout=self.traits.timeout,
                             warmup='true',
                             iterations=self.traits.iterations,
                             scenariotypename='%s (%s)' % (const.SCENARIO_NAMES[const.SDK], const.BUILD_NO_CHANGES),
                             apptorun=const.DOTNET,
                             iterationsetup=None,
                             setupargs=None,
                             workingdir=const.APPDIR,
                             )