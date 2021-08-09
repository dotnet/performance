'''
C# Console app
'''
from shared.runner import TestTraits, Runner
import os

EXENAME = 'mvcdotnetwatch'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='DotnetWatch',
                        guiapp='false',
                        innerloopcommandargs='-c "from shutil import copyfile, move; copyfile(\'app%sProgram.cs\', \'src%sProgram2.cs\');copyfile(\'src%sProgram.cs\', \'app%sProgram.cs\'); move(\'src%sProgram2.cs\', \'src%sProgram.cs\')"' % (os.sep, os.sep, os.sep, os.sep, os.sep, os.sep) ,
                        projext = '.csproj',
                        processwillexit='false',
                        measurementdelay='20',
                        iterations='5',
                        runwithoutexit='true',
                        hotreloaditers = '2',
                        )
    Runner(traits).run()
