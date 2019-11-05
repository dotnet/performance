'''
C# Console app
'''
import sys
import os
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Empty VB Console Template'
EXENAME = 'emptyvbconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(scenarioname=SCENARIONAME, 
                        exename=EXENAME, 
                        startupmetric='TimeToMain',
                        startup=True,
                        guiapp='false', # string passed through to tool
                        )
    Runner(traits).run()
