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
                        innerloopcommandargs='-c "from shutil import copyfile; copyfile(\'src%sShared%sSurveyPrompt.razor\', \'app%sShared%sSurveyPrompt.razor\')"' % (os.sep, os.sep, os.sep, os.sep) ,
                        projext = '.csproj',
                        processwillexit='false',
                        measurementdelay='15',
                        iterations='5'
                        )
    Runner(traits).run()
