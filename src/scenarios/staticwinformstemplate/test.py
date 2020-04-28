'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Winforms Template'
EXENAME = 'staticwinformstemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='GenericStartup',
                        guiapp='true',
                        processwillexit='false',
                        measurementdelay='5'
                        )
    Runner(traits).run()
