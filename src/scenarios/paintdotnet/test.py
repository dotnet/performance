import os
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'paintdotnet'

def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='true',
                        startupmetric='PDN', 
                        timeout= f'{const.MINUTE*1}',
                        measurementdelay='10',
                        runwithoutexit='false',
                        processwillexit="false",
                        runwithdotnet='true',
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
