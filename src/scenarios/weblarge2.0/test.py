import os
from shared.runner import TestTraits, Runner
from shared import const

SCENARIONAME = 'Web Large 2.0'
EXENAME = 'weblarge20'

def main():
    traits = TestTraits(scenarioname=SCENARIONAME,
                        exename=EXENAME,
                        guiapp='false',
                        workingdir='mvc',
                        timeout='50',  # increase timeout for the large project
                        sdk=True,
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
