import os
from shared.runner import TestTraits, Runner
from shared import const

SCENARIONAME = 'WPF Large'
EXENAME = 'wpflarge'

def main():
    traits = TestTraits(scenarioname=SCENARIONAME,
                        exename=EXENAME,
                        guiapp='false', 
                        timeout= f'{const.MINUTE*15}',
                        sdk=True,
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
