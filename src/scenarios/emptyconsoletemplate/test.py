'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Empty C# Console Template'
EXENAME = 'emptycsconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='TimeToMain',
                        startup=True,
                        sdk=True,
                        guiapp='false',
                        # appargs=None,
                        iterations=None,
                        timeout=None,
                        warmup=None,
                        workingdir=None,
                        # iterationsetup=None,
                        # setupargs=None,
                        # iterationcleanup=None,
                        # cleanupargs=None,
                        processwillexit=None,
                        measurementdelay=None
                        )
    Runner(traits).run()

