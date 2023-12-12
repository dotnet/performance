'''
C# Console app
'''
from shared.runner import TestTraits, Runner
import os

EXENAME = 'blazorwasminnerloop'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME, 
                        startupmetric='InnerLoop',
                        guiapp='false',
                        innerloopcommandargs='-c "from shutil import copyfile; copyfile(\'src%sPages%sSurveyPrompt.razor\', \'app%sPages%sSurveyPrompt.razor\')"' % (os.sep, os.sep, os.sep, os.sep) ,
                        projext = '.csproj',
                        processwillexit='false',
                        measurementdelay='20',
                        iterations='5'
                        )
    Runner(traits).run()
