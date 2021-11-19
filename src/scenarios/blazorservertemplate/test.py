import os
import sys
from shared.runner import TestTraits, Runner
from shared import const

EXENAME = 'blazorservertemplate'


def main():
    traits = TestTraits(exename=EXENAME,
                        guiapp='false', 
                        timeout= f'{const.MINUTE*10}'
                        )
    runner = Runner(traits)
    runner.run()
    sys.exit(1)


if __name__ == "__main__":
    main()
