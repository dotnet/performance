'''
MAUI Android Deploy Time Measurement
Orchestrates first deploy → file edit → incremental deploy → parse binlogs.
'''
import os
from shared.runner import TestTraits, Runner

EXENAME = 'MauiAndroidInnerLoop'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME,
                        guiapp='false',
                        )
    Runner(traits).run()
