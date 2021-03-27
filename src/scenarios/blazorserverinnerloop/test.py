'''
C# Console app
'''
from shared.runner import TestTraits, Runner
import os

EXENAME = 'blazorserverinnerloop'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='InnerLoop',
                        guiapp='false',
                        innerloopcommandargs='-c "from shutil import copyfile; copyfile(\'src%sStartup.cs\', \'app%sStartup.cs\')"' % (os.sep, os.sep) ,
                        projext = '.csproj'
                        )
    Runner(traits).run()
