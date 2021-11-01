'''
C# Console app
'''
from shared.runner import TestTraits, Runner

EXENAME = 'MauiMacCatalystDefault'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        guiapp='false',
                        )
    Runner(traits).run()
