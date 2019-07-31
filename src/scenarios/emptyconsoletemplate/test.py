'''
C# Console app
'''
import sys
import os
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Empty C# Console Template'
EXENAME = 'emptycsconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(scenarioname=SCENARIONAME, 
                        exename=EXENAME, 
                        startupmetric='TimeToMain',
                        guiapp='false', # string passed through to tool
                        )
    Runner(traits).run()
