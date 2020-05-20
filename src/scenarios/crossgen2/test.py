'''
C# Console app
'''
from shared.runner import TestTraits, Runner

EXENAME = 'crossgen2'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME,
                        startupmetric='Crossgen2',
                        guiapp='false',
                        )
    Runner(traits).run()
