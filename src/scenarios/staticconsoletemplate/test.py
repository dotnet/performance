'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Static Console Template'
EXENAME = 'staticconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='TimeToMain',
                        startup=True,
                        guiapp='false',
                        )
    Runner(traits).run()
