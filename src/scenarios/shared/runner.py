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
             'environmentvariables',
             'iterations',
             'timeout',
             'warmup',
             'workingdir',
             'iterationsetup',
             'setupargs',
             'processwillexit',
             'measurementdelay'
             )

# These are the kinds of scenarios we run. Default here indicates whether ALL
# scenarios should try and run a given test type.
testtypes = {const.STARTUP: False,
             const.SDK: False}

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
        elif self.testtype == const.SDK:
            startup = StartupWrapper()
            envlistbuild = 'DOTNET_MULTILEVEL_LOOKUP=0'
            envlistcleanbuild= ';'.join(['MSBUILDDISABLENODEREUSE=1', envlistbuild])
            # clean build
            startup.runtests(scenarioname=self.traits.scenarioname,
                             exename=self.traits.exename,
                             guiapp=self.traits.guiapp,
                             startupmetric=const.STARTUP_PROCESSTIME,
                             appargs='build',
                             timeout=self.traits.timeout,
                             warmup='true',
                             iterations=self.traits.iterations,
                             scenariotypename='%s (%s)' % (const.SCENARIO_NAMES[const.SDK], 'Clean Build'),
                             apptorun=const.DOTNET,
                             iterationsetup='py' if sys.platform == 'win32' else 'py3',
                             setupargs='-3 %s' % const.ITERATION_SETUP_FILE if sys.platform == 'win32' else const.ITERATION_SETUP_FILE,
                             workingdir=const.TMPDIR,
                             environmentvariables=envlistcleanbuild,
                             processwillexit=self.traits.processwillexit,
                             measurementdelay=self.traits.measurementdelay
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
                             scenariotypename='%s (%s)' % (const.SCENARIO_NAMES[const.SDK], 'Build(no changes)'),
                             apptorun=const.DOTNET,
                             iterationsetup=None,
                             setupargs=None,
                             workingdir=const.TMPDIR,
                             environmentvariables=envlistbuild,
                             processwillexit=self.traits.processwillexit,
                             measurementdelay=self.traits.measurementdelay
                             )