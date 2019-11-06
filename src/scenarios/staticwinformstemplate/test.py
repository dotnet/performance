'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Winforms Template'
EXENAME = 'staticwinformstemplate'

if __name__ == "__main__":
    traits = TestTraits(scenarioname=SCENARIONAME, 
                        exename=EXENAME, 
                        startupmetric='GenericStartup',
                        startup=True,
                        guiapp='true',
                        processwillexit='false',
                        measurementdelay='5'
                        )
    Runner(traits).run()
