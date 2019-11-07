import os
from shared.runner import TestTraits, Runner
from shared import const

SCENARIO_NAME = 'Web Large 2.0'
EXE_NAME = 'weblarge20'

def main():
    traits = TestTraits(scenarioname=SCENARIO_NAME,
                        exename=EXE_NAME,
                        guiapp='false',
                        workingdir='mvc',
                        timeout='50',  # increase timeout for the large project
                        sdk=True,
                        )
    runner = Runner(traits)
    runner.run()


if __name__ == "__main__":
    main()
