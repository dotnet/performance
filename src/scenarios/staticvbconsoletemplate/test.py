'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Static VB Console Template'
EXENAME = 'staticvbconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='TimeToMain',
                        startup=True,
                        guiapp='false',
                        )
    Runner(traits).run()
