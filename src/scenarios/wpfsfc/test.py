import os
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'wpfsfc'

def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='true',
                        startupmetric='WPF', 
                        timeout= f'{const.MINUTE*15}',
                        measurementdelay='6',
                        runwithoutexit='false',
                        processwillexit="false", 
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
