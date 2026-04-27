'''
C# Console app
'''
from shared.runner import TestTraits, Runner

EXENAME = 'staticvbconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='TimeToMain2',
                        guiapp='false',
                        )
    Runner(traits).run()
