'''
F# Compiler Service
'''
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'FSharp.Compiler.Service'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME,
                        guiapp='false',
                        startupmetric='GenericStartup',
                        timeout= f'{const.MINUTE*10}')
    Runner(traits).run()
