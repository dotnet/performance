'''
F# Console app
'''
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'FSharp.Compiler.Service'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME,
                        # startupmetric='sdk',
                        guiapp='false',
                        timeout= f'{const.MINUTE*10}'
                        )
    Runner(traits).run()
