'''
C# Console app
'''
from shared.runner import TestTraits, Runner
import os

EXENAME = 'aspwebtemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='sdk',
                        guiapp='false'
                        )
    Runner(traits).run()
