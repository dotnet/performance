'''
MAUI iOS Inner Loop (Debug End-2-End) Time Measurement
Orchestrates first build-deploy-startup → file edit → incremental build-deploy-startup → parse binlogs and startup times.
'''
from shared.runner import TestTraits, Runner

EXENAME = 'MauiiOSInnerLoop'


if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME,
                        guiapp='false',
                        )
    Runner(traits).run()
