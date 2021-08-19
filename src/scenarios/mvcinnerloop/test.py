'''
C# Console app
'''
from shared.runner import TestTraits, Runner
import os

EXENAME = 'mvcinnerloop'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='InnerLoop',
                        guiapp='false',
                        innerloopcommandargs='-c "from shutil import copyfile; copyfile(\'src%sProgram.cs\', \'app%sProgram.cs\')"' % (os.sep, os.sep) ,
                        projext = '.csproj',
                        processwillexit='false',
                        measurementdelay='20'
                        )
    Runner(traits).run()
