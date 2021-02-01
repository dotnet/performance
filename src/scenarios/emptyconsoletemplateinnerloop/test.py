'''
C# Console app
'''
from shared.runner import TestTraits, Runner

EXENAME = 'emptyconsoletemplateinnerloop'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='InnerLoop',
                        guiapp='false',
                        innerloopcommandargs='-c "from shutil import copyfile; copyfile(\'src/Program.cs\', \'app/Program.cs\')"' 
                        )
    Runner(traits).run()
