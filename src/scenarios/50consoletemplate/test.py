'''
C# Console app
'''
import sys
import os
from shared.runner import TestTraits, Runner

SCENARIONAME = '.NET Core 5.0 Console Template'
EXENAME = '50consoletemplate'

if __name__ == "__main__":
    traits = TestTraits(scenarioname=SCENARIONAME, 
                        exename=EXENAME, 
                        startupmetric='TimeToMain',
                        guiapp='false', # string passed through to tool
                        )
    Runner(traits).run()
