'''
Module for running scenario tasks
'''

import sys
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
        # if testtype == 'sdk' and self.traits.sdk:
        #     print("sdk")
        #     startup = StartupWrapper()
        #     startup.runtests(**self.traits._asdict(),
        #         scenariotypename='Build No Changes')
        #     # fix some other traits
        #     startup.runtests(**self.traits._asdict(),
        #         scenariotypename='Rebuild')