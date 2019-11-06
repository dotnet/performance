'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Empty VB Console Template'
EXENAME = 'emptyvbconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(scenarioname=SCENARIONAME, 
                        exename=EXENAME, 
                        startupmetric='TimeToMain',
                        startup=True,
                        guiapp='false',
                        )
    Runner(traits).run()
