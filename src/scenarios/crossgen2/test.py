'''
Crossgen2
'''
from shared.runner import Runner
from shared.testtraits import TestTraits, testtypes
from shared import const

EXENAME = 'crossgen2'

if __name__ == "__main__":
    traits = TestTraits(exename=EXENAME,
                        startupmetric='Crossgen2',
                        guiapp='false',
                        timeout=const.MINUTE*30
                        )
    Runner(traits).run()
