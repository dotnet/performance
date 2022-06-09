import os
from socket import timeout
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'MauiDesktopTesting'

def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='true',
                        startupmetric='WinUI',
                        timeout=30,
                        measurementdelay='6',
                        runwithoutexit='false',
                        processwillexit="false",
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
