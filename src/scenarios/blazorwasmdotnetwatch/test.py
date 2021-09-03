'''
C# Console app
'''
from shared.runner import TestTraits, Runner
import os

EXENAME = 'blazorwasmdotnetwatch'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='DotnetWatch',
                        guiapp='false',
                        innerloopcommandargs='-c "from shutil import copyfile, move; import time; time.sleep(15.0); copyfile(\'app%sPages%sCounter.razor\', \'src%sPages%sCounter2.razor\');copyfile(\'src%sPages%sCounter.razor\', \'app%sPages%sCounter.razor\'); move(\'src%sPages%sCounter2.razor\', \'src%sPages%sCounter.razor\')"' % (os.sep, os.sep, os.sep, os.sep, os.sep, os.sep, os.sep, os.sep, os.sep, os.sep, os.sep, os.sep) ,
                        projext = '.csproj',
                        processwillexit='false',
                        measurementdelay='20',
                        iterations='5',
                        runwithoutexit='true',
                        hotreloaditers = '2',
                        )
    Runner(traits).run()
