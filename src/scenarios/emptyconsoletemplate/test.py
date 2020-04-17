'''
C# Console app
'''
from shared.runner import TestTraits, Runner

SCENARIONAME = 'Empty C# Console Template'
EXENAME = 'emptycsconsoletemplate'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME)
    traits.add_traits(#startupmetric='TimeToMain',
                      startup=True,
                      sdk=True,
                      guiapp='false',)
    Runner(traits).run()
